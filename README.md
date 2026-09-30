# HGB_CMP_BE

Backend API for the Haryana Gramin Bank Complaint Management Portal (CMP). It receives complaints from the Bank website, exposes IAM-protected APIs to the staff portal ([HGB_CMP_FE](https://github.com/kunal-shgb/HGB_CMP_FE)), and runs the background jobs for SLA monitoring, escalation and notifications.

The full product spec is `../README.md` in the parent `HGB_CMP` folder. This file covers only what matters for working in the backend repo.

## Stack

- .NET 10 (LTS), C#, ASP.NET Core Web API
- Entity Framework Core with PostgreSQL (Npgsql)
- Redis for distributed cache and queue-backed jobs
- .NET `BackgroundService` for scheduled work
- OpenAPI/Swagger in development only
- Fronted by Nginx. Local development uses locally installed PostgreSQL and Redis (no Docker)

## Solution layout

```text
HGB_CMP_BE/
├── ComplaintManagement.sln
├── src/
│   ├── ComplaintManagement.Api/             controllers, middleware, filters, auth + policy setup, Program.cs
│   ├── ComplaintManagement.Application/     use cases: Complaints, Categories, Assignments, Escalations,
│   │                                        Reports, Notifications, Common (SLA, validation, interfaces)
│   ├── ComplaintManagement.Domain/          entities, enums, value objects, domain rules, interfaces
│   ├── ComplaintManagement.Infrastructure/  Persistence (DbContext, configs, migrations), Repositories,
│   │                                        IAM, Notifications (SMS/email), FileStorage, Services (Redis, jobs)
│   └── ComplaintManagement.Contracts/       request/response DTOs shared with the API surface
└── tests/
    ├── ComplaintManagement.UnitTests/
    └── ComplaintManagement.IntegrationTests/
```

Dependencies point inward: Api → Application → Domain, with Infrastructure implementing interfaces declared in Application/Domain. Controllers stay thin and delegate to Application services. DTOs live in Contracts and never expose EF entities directly.

## Rules that are not negotiable

The portal keeps no employee credentials: no users table with passwords, no password hashes, OTP secrets or MFA data. (Temporary exception: until the Bank IAM login API is integrated, a dummy users table stands in for it. See "Mock IAM" below.) Staff sign in with employee code and password on the portal; `POST /api/v1/auth/login` relays them to the Bank IAM API, which verifies them and returns the user profile. The password is never stored, cached or logged. The API then issues its own short-lived signed token (HS256, `Auth:SigningKey`) carrying the profile's identity and office, validated on every request by ASP.NET Core's JwtBearer middleware.

All IAM code sits in `Infrastructure/IAM` (`IamClient`, `IamUserService`, `IamTokenValidator`, `IamModels`, `IamOptions`) behind interfaces such as `IIamUserService`. Nothing outside that folder calls the IAM API directly.

Authorization has two parts: the IAM `accessRole` decides what a user may do, the IAM office decides which complaints they see (details below). Application roles are **Maker**, **Checker** and **Admin**.

Every significant action (register, view, assign, status change, remark, attachment upload/download, escalation, export) writes an `audit_logs` row with employee ID from the IAM context, action, module, record ID, IP address, user agent and timestamp.

Category groups, categories, statuses and their allowed transitions, SLA/TAT, priority rules, escalation levels and routing rules are data in the database, seeded by migrations, not hardcoded `switch` statements.

Logs never contain passwords, OTPs, access or refresh tokens, client secrets, CVV, full card numbers or unmasked customer data. Mobile, account and customer ID values are masked in logs and in any response that doesn't need them in full.

Secrets never go in `appsettings.json` or source control. Use `dotnet user-secrets` locally and environment variables or the Bank's secret store in deployed environments.

## Two API surfaces

Staff endpoints are IAM-protected and versioned under `/api/v1`:

```text
/api/v1/complaints
/api/v1/complaints/{id}
/api/v1/complaints/{id}/status
/api/v1/complaints/{id}/assign
/api/v1/complaints/{id}/remarks
/api/v1/complaints/{id}/attachments
/api/v1/complaints/{id}/history
/api/v1/categories
/api/v1/branches
/api/v1/regions
/api/v1/departments
/api/v1/reports
/api/v1/dashboard
/api/v1/notifications
```

Public customer endpoints (complaint submission from the Bank website, tracking by complaint number + registered mobile + OTP; both built) are anonymous and must be kept separate from the staff routes. The spec's example uses `POST /api/v1/complaints` for submission, which collides with the staff route; the proposal here is a `/api/v1/public/...` prefix, pending confirmation. Public endpoints need their own rate limiting, strict input validation, CAPTCHA or equivalent if the Bank requires it, and file upload checks (size, extension allow-list of PDF/JPG/JPEG/PNG/XLS/XLSX, MIME sniffing, malware scan). Tracking responses return only status, registration date, last action, customer-visible resolution text and closure date. Internal remarks, employee details and assignment data are never included.

Complaint numbers follow `HGB-YYYY-NNNNNNNN` (e.g. `HGB-2026-00001245`) and must be generated without race conditions (a PostgreSQL sequence per year or equivalent).

## Data model

Core tables: `complaints`, `complaint_category_groups`, `complaint_categories` (no sub-categories: the product owner dropped them; TAT, default priority and escalation route are set per category), `complaint_status_history`, `complaint_assignments`, `complaint_attachments`, `complaint_remarks` (with `visibility`: internal vs customer), `audit_logs`, `application_role_mapping`. Column lists are in section 33 of the parent README. Use snake_case table and column names, UTC timestamps, and GUID primary keys. EF migrations are committed with the code that needs them.

## Background jobs

SLA monitoring (Normal → SLA Warning → Overdue) and automatic escalation through Branch → RO → Head Office (built; see Escalation), SMS/email notifications, pending-complaint reminders, daily MIS generation and data cleanup. Jobs must be safe to run on more than one API instance; use a Redis lock or equivalent.

## Local development

Prerequisites: .NET 10 SDK, PostgreSQL 18 running locally on port 5432 (the EnterpriseDB installer at `/Library/PostgreSQL/18` on the current dev Mac), Redis running locally on port 6379 (`brew services start redis`).

One-time database setup: create an empty `cmp` database (the API creates the tables on first run):

```bash
/Library/PostgreSQL/18/bin/psql -h localhost -U postgres -c "CREATE DATABASE cmp;"
```

Then point the API at it (secrets never go in appsettings.json):

```bash
cd src/ComplaintManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=cmp;Username=postgres;Password=<your local postgres password>"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379"

# run: in Development the API applies migrations and seeds sample data on startup
dotnet run --launch-profile http    # http://localhost:5080, OpenAPI at /openapi/v1.json
```

Also set the token signing key and the mock IAM seed password (both secrets, never in appsettings):

```bash
dotnet user-secrets set "Auth:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "MockIam:SeedPassword" "<choose a local password>"
```

### Mock IAM (temporary dummy users)

Until the Bank IAM login API is integrated, sign-in checks a dummy users table instead: `mock_iam.users`, in its own schema and `MockIamDbContext`, outside the product migrations. Each row has the same fields as the IAM user profile plus a PBKDF2-SHA256 password hash. With `MockIam:Enabled=true` (on in `appsettings.Development.json`) the API creates the schema on startup and inserts any users from `Infrastructure/IAM/Mock/MockIamSeedData.cs` that are missing, all with `MockIam:SeedPassword`. To add a dummy user, add a row there and restart. The API refuses to start with MockIam enabled in Production. To switch to the real IAM, set `MockIam:Enabled=false` and `IAM:BaseUrl`, then drop the `mock_iam` schema.

`POST /api/v1/auth/login` with `{ "employeeCode": "300001", "password": "..." }` returns a token to send as `Authorization: Bearer <token>`. Dummy users (fictional):

| Code | Access role | Office |
|---|---|---|
| 300001 | OfficeHead | Rohtak Main (branch, RO Rohtak) |
| 300005 | NoRole (view; acts on assigned complaints) | Rohtak Main |
| 300002 | OfficeHead | Sampla (branch, RO Rohtak) |
| 300003 | OfficeHead | Hisar City (branch, RO Hisar) |
| 300004 | OfficeHead | Karnal Sector 12 (branch, RO Karnal) |
| 300010 | OfficeHead | Jhajjar Main (branch, RO Jhajjar; no sample complaints) |
| 300011 | NoRole (view; acts on assigned complaints) | Jhajjar Main |
| 200003 | Maker | RO Rohtak |
| 200001 | Checker | RO Rohtak |
| 200002 | Checker | RO Hisar |
| 200004 | Checker | RO Karnal |
| 200010 | Checker | RO Jhajjar |
| 100002 | Maker | Head Office, CSD |
| 100003 | Maker | Head Office, DBD |
| 100001 | Checker | Head Office, CSD (checks HO Makers in dev data) |
| 100004 | Checker | Head Office, DBD |
| 900001 | NoRole + isSystemAdmin (Admin) | Head Office, DBD |
| 300099 | NoRole (inactive) | always refused |

The mock IAM also holds sample Regional Offices, branches and departments (`mock_iam.regions`, `mock_iam.branches`, `mock_iam.departments`, seeded from `MockIamSeedData.cs`). The 120 sample complaints are seeded only in Development. None of this is real Bank data.

## Identity, visibility and maker-checker

The Bank IAM login API returns a user profile (`employeeCode`, `fullName`, `designation`, `accessRole`, `isSystemAdmin`, `officeType`, `officeCode`, `officeName`, `departmentName`, `isActive`, `lastLogin`, `createdAt`, ...). `Infrastructure/IAM` holds the model and the HTTP client (`IAM:BaseUrl` + `IAM:LoginPath`). Inactive employees are refused. Every sign-in attempt writes an `audit_logs` row (`LOGIN_SUCCESS`, `LOGIN_FAILED`, `LOGIN_INACTIVE`), and a wrong code, wrong password and inactive employee all get the same response.

- **What a user sees** comes from `officeType` + `officeCode`: Branch → complaints logged for that branch; Regional Office → complaints of every branch under it; Head Office → all complaints. A complaint assigned to the user personally is also visible. `OfficeScopes:Map` in appsettings maps the IAM officeType strings. Branch and RO codes from the IAM organisation data match the IAM `officeCode` values.
- **What a user does** comes from the IAM profile:
  - Every signed-in employee is a **Viewer**: they see complaints in their office scope with customer details masked, and cannot act.
  - At a **Branch**, only the **OfficeHead** (`accessRole: "OfficeHead"`) works complaints: status, assignment, remarks, attachments, escalation. Other branch staff (for example `NoRole`) only view, until the OfficeHead **assigns** them a complaint; the assignee may then do everything on that complaint except reassign it, and sees full customer details. Rights move with the assignment.
  - At **Regional Offices and Head Office**, `Maker` and `Checker` work as before (Makers work complaints, Checkers approve and can assign). An **RO Checker** may assign a branch's complaint only when that branch has no active OfficeHead in the IAM directory (`AssignmentPolicy`); otherwise the branch OfficeHead assigns it. RO/HO Makers and HO Checkers keep their assignment rights.
  - **Admin** comes from the IAM `isSystemAdmin` flag, not the access role.
  - Mappings live in `application_role_mapping` (with an optional `office_type`); per-complaint rights are decided by `ComplaintAccess` in the Application layer, and `GET /complaints/{id}` returns them as `abilities` for the UI.
- **Maker-checker.** Moves flagged `requires_approval` in `complaint_status_transitions` (seeded: to Resolved, Rejected, Duplicate) do not apply when a Maker requests them. The complaint goes to *Pending checker approval* and a `complaint_approvals` row routes it: a Branch Maker's request to a Checker at that branch's RO; an RO Maker's request to any HO Checker; an HO Maker's request to a Checker of the HO department set by the Admin (`app_settings` key `approvals.ho_maker_checker_department`; any HO Checker while unset). Approve applies the requested status; Return (remark required) puts the complaint back where it was. No one decides their own request, only one request per complaint can be open, and an `xmin` concurrency check stops two Checkers deciding the same request.

## Escalation

Levels follow the spec's ladder: 1 Branch, 2 Regional Office, 3 Head Office (the spec's level 4, a nodal officer, has no role in the IAM, so Head Office is the top). Escalation raises `complaints.escalation_level` and adds a `complaint_escalations` row (shown in the history); it does not change the status, and visibility is unchanged because the RO and HO already see their branches' complaints.

- **Automatic:** `EscalationJob` (a hosted `BackgroundService`) runs every `Escalation:IntervalMinutes` (15) and escalates open, unresolved complaints past their TAT due date: to the RO after `escalation.to_ro_after_overdue_days` (0) and to HO after `escalation.to_ho_after_overdue_days` (7). These and `escalation.enabled` are Admin settings on the Workflow screen (provisional values seeded by migration). Each run takes a Redis lock (`cmp:lock:escalation-job`), so only one API instance escalates at a time; without Redis an in-process lock is used, and Production refuses to start without Redis. `Escalation:JobEnabled=false` turns the job off on an instance.
- **Manual:** `POST /api/v1/complaints/{id}/escalate` with a reason. Makers and Checkers raise a complaint one level above the higher of its current level and their own office: a branch to its RO, an RO to Head Office. Head Office cannot escalate further.
- Filter with `minEscalationLevel`; the dashboard shows open escalated complaints.


### Routing by category

The Admin sets each category's flow on the categories screen: the **RO division** and **HO division** (IAM department codes; RO staff carry a `departmentName` from the same list as HO) that receive its escalations and Checker approvals, and whether it goes **straight to Head Office**. A branch escalation goes to the RO division (or the HO division when the category skips the RO); an RO escalation goes to the HO division; automatic escalation follows the same route. Branch approvals go to the RO division's Checkers (or the HO division's, when skipping the RO); RO approvals go to the HO division's Checkers. While a complaint sits with a division, other staff at that office can see it but not act on it. Categories with no route keep the whole-office behaviour.
## Customer tracking

`POST /api/v1/public/tracking/otp` takes a complaint number and the registered mobile. If they match, a 6-digit one-time code (valid `Tracking:OtpLifetimeMinutes`, 10) is queued as an SMS through the notification outbox; the response is the same whether or not they matched, so the endpoint cannot be used to discover complaints. At most `Tracking:MaxOtpsPerWindow` (3) codes per complaint per `Tracking:OtpWindowMinutes` (15). `POST /api/v1/public/tracking/verify` with the code returns a customer-safe view: customer-facing status, dates, category, branch, the customer-visible status changes and remarks marked visible to the customer. No internal remarks, staff names or assignment details. A code works once and locks after `Tracking:MaxOtpAttempts` (5) wrong tries. Codes are stored only as salted SHA-256 hashes, never logged, and blanked from the outbox after sending; every request, failure and view is audited.

Until an SMS gateway exists, `Tracking:ExposeOtpForTesting=true` (Development only; refused in Production) returns the code in the response so the demo page can show it.

## Customer notifications

Customer messages use a transactional outbox (`notification_outbox`): rows are written in the same save as the change that caused them, so a message is never lost or sent for a change that was rolled back. `CustomerNotifier` decides what to send: on registration; on a change of the customer-facing status label (internal moves such as Received → Assigned, or waiting for a Checker, send nothing); on resolution; on closure. Messages go by SMS, or by email when the customer chose email and gave an address. Staff see them on the complaint (`GET /api/v1/complaints/{id}/notifications`, recipient masked).

`NotificationDispatchJob` delivers due messages every `Notifications:IntervalSeconds`, under a Redis lock, retrying after 1, 2, 4 and 8 minutes and marking a message FAILED after five attempts. `Notifications:Provider` picks the sender: `Log` (Development: writes the message to the log with the recipient masked and marks it sent; refused in Production) or `None` (messages stay queued). Real SMS and email gateways plug in as further providers; SMS wording must then match the Bank's DLT-registered templates. Staff notifications (assignment, escalation) need staff contact details from the IAM and are not built yet.

## Customer feedback

After a complaint closes, the customer can say whether the issue was resolved, rate the handling 1–5 and add an optional comment (up to 1,000 characters). The closure message carries a one-time link, `{Feedback:LinkBaseUrl}/{token}` (no link while `LinkBaseUrl` is empty); the tracking view offers the same form after OTP. Tokens are 32 random bytes, stored only as a SHA-256 hash (`feedback_invitations`), tied to that closure, and work once. Feedback is accepted for the Admin-set number of days after closure (`feedback.window_days`, default 30, on the Workflow page); a link stops working once the complaint is reopened. One feedback per closure (`complaint_feedback`, unique on complaint + closure time), so a complaint closed again can be rated again.

Public endpoints: `GET /api/v1/public/feedback/{token}` (complaint number, title, closure date and OPEN / SUBMITTED / EXPIRED; no personal data) and `POST` to submit; anonymous, rate-limited and audited as `CUSTOMER`. "Not resolved" feedback is flagged for staff: it shows on the complaint and the dashboard, and `GET /api/v1/complaints?feedbackNeedsReview=true` lists it, until someone who may change the complaint's status marks it reviewed (`POST /api/v1/complaints/{id}/feedback/review`, optional note) or reopens the complaint. The dashboard adds feedback received, average rating and "not resolved" for the period.

## Attachments

Customers can attach up to 5 supporting documents when registering (multipart `POST /api/v1/public/complaints` with a `complaint` JSON field and `files`; the plain JSON call still works). Staff add files only together with a remark: multipart `POST /api/v1/complaints/{id}/remarks` with a `remark` JSON field and `files` (needs both the AddRemark and AddAttachment rights for that complaint; up to 20 files per complaint). Each file records its `remark_id`; files with no remark are the documents lodged with the complaint. The detail response returns lodged documents in `attachments` and each remark's files in `remarks[].attachments`. Downloads (`GET /api/v1/complaints/{id}/attachments/{attachmentId}`) need full customer-data access (`Complaint.ViewUnmasked`, so not Admins) and a complaint in the caller's scope, and are always served as downloads, never inline.

Every file is checked before any is stored: extension in the allow-list (PDF, JPG/JPEG, PNG, XLS, XLSX), at most 5 MB, and leading bytes matching the type (a renamed executable is refused). The stored content type is the detected one. Each file then goes through `IMalwareScanner`; infected files are refused, and until an engine is configured the placeholder records `NOT_SCANNED` and the portal shows a warning. Files are saved under random keys (`yyyy/MM/{guid}`) in `FileStorage:BasePath`, outside any web root (locally `HGB_CMP_BE/.data/attachments`, git-ignored), with their SHA-256. Uploads and downloads are audited. Limits live in the `Attachments` config section.

## Which data lives where

The portal database holds only what the portal owns: complaints and their history (status changes, assignments, remarks, attachments, approvals), audit logs, and the Admin-managed configuration (category groups and categories, statuses and transitions, priorities, role mapping, settings).

Everything about the organisation and its people comes from the Bank IAM API: employees, Regional Offices, branches (with the RO each reports to) and departments. `IIamOrganisationService` (Regional Offices, branches, departments) and `IIamUserService` (employees) are the only way in. Organisation lookups are cached for 10 minutes. The real IAM client for these is a stub until the IAM API specification is shared; today the mock IAM store serves them.

When a complaint is registered it records its branch code and name and its RO code and name as the IAM holds them at that moment; an assigned department is recorded as code and name. Visibility, Checker routing and reports use these recorded codes, so if the Bank later moves a branch to another RO, existing complaints stay with the RO they were registered under and new complaints follow the new structure.

## What is built (Phase 1 slice)

- `POST /api/v1/auth/login`: employee code + password → Bank IAM → signed portal token (rate-limited per IP).
- `GET /api/v1/me`: the caller's application roles, permissions and office scope.
- `GET /api/v1/approvals`, `POST /api/v1/approvals/{id}/approve`, `POST /api/v1/approvals/{id}/return`: the Checker's queue and decisions.
- `/api/v1/admin/categories[...]`, `/api/v1/admin/category-groups[...]` and `/api/v1/admin/workflow[...]`: Admin configuration (category groups, categories with their TAT, default priority and escalation route, status labels, transitions, HO checking department). Every change is audited. Groups are a table (`complaint_category_groups`); each category belongs to one, and hiding a group hides its categories from complaint forms. Groups and categories can be added, edited, hidden (`isActive = false`) or deleted, but a delete is refused while anything depends on the item: a group with categories, or a category any complaint uses. Hiding is the answer for anything in use, so existing complaints never lose their category.
- Complaints: list with every spec filter + paging, detail (identifiers masked unless `Complaint.ViewUnmasked`), history timeline, status change (workflow-validated), assign/reassign (target must be inside the caller's scope), remarks (internal vs customer).
- Reference data: categories, statuses and priorities (portal configuration); regions, branches and departments (from the IAM, cached 10 minutes); `GET /api/v1/employees` for assignment (from the IAM directory).
- `GET /api/v1/dashboard/summary`: totals, status counts, overdue / due-soon, 30-day trend, category, region, ageing.
- `POST /api/v1/public/complaints`: anonymous registration, rate-limited per IP (10/min), race-free `HGB-YYYY-NNNNNNNN` numbers from an atomic per-year counter.
- `GET /api/v1/public/complaints/form-options`: active branches and categories for the website form (rate-limited, no internal config such as TAT).
- Staff intake: `POST /api/v1/complaints` (JSON, or multipart with documents) lets any employee (`Complaint.Create`, part of Viewer) lodge a complaint on a customer's behalf, with no OTP. Branch staff always lodge for their own branch (any `branchCode` sent is ignored); RO and HO staff must choose the branch, from any region. `GET /api/v1/complaints/lodge-options` feeds the form. Every complaint records its `source` (WEBSITE, BRANCH, REGIONAL_OFFICE, HEAD_OFFICE, from the lodger's office type) and, for staff intake, who lodged it and from which office; the list can be filtered by `source` (`GET /api/v1/complaint-sources`). Website and staff intake share `ComplaintRegistrar`, so category, SLA and the customer's acknowledgement work the same way. Lodging does not grant any action on the complaint: the usual OfficeHead / Maker / Checker rules apply.
- Audit rows for list, view, status change, assign, remark and registration.

Provisional pieces, flagged in code: the status transition set, which moves need Checker approval and the default priority (`Persistence/Seed/ReferenceSeed.cs`), the Maker/Checker permission matrix (`Application/Common/Security/Permissions.cs`), the SLA warning window (`Sla:WarningWindowHours`) and the IAM login request/response format (`Infrastructure/IAM/IamAuthenticator.cs`). TAT values are null in the migration until the Bank supplies them. Not built yet: MIS reports and exports, and staff notifications.

Common commands:

```bash
dotnet build
dotnet test
dotnet ef migrations add <Name> --context ApplicationDbContext --project src/ComplaintManagement.Infrastructure --startup-project src/ComplaintManagement.Api
```

Local runs use the mock IAM described above.

## Configuration keys

```text
ConnectionStrings:DefaultConnection
Redis:ConnectionString
IAM:BaseUrl, IAM:LoginPath, IAM:TimeoutSeconds
Auth:SigningKey (secret), Auth:Issuer, Auth:Audience, Auth:TokenLifetimeMinutes
OfficeScopes:Map
MockIam:Enabled, MockIam:SeedPassword (secret; refused in Production)
FileStorage:BasePath (outside any web root)
Attachments:MaxFileBytes, Attachments:MaxFilesPerUpload, Attachments:MaxFilesPerComplaint
Escalation:JobEnabled, Escalation:IntervalMinutes
Notifications:Provider (Log | None), Notifications:IntervalSeconds
Tracking:OtpLifetimeMinutes, MaxOtpAttempts, MaxOtpsPerWindow, OtpWindowMinutes, ExposeOtpForTesting (Development only)
RateLimits:PublicPerMinute, PublicReadPerMinute, LoginPerMinute
Notification:SmsApiUrl, Notification:EmailApiUrl
```

## Testing

Unit tests cover complaint rules, SLA calculation, assignment and routing, escalation, validation and authorization policies (including HO/RO/Branch scoping). Integration tests cover PostgreSQL, API endpoints, IAM integration, file storage and notifications. They create a throwaway `cmp_test_<id>` database on the local PostgreSQL server for each run and drop it afterwards, so the configured user needs permission to create databases. Configure the server connection once:

```bash
cd tests/ComplaintManagement.IntegrationTests
dotnet user-secrets set "ConnectionStrings:TestServer" "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<your local postgres password>"
```

(or set `CMP_TEST_POSTGRES` in CI). The application goes through VAPT, so security-relevant behaviour needs tests, not just code.

## Open decisions

These are not settled in the spec. Don't invent specifics for them; ask or leave a clear seam.

- Bank IAM organisation API: endpoints for Regional Offices, branches (with their RO) and departments, and whether they include inactive offices.
- Bank IAM login API contract: endpoint path, request field names, error codes, and whether it also returns a token or needs client credentials. The profile shape is known; `IamAuthenticator` assumes `{ employeeCode, password }` in and the profile JSON out. Exact `officeType` strings for RO and Branch are assumed to be "Regional Office" and "Branch".
- Which HO department checks HO Makers (set by the Admin; the dev data uses CSD).
- Session policy: token lifetime (default 8 hours) and whether sign-out must revoke tokens server-side.
- Public endpoint prefix and whether the Bank website calls the API directly or through a gateway.
- File storage backend (local volume, NFS or object storage) and the malware scanning engine (e.g. ClamAV). Until an engine is plugged in, uploads are stored as NOT_SCANNED.
- SMS and email gateway providers and their APIs, and the DLT-registered SMS templates. Until then, messages are only logged (Development) or stay queued.
- Final status workflow and transition rules, SLA/TAT values per category, and escalation thresholds.
- Data retention periods for complaints, attachments and audit logs.

## Roadmap (backend view)

1. Phase 1: IAM integration seam, public complaint registration, complaint number generation, listing, details, assignment, status management, remarks, basic dashboard API.
2. Phase 2: SLA/TAT, escalation, notifications, ageing, advanced search, MIS reports, Excel/CSV export.
3. Phase 3: Bank website integration, SMS and email gateways, transaction verification, CBS / digital banking integrations.
4. Phase 4: automated routing, AI-assisted categorisation, duplicate detection, advanced analytics.
