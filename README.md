# HGB_CMP_BE

Backend API for the Haryana Gramin Bank Complaint Management Portal (CMP). It receives complaints from the Bank website, exposes IAM-protected APIs to the staff portal ([HGB_CMP_FE](https://github.com/kunal-shgb/HGB_CMP_FE)), and runs the background jobs for SLA monitoring, escalation and notifications.

The full product spec is `../README.md` in the parent `HGB_CMP` folder. This file covers only what matters for working in the backend repo.

## Stack

- .NET 10 (LTS), C#, ASP.NET Core Web API
- Entity Framework Core with PostgreSQL (Npgsql)
- Redis for distributed cache and queue-backed jobs
- .NET `BackgroundService` for scheduled work
- OpenAPI/Swagger in development only
- Docker / Docker Compose, fronted by Nginx

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

The portal has no local employee authentication. There is no users table with passwords, no password hashes, OTP secrets or MFA data, and no `/auth/login` endpoint for employees. Identity comes from the Bank IAM through tokens validated by ASP.NET Core authentication middleware.

All IAM code sits in `Infrastructure/IAM` (`IamClient`, `IamUserService`, `IamTokenValidator`, `IamModels`, `IamOptions`) behind interfaces such as `IIamUserService`. Nothing outside that folder calls the IAM API directly.

Authorization runs at two levels. IAM supplies identity and enterprise roles as claims (EmployeeId, Name, Email, Role, Department, Region, Branch, Designation). The portal maps IAM roles to application roles through `application_role_mapping` and enforces permissions with named policies such as `Complaint.View`, `Complaint.Assign`, `Complaint.ChangeStatus`, `Complaint.Escalate` and `Report.View`. Every query that returns complaints is filtered by the caller's HO / RO / Branch scope, not just the endpoint policy.

Application roles: Super Admin, HO Admin, HO Department User, Regional Office User, Branch User, Nodal Officer, Management, Auditor (read-only).

Every significant action (register, view, assign, status change, remark, attachment upload/download, escalation, export) writes an `audit_logs` row with employee ID from the IAM context, action, module, record ID, IP address, user agent and timestamp.

Categories, sub-categories, statuses and their allowed transitions, SLA/TAT, priority rules, escalation levels and routing rules are data in the database, seeded by migrations, not hardcoded `switch` statements.

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
/api/v1/subcategories
/api/v1/branches
/api/v1/regions
/api/v1/departments
/api/v1/reports
/api/v1/dashboard
/api/v1/notifications
```

Public customer endpoints (complaint submission from the Bank website, tracking by complaint number + registered mobile + OTP) are anonymous and must be kept separate from the staff routes. The spec's example uses `POST /api/v1/complaints` for submission, which collides with the staff route; the proposal here is a `/api/v1/public/...` prefix, pending confirmation. Public endpoints need their own rate limiting, strict input validation, CAPTCHA or equivalent if the Bank requires it, and file upload checks (size, extension allow-list of PDF/JPG/JPEG/PNG/XLS/XLSX, MIME sniffing, malware scan). Tracking responses return only status, registration date, last action, customer-visible resolution text and closure date. Internal remarks, employee details and assignment data are never included.

Complaint numbers follow `HGB-YYYY-NNNNNNNN` (e.g. `HGB-2026-00001245`) and must be generated without race conditions (a PostgreSQL sequence per year or equivalent).

## Data model

Core tables: `complaints`, `complaint_categories` (plus sub-categories), `complaint_status_history`, `complaint_assignments`, `complaint_attachments`, `complaint_remarks` (with `visibility`: internal vs customer), `audit_logs`, `application_role_mapping`. Column lists are in section 33 of the parent README. Use snake_case table and column names, UTC timestamps, and GUID primary keys. EF migrations are committed with the code that needs them.

## Background jobs

SLA monitoring (Normal → SLA Warning → Overdue), automatic escalation through Branch → RO → HO Department → Nodal Officer, SMS/email notifications, pending-complaint reminders, daily MIS generation and data cleanup. Jobs must be safe to run on more than one API instance; use a Redis lock or equivalent.

## Local development

Prerequisites: .NET 10 SDK, Docker.

```bash
# local-only DB password (git-ignored)
cp .env.example .env    # set POSTGRES_PASSWORD

# start PostgreSQL (localhost:5433) and Redis (localhost:6380); ports avoid clashing with local installs
docker compose up -d postgres redis

# set secrets (never in appsettings.json)
cd src/ComplaintManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=cmp;Username=cmp;Password=<from .env>"
dotnet user-secrets set "Redis:ConnectionString" "localhost:6380"

# run: in Development the API applies migrations and seeds sample data on startup
dotnet run --launch-profile http    # http://localhost:5080, OpenAPI at /openapi/v1.json
```

Development sign-in: send `Authorization: Bearer dev.<EmployeeId>` for any identity in `DevAuth:Users` (`appsettings.Development.json`), e.g. `dev.E1001` (HO Admin), `dev.E2001` (RO Rohtak), `dev.E3001` (Branch Rohtak Main), `dev.E5001` (Management). The sample regions, branches, departments, role mappings and 120 complaints are seeded only in Development and are not real Bank data.

## What is built (Phase 1 slice)

- `GET /api/v1/me`: the caller's application roles, permissions and HO/RO/Branch scope.
- Complaints: list with every spec filter + paging, detail (identifiers masked unless `Complaint.ViewUnmasked`), history timeline, status change (workflow-validated), assign/reassign (target must be inside the caller's scope), remarks (internal vs customer).
- Reference data: categories, statuses, priorities, regions, branches, departments; `GET /api/v1/employees` for assignment (from the IAM directory seam).
- `GET /api/v1/dashboard/summary`: totals, status counts, overdue / due-soon, 30-day trend, category, region, ageing.
- `POST /api/v1/public/complaints`: anonymous registration, rate-limited per IP, race-free `HGB-YYYY-NNNNNNNN` numbers from an atomic per-year counter.
- Audit rows for list, view, status change, assign, remark and registration.

Provisional pieces, flagged in code: the status transition set and default priority (`Persistence/Seed/ReferenceSeed.cs`), the role → permission matrix (`Application/Common/Security/Permissions.cs`), and the SLA warning window (`Sla:WarningWindowHours`). TAT values are null in the migration until the Bank supplies them. Not built yet: attachment upload/download, escalation jobs, notifications, Redis use, MIS export, admin configuration APIs.

Common commands:

```bash
dotnet build
dotnet test
dotnet ef migrations add <Name> --project src/ComplaintManagement.Infrastructure --startup-project src/ComplaintManagement.Api
```

Until the Bank IAM spec arrives, local runs use a development-only token handler that issues test claims. It must be registered only when `ASPNETCORE_ENVIRONMENT=Development` and must fail startup if enabled anywhere else.

## Configuration keys

```text
ConnectionStrings:DefaultConnection
Redis:ConnectionString
IAM:BaseUrl, IAM:Authority, IAM:ClientId, IAM:ClientSecret
FileStorage:BasePath
Notification:SmsApiUrl, Notification:EmailApiUrl
```

## Testing

Unit tests cover complaint rules, SLA calculation, assignment and routing, escalation, validation and authorization policies (including HO/RO/Branch scoping). Integration tests cover PostgreSQL (Testcontainers), API endpoints, IAM integration, file storage and notifications. The application goes through VAPT, so security-relevant behaviour needs tests, not just code.

## Open decisions

These are not settled in the spec. Don't invent specifics for them; ask or leave a clear seam.

- IAM protocol: OAuth 2.0 / OIDC, SAML, JWT or a Bank-specific API. Claim names and the role list depend on it.
- Public endpoint prefix and whether the Bank website calls the API directly or through a gateway.
- File storage backend (local volume, NFS or object storage) and the malware scanning engine.
- SMS and email gateway providers and their APIs.
- Final status workflow and transition rules, SLA/TAT values per category, and escalation thresholds.
- Data retention periods for complaints, attachments and audit logs.

## Roadmap (backend view)

1. Phase 1: IAM integration seam, public complaint registration, complaint number generation, listing, details, assignment, status management, remarks, basic dashboard API.
2. Phase 2: SLA/TAT, escalation, notifications, ageing, advanced search, MIS reports, Excel/CSV export.
3. Phase 3: Bank website integration, SMS and email gateways, transaction verification, CBS / digital banking integrations.
4. Phase 4: automated routing, AI-assisted categorisation, duplicate detection, advanced analytics.
