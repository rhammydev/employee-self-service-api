# Employee Self-Service API

An ASP.NET Core Web API that takes employee requests from submission through approval to completion. It covers five modules:

| Module | What it covers |
|---|---|
| **Training requests** | Manager → HR → Finance approval chain, estacode allowance versions, withdrawal, reviewer reassignment, allowance PDF, completion evidence |
| **Learning management** | Courses, lessons, multiple-choice assessments, assignments, progress, attempt cycles |
| **Performance (KPI)** | Review periods, weighted KPI plans, progress and evidence, manager review and scoring, deadline reopening |
| **Payslips** | Sample earnings and deductions, publication, revisions, private PDF downloads |
| **Internal opportunities** | Vacancies and development programmes, eligibility rules, applications, selection status |

Supporting features: accounts and roles, JWT authentication, in-app notifications, an email queue with a background worker, daily reminders, private file downloads and a personal dashboard.

There is no frontend. Every workflow can be demonstrated in Swagger.

---

## Contents

- [Tech stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Setup](#setup)
- [Running](#running)
- [Sample accounts](#sample-accounts)
- [Seeded demo data](#seeded-demo-data)
- [API conventions](#api-conventions)
- [Endpoints](#endpoints)
- [Email delivery (at-least-once)](#email-delivery-at-least-once)
- [Daily reminders](#daily-reminders)
- [Files](#files)
- [Architecture](#architecture)
- [Business rules and assumptions](#business-rules-and-assumptions)
- [Known limitations](#known-limitations)

---

## Tech stack

- .NET 9, ASP.NET Core Web API (controllers)
- Entity Framework Core 9 with SQL Server (MSSQL), code-first migrations
- FluentValidation for request validation
- JWT bearer authentication (`System.IdentityModel.Tokens.Jwt`), BCrypt password hashing
- MailKit / MimeKit for SMTP
- QuestPDF (Community license) for the allowance statement and payslip PDFs
- Swashbuckle (Swagger UI)

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2019 or later (local install or Docker)
- EF Core CLI tools, matching the runtime version:
  ```bash
  dotnet tool update --global dotnet-ef --version 9.0.*
  ```
- An SMTP account for real email delivery (optional; see [Email delivery](#email-delivery-at-least-once))

To run SQL Server in Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name ess-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

## Setup

### 1. Secrets

Secrets never go in `appsettings.json`. The project uses .NET user-secrets in development. From the project folder (the one with `EmployeeSelfServiceAPI.csproj`):

```bash
dotnet user-secrets init   # once; the project already has a UserSecretsId

# Database
dotnet user-secrets set "ConnectionStrings:EmployeeConnection" \
  "Server=localhost,1433;Database=EmployeeSelfService;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"

# JWT (the key must be at least 32 bytes)
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets set "Jwt:Issuer" "EmployeeSelfServiceAPI"
dotnet user-secrets set "Jwt:Audience" "EmployeeSelfServiceClients"

# SMTP (optional; without it emails stay queued as Pending)
dotnet user-secrets set "SmtpMail:Username" "you@example.com"
dotnet user-secrets set "SmtpMail:Password" "your-app-password"
dotnet user-secrets set "SmtpMail:SenderEmail" "you@example.com"
```

In other environments, use environment variables with `__` in place of `:` (for example `ConnectionStrings__EmployeeConnection`).

### 2. Non-secret settings (`appsettings.json`)

| Section | Key settings | Default |
|---|---|---|
| `Auth` | `SetupTokenExpiryHours`, `ResetTokenExpiryMinutes`, `MaxFailedLogins`, `FailedLoginWindowMinutes`, `LockoutMinutes`, `ClientBaseUrl` | 48 h, 30 min, 5, 15 min, 15 min |
| `SmtpMail` | `Server`, `Port`, `SenderName` | `smtp.gmail.com`, 587 |
| `EmailQueue` | `IntervalSeconds`, `BatchSize`, `ClaimTimeoutMinutes` | 120, 20, 10 |
| `Reminders` | `IntervalMinutes` | 60 |
| `FileStorage` | `RootPath`, `MaxFileSizeBytes` | `App_Data/uploads`, 5 MB |

**Gmail:** use an [app password](https://support.google.com/accounts/answer/185833), not your account password. Port 587 uses STARTTLS and 465 uses implicit TLS.

### 3. Database and migrations

Migrations are applied **automatically on startup** (`Database.MigrateAsync()` in `Program.cs`), followed by the seeder. To apply them by hand instead:

```bash
dotnet ef database update
```

To add a migration after changing the model:

```bash
dotnet ef migrations add <Name>
```

## Running

```bash
dotnet run
```

- Swagger UI: http://localhost:5265/swagger (or https://localhost:7021/swagger with the `https` profile)
- Base route for every endpoint: `/api/v1`

**To call protected endpoints in Swagger:**
1. Call `POST /api/v1/auth/login`.
2. Copy `data.token` from the response.
3. Click **Authorize** and paste the token, without the `Bearer ` prefix.

## Sample accounts

All seeded accounts use the password **`Password@123`**.

| Employee no. | Name | Email | Roles | Dept / Grade | Reports to |
|---|---|---|---|---|---|
| ESS-0001 | Tunde Bakare | tunde.bakare@ess.test | Admin, Manager, Staff | Operations / GL-14 | none |
| ESS-0002 | Ngozi Okafor | ngozi.okafor@ess.test | HR, Manager, Staff | Human Resources / GL-14 | Tunde |
| ESS-0003 | Bola Adeyemi | bola.adeyemi@ess.test | HR, Staff | Human Resources / GL-10 | Ngozi |
| ESS-0004 | Chinedu Eze | chinedu.eze@ess.test | Finance, Manager, Staff | Finance / GL-14 | Tunde |
| ESS-0005 | Amina Bello | amina.bello@ess.test | Finance, Staff | Finance / GL-10 | Chinedu |
| ESS-0006 | Ifeoma Nwosu | ifeoma.nwosu@ess.test | Manager, Staff | Engineering / GL-12 | Tunde |
| ESS-0007 | Musa Ibrahim | musa.ibrahim@ess.test | Manager, Staff | Operations / GL-12 | Tunde |
| ESS-0008 | Kunle Adebayo | kunle.adebayo@ess.test | Staff | Engineering / GL-08 | Ifeoma |
| ESS-0009 | Zainab Yusuf | zainab.yusuf@ess.test | Staff | Engineering / GL-10 | Ifeoma |
| ESS-0010 | Emeka Obi | emeka.obi@ess.test | Staff | Operations / GL-08 | Musa |
| ESS-0011 | Grace Etim | grace.etim@ess.test | Staff (**inactive**) | Operations / GL-08 | Musa |

There are **two HR users and two Finance users** on purpose, because nobody can approve their own record. Seeded addresses use the reserved `.test` domain, so no real inbox ever receives them.

**Estacode rates** (USD a day) are seeded for GB, US, AE, ZA and KE across GL-08 to GL-14. The rate is the country base plus 20 per grade level above GL-08; for example, **KE at GL-08 is 150**, which matches the TRN-03 worked example. **There is no rate for Japan (JP)**, so the "HR approves training with no configured rate" case can be demonstrated.

## Seeded demo data

On startup the seeder adds demo data for every module, once. Each module checks a marker (seed references and titles), so restarting never duplicates anything, and the data is also added to a database that already has accounts. Dates are relative to the day of seeding, so time-based demos keep working. Seeded notifications are in-app only: **no emails are queued**.

| To demonstrate… | Use |
|---|---|
| Manager approves / rejects; Admin reassigns | `TRN-SEED-002` (Zainab, US) waiting for **Ifeoma**. Musa is another Manager to reassign to. |
| HR confirms and the allowance is generated | `TRN-SEED-003` (Emeka, ZA) waiting for HR |
| HR approves with no configured rate → 409 | `TRN-SEED-004` (Kunle, **JP**) waiting for HR |
| Finance approves; the 5 × USD 150 = 750.00 example | `TRN-SEED-005` (Kunle, KE) waiting for **Amina** or Chinedu |
| HR correction → version 2, version 1 unchanged | `TRN-SEED-006` (Kunle, US) returned to HR by Finance |
| Evidence upload and allowance PDF | `TRN-SEED-007` (Zainab, AE), approved; the training ended 10 days ago |
| 60-day no-evidence follow-up | `TRN-SEED-008` (Emeka, GB), approved; ended 75 days ago (`GET …/evidence-overdue`) |
| Edit a draft; withdraw before approval | `TRN-SEED-001` (Kunle, draft) |
| Complete lessons and pass | Kunle: *Information Security Essentials* (not started, pass mark 70) |
| Exhausted attempts → HR reset | Musa: same course, 3 failed attempts |
| Due tomorrow / overdue (reminders, dashboard) | Zainab is due tomorrow; Ifeoma is overdue |
| Edit a draft course; publish | *Customer Service Excellence* (Draft) |
| Staff updates and submits a KPI plan | Kunle: *H2 2026 Performance Review* (Active, reviewer Ifeoma) |
| Manager finalizes or returns | Zainab: *H2 2026* (Submitted, reviewer Ifeoma) |
| HR activates a plan | Emeka: *H2 2026* (Draft, weights = 100) |
| Blocked after deadline → HR reopens | Zainab: *Q3 2026 Quarterly Review* (Active, deadline passed) |
| Reopening a finalized plan → 409; scoring example | Kunle: *Q3 2026* (Finalized; target 4, actual 3, weight 20 → 75% and 15.00) |
| Finance publishes a payslip | Kunle: this month (Draft) |
| Current and historical payslip PDFs | Zainab: last month (v1 Superseded, v2 Published) |
| HR / Manager opens a payslip → 403 | Any payslip, as Bola or Ifeoma |
| Eligible applicant accepted; ineligible → 403 | *Senior Software Engineer* (Engineering, GL-08/GL-10). Kunle can apply; Emeka cannot. |
| HR moves an application | Zainab's application (Shortlisted) |
| Applying to a closed opportunity → 409 | *Data Analytics Bootcamp* (Closed) |

## API conventions

- **Base route:** `/api/v1`
- **Response wrapper:**
  ```json
  { "responseCode": "00", "responseMessage": "…", "data": { } }
  ```
- **Lists:**
  ```json
  { "responseCode": "00", "responseMessage": "…", "data": [ ], "pagination": { "pageNumber": 1, "pageSize": 20, "totalCount": 42, "totalPages": 3 } }
  ```
  Use `pageNumber` and `pageSize`. `pageSize` is capped at 100.
- **Errors:**
  ```json
  { "responseCode": "99", "responseMessage": "Validation failed.", "errors": [ { "field": "email", "message": "…" } ] }
  ```
- **Status codes:**

  | Code | Meaning |
  |---|---|
  | 400 | Validation failed |
  | 401 | Not authenticated, or the account is inactive or locked |
  | 403 | The record exists but isn't yours, or your role can't do this |
  | 404 | The record doesn't exist |
  | 409 | Duplicate, invalid status transition, or a concurrent change (stale `RowVersion`) |

- **Downloads** return the file itself (`application/pdf` and so on, with `Content-Disposition: attachment`), not JSON.
- **Dates and money:**
  - Timestamps are stored and returned in UTC.
  - Calendar dates (training dates, due dates, deadlines) are `yyyy-MM-dd`, using the **Africa/Lagos** date.
  - Money is `decimal`, rounded to 2 places with `MidpointRounding.AwayFromZero`.

## Endpoints

"Any" means any signed-in employee. Every active employee is also Staff. Ownership and assigned-reviewer checks happen in the services, on top of the role checks.

<details>
<summary><b>Auth and employees</b></summary>

| Method | Route | Who |
|---|---|---|
| POST | `/auth/login` | public |
| POST | `/auth/setup-password` | public (setup token) |
| POST | `/auth/forgot-password` | public |
| POST | `/auth/reset-password` | public (reset token) |
| POST | `/auth/change-password` | any |
| GET | `/employees/me` | any |
| PUT | `/employees/me/phone` | any |
| GET | `/employees/my-team` | Manager |
| GET | `/employees`, `/employees/{id}` | HR, Admin |
| PUT | `/employees/{id}/employment` | HR, Admin |
| POST | `/employees` | Admin |
| POST | `/employees/{id}/resend-setup-link` | Admin |
| PUT | `/employees/{id}/roles` | Admin |
| PUT | `/employees/{id}/status` | Admin |
</details>

<details>
<summary><b>Training requests</b> (<code>/training-requests</code>)</summary>

| Method | Route | Who |
|---|---|---|
| POST | `/` (create draft) | any |
| PUT | `/{id}` (edit draft) | owner |
| POST | `/{id}/attachments` (multipart, PDF/JPG/PNG ≤ 5 MB) | owner |
| POST | `/{id}/submit`, `/{id}/withdraw` | owner |
| GET | `/my`, `/{id}` | any (access-checked) |
| GET | `/pending-manager` | Manager |
| POST | `/{id}/manager-decision` | assigned Manager |
| GET | `/pending-manager/all` | Admin |
| POST | `/{id}/reassign-reviewer` | Admin |
| GET | `/pending-hr` | HR |
| POST | `/{id}/hr-approve`, `/{id}/allowance-corrections` | HR |
| GET | `/pending-finance` | Finance |
| POST | `/{id}/finance-decision` (`Approve`, `Reject`, `ReturnToHR`) | Finance |
| GET | `/{id}/allowance-pdf` | anyone who can view the request, after Finance approval |
| POST | `/{id}/evidence` (multipart) | owner |
| GET | `/evidence-pending`, `/evidence-overdue` | HR |
| POST | `/{id}/evidence-review`, `/{id}/close-no-evidence` | HR |
</details>

<details>
<summary><b>Learning</b> (<code>/courses</code>, <code>/course-assignments</code>)</summary>

| Method | Route | Who |
|---|---|---|
| POST, GET | `/courses` | HR |
| GET, PUT | `/courses/{id}` | HR |
| POST, PUT, DELETE | `/courses/{id}/lessons[/{lessonId}]` | HR (Draft only) |
| PUT | `/courses/{id}/lessons/order` | HR |
| POST, PUT, DELETE | `/courses/{id}/questions[/{questionId}]` | HR (Draft only) |
| PUT | `/courses/{id}/questions/order` | HR |
| POST | `/courses/{id}/publish` | HR |
| POST, GET | `/courses/{id}/assignments` | HR |
| GET | `/course-assignments/my`, `/course-assignments/{id}` | owner, or HR |
| POST | `/course-assignments/{id}/lessons/{lessonId}/complete` | owner |
| POST | `/course-assignments/{id}/attempts` | owner |
| POST | `/course-assignments/{id}/reset` | HR |
</details>

<details>
<summary><b>KPI</b> (<code>/review-periods</code>, <code>/kpi-plans</code>)</summary>

| Method | Route | Who |
|---|---|---|
| POST | `/review-periods` | HR |
| GET | `/review-periods` | any |
| POST, GET | `/kpi-plans` | HR |
| POST, PUT, DELETE | `/kpi-plans/{id}/items[/{itemId}]` | HR (Draft only) |
| POST | `/kpi-plans/{id}/activate`, `/kpi-plans/{id}/reopen` | HR |
| GET | `/kpi-plans/my`, `/kpi-plans/{id}` | owner, assigned reviewer, HR |
| PUT | `/kpi-plans/{id}/items/{itemId}/progress` | owner |
| POST | `/kpi-plans/{id}/items/{itemId}/evidence` (multipart) | owner |
| POST | `/kpi-plans/{id}/submit` | owner |
| GET | `/kpi-plans/pending-review` | Manager |
| POST | `/kpi-plans/{id}/review` | assigned Manager |
</details>

<details>
<summary><b>Payslips</b> (<code>/payslips</code>)</summary>

| Method | Route | Who |
|---|---|---|
| POST, GET | `/payslips` | Finance |
| PUT | `/payslips/{id}/draft` | Finance |
| POST | `/payslips/{id}/revisions`, `/payslips/{id}/publish` | Finance |
| GET | `/payslips/my` (`?month=&year=`) | any (own, published only) |
| GET | `/payslips/{id}` | owner or Finance |
| GET | `/payslips/{id}/pdf?version=` | owner or Finance |
</details>

<details>
<summary><b>Opportunities</b> (<code>/opportunities</code>, <code>/applications</code>)</summary>

| Method | Route | Who |
|---|---|---|
| POST | `/opportunities` | HR |
| PUT | `/opportunities/{id}` | HR |
| POST | `/opportunities/{id}/publish`, `/opportunities/{id}/close` | HR |
| GET | `/opportunities`, `/opportunities/{id}` | any (staff see Published only) |
| POST | `/opportunities/{id}/applications` (multipart: `SupportingStatement`, optional PDF `Cv`) | any |
| GET | `/opportunities/{id}/applications` | HR |
| GET | `/applications/my`, `/applications/{id}` | owner or HR |
| POST | `/applications/{id}/withdraw` | owner |
| POST | `/applications/{id}/status` | HR |
</details>

<details>
<summary><b>Notifications, dashboard, files, reminders</b></summary>

| Method | Route | Who |
|---|---|---|
| GET | `/notifications` (`?isRead=`) | any (own) |
| POST | `/notifications/{id}/read`, `/notifications/read-all` | any (own) |
| GET | `/dashboard/me` | any |
| GET | `/attachments/{id}/download` | authorized against the parent record |
| POST | `/reminders/run` | Admin |
</details>

## Email delivery (at-least-once)

**Business code never sends email directly.** Every notifiable event writes an in-app `Notification` and a queued `EmailMessage` in the **same transaction** as the business change. A failed email therefore never undoes the action.

`EmailQueueWorker` runs every `EmailQueue:IntervalSeconds` (default 120 s). Each run:

1. **Claims** up to `BatchSize` (default 20) due messages in a single SQL statement, using `UPDLOCK, READPAST`. Overlapping runs, or several app instances, never claim the same row.
2. Sends each message through SMTP, and marks it **`Sent` only after the SMTP server accepts it**.
3. On failure, records the attempt count and last error, then schedules a retry after **5, 15 and 60 minutes**. After one initial attempt and three retries, the message becomes **`Failed`**.
4. Re-claims a message left in `Processing` for more than `ClaimTimeoutMinutes`. This recovers from a run that crashed mid-send.

The worker honours cancellation and shuts down cleanly. SMTP credentials come from user-secrets or environment variables. If SMTP isn't configured, messages simply stay `Pending`.

> **Delivery is at-least-once, not exactly-once.** If the process stops after the SMTP server has accepted a message but before `Sent` is saved, that message is claimed again later and delivered twice. Avoiding this would need the SMTP server to support idempotency, which standard SMTP doesn't.

When a new setup or reset link is issued, any older, unsent email carrying the previous link is marked `Cancelled`, so a dead link is never delivered.

## Daily reminders

`ReminderWorker` runs every `Reminders:IntervalMinutes` (default hourly). It sends:

- **Courses:** a reminder for incomplete courses that are due tomorrow or already overdue.
- **KPI plans:** a reminder to the owner and the assigned reviewer **3 days before** the submission deadline, and **on the deadline day**.

Date boundaries use **Africa/Lagos**. Each reminder has a deduplication key (recipient, item, reminder type, Lagos date), enforced by a unique index on `ReminderLogs`. Rerunning the job, or running it hourly, never creates duplicates. Reminders go through the same email queue and retry rules.

`POST /api/v1/reminders/run` (Admin) runs the job on demand. Calling it twice in a row shows `created: 0` the second time.

## Files

- **Accepted types:** PDF, JPG and PNG up to 5 MB, and PDF only for CVs. The type is checked from the file's **content signature**, not just its extension.
- **Storage:** files are stored under `FileStorage:RootPath` (default `App_Data/uploads`, outside `wwwroot`, git-ignored) with random GUID names.
- **Downloads:** only through `GET /attachments/{id}/download`, which checks access against the parent record:

  | Attached to | Who can download |
  |---|---|
  | Training request | the owner, the current reviewer, HR, Admin, and Finance once an allowance exists |
  | KPI item | the owner, the assigned reviewer, HR |
  | CV | the applicant, HR |

- **PDFs:** the allowance statement and payslip PDFs are generated on request from the stored data, never stored.

## Architecture

```
Controllers/     HTTP only: bind, authorize by role, call one service method, return the response or file
Services/        Business rules, ownership checks, status transitions, notifications, transactions
Repositories/    EF Core data access; no business rules; never call SaveChanges themselves
Data/            AppDbContext (Fluent configuration), seeders
Domain/          Entities, enums, settings classes
DTOs/            Request and response models (entities are never returned)
Validators/      FluentValidation rules (run by ValidationFilter before every action)
Helpers/         Pure calculation and policy helpers (AllowanceCalculator, AssessmentScorer,
                 KpiCalculator, PayslipCalculator, ApplicationWorkflow, access rules, AppClock)
Mappings/        Entity → DTO mapping shared between services
Middleware/      GlobalExceptionHandler (500s, plus 409s for concurrency and unique-index conflicts)
Workers/         EmailQueueWorker, ReminderWorker (BackgroundService)
Utilities/       MailUtils (HTML-encoded email templates)
```

- **One unit of work per action.** Repositories only stage changes, and the service calls `IUnitOfWork.SaveChangesAsync()` once at the end. The business change, its history row, notifications and queued emails therefore commit together. The two places that need ordered saves wrap them in one explicit transaction: publishing a payslip revision, and applying with a CV.
- **Concurrency.** Approvable records (`TrainingRequest`, `CourseAssignment`, `KpiPlan`, `PayslipVersion`, `OpportunityApplication`) carry a SQL Server `rowversion`. Two simultaneous decisions can't both succeed: the second gets **409**. Unique and filtered unique indexes back up every "only one" rule.
- **Account status on every request.** JWT validation reloads the employee and their roles from the database. A deactivated account's existing token is rejected immediately (401), and role changes take effect on the next request.
- **Enums** are stored as their names (`nvarchar(50)`). Money uses `decimal(18,2)`; scores and KPI values use higher precision.

## Business rules and assumptions

These follow the Revised User Stories. Where the stories left a choice open, the decision taken is listed here.

**General**
- Every active employee is implicitly Staff. Nobody approves, reviews or publishes their own record.
- 403 means the record exists but isn't yours; 404 means it doesn't exist. Where a user has no right to see a record at all, permission is checked **before** status, so a 403 never reveals the record's status.
- Workflow access follows the **stored assigned reviewer**, not the current reporting line.

**Accounts**
- **Passwords:** at least 10 characters with an upper-case letter, a lower-case letter and a digit, stored with BCrypt.
- **Login failures:** every failure returns the same generic 401 (unknown email, wrong password, locked or inactive account). The real reason is written to the audit log only.
- **Lockout:** 5 failed logins within 15 minutes lock the account for 15 minutes.
- **Tokens:** setup and reset tokens are 256-bit random values. Only their SHA-256 hash is stored, they expire, and each works once. Issuing a new one invalidates the previous one.
- **Reporting managers** must exist, be active and hold the **Manager** role, so they can see their team and review requests. Reporting cycles are rejected.
- **Admin safeguards:** an Admin can't remove their own Admin role or deactivate themselves. The Manager role can't be removed while the person still has direct reports.

**Training**
- **Allowance calculation:**
  - Eligible days = `(return - departure).Days + 1`.
  - Allowance = days × daily rate, where the rate is looked up by country and the employee's grade **at the time of HR approval**.
  - The allowance stores a snapshot of what it used. Course fee and flight cost are shown separately and never added to it.
- **Finance:** Finance must send the allowance version it reviewed. If it's no longer the current version, the decision returns 409.
- **HR corrections** create a new version that can override the daily rate and/or **reduce** eligible days (never above the computed number). Earlier versions are never changed.
- **Evidence:** can be uploaded only *after* the training end date (Lagos date). HR either accepts it (→ `Completed`) or returns it (→ `Approved`).
- **Closing without evidence:** approved requests with no evidence **60 days** after the end date are listed for HR, who can close them as `RejectedNoEvidence` with a reason.
- **Withdrawal:** when a request is withdrawn at the HR or Finance stage, every active HR or Finance user is notified, because there's no single assigned person at those stages.

**Learning**
- **Questions:** each needs at least two options and exactly one correct option, and all questions weigh the same.
- **Courses:** publishing needs at least one lesson and one question. A published course is immutable; a new version means a new course.
- **Submissions:** must answer **every** question exactly once, with options that belong to their question. Invalid submissions don't use up an attempt.
- **Scoring:** score = correct / total × 100, compared **unrounded** with the pass mark and shown to 2 decimal places. A client-sent `score` is ignored, and the response reports `clientScoreIgnored: true`.
- **Attempts:** 3 per cycle. An HR reset starts a new cycle; lesson progress and earlier attempts are kept. A completed assignment can't be reset.

**KPI**
- **Weights** must total exactly 100.00, with no tolerance.
- **Reviewer:** defaults to the employee's reporting manager at activation, must be an active Manager, and can't be the owner.
- **Activation and HR access:** activation is refused if the period's deadline has already passed. HR can't create, edit or activate their own plan.
- **Staff edits:** allowed while the plan is `Active` or `Returned`, up to the end of the effective deadline day (Lagos). Every actual value must be recorded before submitting.
- **Scores** are calculated and saved when the plan is finalized:
  - achievement = min(actual / target × 100, 100)
  - weighted score = achievement / 100 × weight
  - overall score = the sum of weighted scores, rounded to 2 places
- **Reopening:**
  - Only `Active` or `Returned` plans, with a future deadline.
  - `Submitted` plans must be returned by the manager first; `Finalized` plans can never be reopened.
  - Every reopening is recorded, and the original deadline stays on the review period.

**Payslips**
- **Amounts:** all in NGN and never negative. Gross = basic + allowances; net = gross − deductions. Deductions above gross return 400.
- **Versions:**
  - Only drafts can be edited. Publishing locks the version.
  - At most one pending draft revision per payslip.
  - Publishing a revision supersedes the previous version in the same transaction.
- **Access:** staff only ever see their own published versions. HR and Managers get 403. PDFs are available to the owner and Finance, for the current and any historical published version.
- **Finance self-service:** a Finance user can't prepare or publish their own payslip.
- **Dashboard count:** the payslip count is the number of distinct published **periods** in the current calendar year, not the number of versions.

**Opportunities**
- **Publishing:** needs a future closing time. A closed opportunity is never reopened.
- **Applying:** blocked after the closing timestamp even if HR hasn't closed the record yet.
- **Eligibility:** an empty department or grade list means unrestricted. Otherwise the applicant's *current* department and grade must match (403 if not). This eligibility snapshot is stored on the application.
- **Status moves:**

  | From | To |
  |---|---|
  | Submitted | Shortlisted, Rejected |
  | Shortlisted | Selected, Rejected |

  Any other move returns 409. Staff can withdraw while an application is `Submitted` or `Shortlisted`. Selected, Rejected and Withdrawn are final.

## Known limitations

- **Email is at-least-once.** See [Email delivery](#email-delivery-at-least-once).
- **Orphaned uploads are possible.** An uploaded file is written to disk before the database save. If that save fails, the file remains but has no `Attachment` row. It can never be downloaded, but it isn't cleaned up automatically.
- **No real SMS integration.** The `SmsSettings` section is unused and not part of the brief.
- **Rate management.** Estacode rates come from seed data. There are no endpoints to manage them.
- **Sample data only.** Payroll uses sample salary bands. There's no tax engine and no payment recording, both of which are out of scope.
