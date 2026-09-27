# CLAUDE.md — HGB_CMP_BE

Backend API for the Haryana Gramin Bank Complaint Management Portal. .NET 10, ASP.NET Core Web API, EF Core + PostgreSQL, Redis, BackgroundService jobs. Local development uses a locally installed PostgreSQL and Redis; no Docker. The frontend is the sibling repo `../HGB_CMP_FE`.

Read `README.md` in this repo before starting a task. The full product spec is `../README.md`; check it before making decisions about schema, statuses, roles, APIs or the roadmap. If the spec leaves something open (see "Open decisions" in README.md), stop and ask. Don't invent specifics, especially for the IAM API contract, gateways or storage.

## Hard rules

Never violate these. If a request would break one, say so before writing code.

1. No local credential store: no users/password table, no password hashes, OTP secrets or MFA data. The only sign-in endpoint, `POST /api/v1/auth/login`, relays the employee code and password to the Bank IAM API and never stores, caches, logs or echoes the password (keep it out of exception messages, audit rows and `ToString()` output too). On success the API issues its own short-lived signed token built from the IAM profile; the signing key is a secret.
   Temporary exception, agreed with the product owner: until the Bank IAM login API is integrated, a dummy users table (`mock_iam.users`, PBKDF2 hashes only) stands in for the IAM behind `IIamAuthenticator` / `IIamUserService`. It lives only in `Infrastructure/IAM/Mock`, in its own schema and `MockIamDbContext` (never in the product `ApplicationDbContext` or its migrations), is enabled by `MockIam:Enabled`, and the API refuses to start with it in Production. Remove it when the real IAM is wired in.
2. IAM code lives only in `src/ComplaintManagement.Infrastructure/IAM`, behind interfaces like `IIamUserService`. Nothing else references the IAM API or its models.
3. Authorization has two parts. What a user may do: IAM `accessRole` (Maker / Checker / Admin) → `application_role_mapping` → ASP.NET Core named policies (`Complaint.ChangeStatus`, `Complaint.Approve`, etc.). What a user may see: the IAM `officeType` + `officeCode` (Branch → its branch, RO → branches under it, HO → all), applied inside the Application layer to every complaint query. An `[Authorize]` attribute alone is never enough.
3a. Maker-checker: transitions flagged `requires_approval` never apply on a Maker's say-so. A Branch Maker's request is decided by a Checker of that branch's RO; an RO Maker's by any HO Checker; an HO Maker's by a Checker of the HO department named in the `approvals.ho_maker_checker_department` setting (any HO Checker while unset); nobody decides their own request.
4. Every significant action writes an `audit_logs` record using the employee identity from the IAM context (via an `ICurrentUser`-style abstraction, never from the request body).
5. Never log passwords, OTPs, tokens, client secrets, CVV, full card numbers or unmasked customer data. Mask mobile/account/customer ID in logs.
6. Public customer endpoints are separate from staff endpoints, anonymous, rate-limited, strictly validated, and never return internal remarks, employee details or assignment data.
7. File uploads: size limit, extension allow-list (PDF, JPG, JPEG, PNG, XLS, XLSX), MIME/magic-byte check, malware scan hook, stored outside the web root, downloads authorized and audited.
8. No secrets in `appsettings*.json` or git. Use `dotnet user-secrets` locally, environment variables elsewhere.
9. Categories, statuses and transitions, SLA/TAT, priorities, escalation and routing rules are database-driven configuration, seeded via migrations. Don't hardcode them in `switch` statements or enums used as business rules.
9a. Organisation data (Regional Offices, branches, departments) and employees come from the Bank IAM through `IIamOrganisationService` / `IIamUserService`. Never add portal tables for them. Complaints record the office codes and names as of registration or assignment; query on those, not on a live IAM lookup.
10. The mock IAM (`MockIam`) must fail startup in Production. Its seed password lives in user-secrets or environment variables, never in appsettings.

## Architecture

- `Api`: thin controllers, middleware, filters, auth and policy registration, exception handling (ProblemDetails). No business logic, no DbContext use.
- `Application`: use cases, workflows, SLA/assignment/escalation logic, validators, interfaces for infrastructure.
- `Domain`: entities, value objects, enums, domain rules. No EF, ASP.NET or external package dependencies.
- `Infrastructure`: EF Core (DbContext, configurations, migrations), repositories, IAM, Redis, SMS/email, file storage, background jobs.
- `Contracts`: request/response DTOs. Never return EF entities from controllers.
- References point inward only: Api → Application → Domain; Infrastructure → Application/Domain; Api references Infrastructure only for DI registration.

## Code conventions

- Nullable reference types and implicit usings on; treat warnings as errors.
- async/await end to end, pass `CancellationToken` through, no `.Result` or `.Wait()`.
- Constructor DI; register services through `AddApplication()` / `AddInfrastructure()` extension methods.
- Routes versioned under `/api/v1`. Public customer routes under a separate prefix (proposed `/api/v1/public`, awaiting confirmation).
- PostgreSQL naming: snake_case tables and columns, GUID keys, `timestamptz` in UTC.
- Complaint numbers `HGB-YYYY-NNNNNNNN`, generated from a database sequence so they are race-free.
- Options pattern with validation on start for every config section.
- Structured logging with request ID and employee ID; no sensitive fields in message templates.

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/ComplaintManagement.Api
dotnet ef migrations add <Name> --context ApplicationDbContext --project src/ComplaintManagement.Infrastructure --startup-project src/ComplaintManagement.Api
dotnet ef database update --context ApplicationDbContext --project src/ComplaintManagement.Infrastructure --startup-project src/ComplaintManagement.Api
```

## How to work

- Produce complete, compilable files and state each file path. Build and run the tests before saying something is done.
- Add unit tests with any business rule: SLA calculation, assignment/routing, escalation, status transitions, authorization policies and HO/RO/Branch scoping. Integration tests run against a throwaway database on the local PostgreSQL server (see README.md).
- Commit EF migrations with the change that needs them. Never edit a migration that has already been applied; add a new one.
- Call out security concerns plainly. This code goes through VAPT.
- Keep changes scoped to the task. Ask before adding NuGet packages beyond the obvious ones (Npgsql EF provider, StackExchange.Redis, FluentValidation, Serilog, xUnit, ASP.NET Core JwtBearer).
- Commit messages: imperative, short subject, body explaining why when it isn't obvious.
