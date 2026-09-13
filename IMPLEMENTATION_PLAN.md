# School Library System Implementation Plan

## 1. Purpose

This document defines the implementation roadmap for a school library management system built with ASP.NET Core Razor Pages, Entity Framework Core, SQL Server, and ASP.NET Core Identity.

The plan is based on the conventions and lessons documented in `PROJECT_CODING_PROFILE.md`: use a recognizable layered structure, dependency injection, feature-oriented organization, explicit DTO mapping, asynchronous database operations, strong server-side business rules, secure defaults, transactions for multi-step workflows, and repeatable verification.

The repository is now a .NET 9 ASP.NET Core Razor Pages application with ASP.NET Core Identity, Entity Framework Core, SQL Server, and five projects with explicit responsibilities. Milestones 0 through 5 have established the application shell, members, catalog, circulation, reservations, and student self-service workflows. Milestone 7 includes working librarian and student dashboards plus circulation reporting. Production email delivery, remaining staff management screens, staging verification, and release sign-off are still outstanding.

## 2. Product Goals

The system will allow a school to:

- maintain a searchable catalog of books and physical copies;
- manage students, teachers, librarians, and administrators securely;
- check books out and receive returned books;
- track due dates, renewals, overdue loans, fines, lost books, and damaged books;
- reserve books that are currently unavailable;
- maintain authors, categories, publishers, shelves, and school settings;
- provide dashboards, operational reports, and a complete audit trail;
- protect personal and operational data through role-based authorization.

## 3. Initial Scope and Assumptions

### In scope for the first production release

- One school and one library branch.
- Razor Pages for all user-facing screens.
- SQL Server through Entity Framework Core.
- ASP.NET Core Identity for authentication and authorization.
- Roles: `Administrator`, `Librarian`, `Teacher`, and `Student`.
- Book-title records and independently tracked physical copies.
- Barcode-based circulation, with keyboard entry as a fallback.
- Loans, returns, renewals, reservations, fines, notifications, and reports.
- Server-rendered UI with small, feature-specific JavaScript modules where needed.

### Deferred unless requirements change

- Multiple schools or library branches.
- Public self-registration.
- Online fine payment.
- RFID hardware integration.
- Native mobile applications.
- Integration with a student information system.
- Microservices or distributed deployment.

## 4. Recommended Architecture

Use a modular monolith. `LibrarySystem` is the single deployable web application; the other projects are class libraries that enforce understandable code boundaries without introducing separate services or deployments.

```text
LibrarySystem (Razor Pages and composition root)
    -> LibrarySystem.Application (interfaces, requests, and DTOs)
    -> LibrarySystem.Services (business workflow implementations)
        -> LibrarySystem.Infrastructure (EF Core, Identity, storage, migrations)
            -> LibrarySystem.Application
            -> LibrarySystem.Domain
                -> SQL Server
```

### Current solution structure

```text
LibrarySystem.sln
|-- LibrarySystem
|   |-- Areas/Identity
|   |-- Pages
|   |   |-- Dashboards
|   |   |   |-- Books.cshtml
|   |   |   |-- Catalog.cshtml
|   |   |   |-- Checkouts.cshtml
|   |   |   |-- LibrarianDashboard.cshtml
|   |   |   |-- Members.cshtml
|   |   |   `-- Reports.cshtml
|   |   `-- Shared
|   `-- wwwroot
|-- LibrarySystem.Application
|   |-- Catalog
|   |-- Circulation
|   |-- Dashboard
|   |-- Members
|   |-- Reports
|   |-- Reservations
|   `-- Security
|-- LibrarySystem.Domain
|   |-- Catalog
|   |-- Circulation
|   |-- Members
|   `-- Reservations
|-- LibrarySystem.Infrastructure
|   |-- Auditing
|   |-- Catalog entities and storage adapters
|   |-- Circulation persistence entities
|   |-- Data and migrations
|   |-- Identity
|   |-- Member persistence entities
|   |-- Reservation persistence entities
|   `-- Security adapters
`-- LibrarySystem.Services
    |-- Catalog
    |-- Circulation
    |-- Dashboard
    |-- Members
    |-- Reports
    `-- Reservations
```

`LibrarySystem.Pages` was intentionally removed. All `.cshtml` files and PageModels belong in the deployable `LibrarySystem/Pages` directory so Razor Page discovery and Visual Studio scaffolding work normally.

### Project responsibilities and dependency rules

| Project | Responsibility | May depend on |
|---|---|---|
| `LibrarySystem.Domain` | Framework-light enums and domain concepts | No other solution project |
| `LibrarySystem.Application` | Service contracts, DTOs, requests, validation annotations, security constants | `Domain` |
| `LibrarySystem.Infrastructure` | `IdentityDbContext`, persistence entities, EF mappings, migrations, Identity, auditing, and storage adapters | `Application`, `Domain` |
| `LibrarySystem.Services` | Catalog, member, circulation, reservation, dashboard, and report workflows | `Application`, `Infrastructure` |
| `LibrarySystem` | Razor Pages, Identity UI, authorization configuration, middleware, and dependency composition | All required class libraries |

Do not put HTTP concerns in services, do not expose `DbSet` or `IQueryable` through application interfaces, and do not move Razor Pages into a class library. Register business services through `AddLibraryServices()` and keep infrastructure adapters registered at the web composition root.

### Request flow

```text
Browser
    -> Razor Page / PageModel in LibrarySystem
        -> interface and DTO in LibrarySystem.Application
            -> implementation in LibrarySystem.Services
                -> ApplicationDbContext in LibrarySystem.Infrastructure
                    -> SQL Server
```

PageModels own HTTP binding, redirects, status messages, and presentation concerns. Services own validation and workflows. Infrastructure owns persistence and external adapters. EF Core migrations stay in `LibrarySystem.Infrastructure`, while `LibrarySystem` remains the startup project for migration commands. Business calculations and status transitions are authoritative on the server.

## 5. Core Domain Model

### Identity and people

- `ApplicationUser`: extends `IdentityUser`; includes `FirstName`, `LastName`, `IsActive`, and audit information.
- `ApplicationRole`: extends `IdentityRole` if role metadata is required.
- `LibraryMember`: links a user to library-specific data such as member number, member type, grade or department, borrowing status, and limits.
- `AuditLog`: records security-sensitive and business-critical actions.

`ApplicationDbContext` should inherit from:

```csharp
IdentityDbContext<ApplicationUser, ApplicationRole, string>
```

This keeps Identity and library data in one transactional database while preserving explicit relationships.

### Catalog

- `Book`: bibliographic title data, ISBN, title, edition, publication year, description, and publisher.
- `BookCopy`: physical item with unique barcode, acquisition date, price, shelf, condition, and availability status.
- `Author` and `BookAuthor`: many-to-many author relationship.
- `Category` and `BookCategory`: many-to-many classification relationship.
- `Publisher`.
- `ShelfLocation`.

Separating `Book` from `BookCopy` is essential: one title can have several physical copies, and each copy can have its own circulation state and condition.

### Circulation

- `Loan`: member, book copy, checkout date, due date, return date, renewal count, and status.
- `LoanHistory`: immutable record of significant loan events.
- `Reservation`: queue entry for a book title, with expiry and fulfillment status.
- `Fine`: charge type, amount, balance, status, and reason.
- `FineTransaction`: adjustment, waiver, or payment record.
- `LibraryPolicy`: configurable limits for loan length, renewals, reservations, and fines by member type.

### Supporting enums

- `MemberType`: Student, Teacher, Librarian.
- `BookCopyStatus`: Available, OnLoan, Reserved, Lost, Damaged, Withdrawn.
- `BookCondition`: New, Good, Fair, Poor, Damaged.
- `LoanStatus`: Active, Returned, Overdue, Lost.
- `ReservationStatus`: Waiting, ReadyForPickup, Fulfilled, Expired, Cancelled.
- `FineStatus`: Outstanding, PartiallyPaid, Paid, Waived.

Avoid storing a second source of truth where a value can be derived reliably. For example, display availability can be calculated from eligible copies, while each copy retains its authoritative status.

## 6. Security and Authorization Model

### Authentication

- Use ASP.NET Core Identity with confirmed accounts configured according to the school's onboarding process.
- Disable public registration unless explicitly approved.
- Allow administrators or librarians to create member accounts.
- Require password reset on first login for provisioned users.
- Configure secure cookies, lockout, password rules, and token lifetimes by environment.
- Use a scoped current-user abstraction only if services need user identity for auditing.

### Authorization

Use policies instead of scattering role-name checks throughout PageModels:

| Policy | Typical access |
|---|---|
| `ManageUsers` | Administrator |
| `ManageCatalog` | Administrator, Librarian |
| `ManageCirculation` | Administrator, Librarian |
| `ViewOperationalReports` | Administrator, Librarian |
| `ViewOwnAccount` | Any authenticated user |
| `ReserveBooks` | Active Student or Teacher member |

Apply a fallback policy requiring authenticated users, then explicitly allow anonymous access only to login and any approved public catalog pages. Razor Pages form posts must use antiforgery protection. Never trust user IDs, prices, fine totals, due dates, or status values posted by the browser.

## 7. Coding and Data Conventions

- Use PascalCase for public types and members, camelCase for parameters and locals, and `_camelCase` for private fields.
- Make injected dependencies `private readonly`.
- Use `Async` on asynchronous methods and pass `CancellationToken` through database operations.
- Enable nullable reference types and maintain a zero-warning target.
- Return nullable types for lookups that can legitimately fail.
- Initialize collections and use required members or constructors for mandatory data.
- Use explicit DTO projections and input models; do not bind domain entities directly to forms.
- Keep PageModels thin and move workflows into application services.
- Keep EF Core and UI-specific types out of general application interfaces.
- Use focused queries instead of exposing unrestricted `DbSet` or `IQueryable` across layers.
- Use transactions for checkout, return, reservation fulfillment, and fine updates.
- Use optimistic concurrency tokens on records that can be updated concurrently, especially `BookCopy`, `Loan`, and `Reservation`.
- Store all timestamps in UTC and convert them for display at the UI boundary.
- Use named migrations such as `CreateInitialLibrarySchema` or `AddReservationQueue`.
- Keep page-specific JavaScript and CSS in feature files when a page becomes substantial.

## 8. Implementation Milestones

## Milestone 0 - Requirements and Project Baseline

**Goal:** Agree on the first-release rules and establish a repeatable engineering baseline.

### Deliverables

- Confirm member types, borrowing limits, loan periods, renewal rules, fine rules, and reservation rules.
- Confirm whether students can browse anonymously and whether email delivery is available.
- Record the supported SQL Server environment and deployment target.
- Convert the current starter from MVC routing to Razor Pages.
- Remove unused MVC controller/view scaffolding after the Razor Pages shell is working.
- Add `.editorconfig`, solution-wide analyzers, and deterministic formatting conventions.
- Keep the solution limited to the web application and focused class libraries.
- Establish repeatable restore, build, migration, and manual smoke-check commands.
- Document local setup, secrets, migrations, and database reset instructions.

### Acceptance criteria

- The application starts and displays a Razor Page as its home page.
- The solution builds with no errors or compiler warnings.
- The documented application smoke check succeeds.
- No credentials or connection-string secrets are committed to source control.

### Dependencies

None.

---

## Milestone 1 - Identity, Database, and Application Shell

**Goal:** Establish secure authentication, authorization, database persistence, and shared navigation.

### Deliverables

- Add the EF Core SQL Server, EF design-time, and ASP.NET Core Identity packages compatible with .NET 9.
- Create `ApplicationUser`, optional `ApplicationRole`, and `ApplicationDbContext` based on `IdentityDbContext<ApplicationUser, ApplicationRole, string>`.
- Configure connection strings using user secrets for development and environment-managed secrets elsewhere.
- Register Razor Pages, EF Core, Identity, authorization policies, and application services in `Program.cs`.
- Add middleware in the correct order: exception handling/HSTS, HTTPS, static assets, routing, authentication, authorization, and Razor Pages.
- Scaffold and tailor the required Identity pages: login, logout, access denied, password change, and password reset.
- Create idempotent role and first-administrator seeding without embedding a production password or password hash in source.
- Add the authenticated layout, navigation, validation summary, status messages, and access-denied experience.
- Create the initial migration and database.

### Acceptance criteria

- An administrator can sign in and sign out.
- An unauthenticated visitor is redirected to login for protected pages.
- A user without a required policy receives an access-denied response.
- Roles can be seeded repeatedly without duplication.
- Identity tables and initial library tables are created by migrations.
- Login, logout, and authorization behavior pass manual verification.

### Dependencies

Milestone 0.

---

## Milestone 2 - Member Administration

**Goal:** Allow authorized staff to manage library membership safely.

### Deliverables

- Implement `LibraryMember` and its EF Core configuration.
- Create unique member-number generation and validation.
- Add administrator pages to create, view, edit, activate, and deactivate users and members.
- Assign roles and member types through authorized workflows.
- Add search and filters for name, member number, grade, department, role, and status.
- Add a member-details page showing current loans, reservation count, and outstanding fine balance.
- Prevent deactivation when policy requires active obligations to be resolved.
- Add audit entries for account creation, role changes, activation, and deactivation.

### Acceptance criteria

- An authorized administrator can provision a member account without public registration.
- Duplicate usernames, emails, and member numbers are rejected with clear validation messages.
- Deactivated members cannot sign in or initiate circulation actions.
- Unauthorized users cannot access or post to member-management pages.
- Provisioning, role assignment, validation, and deactivation pass manual and database verification.

### Dependencies

Milestone 1.

---

## Milestone 3 - Catalog and Book-Copy Management

**Goal:** Build a reliable catalog and inventory of physical library materials.

### Deliverables

- Implement `Book`, `BookCopy`, `Author`, `Category`, `Publisher`, `ShelfLocation`, and join entities.
- Add EF configurations, unique indexes, required constraints, and concurrency tokens.
- Add staff Razor Pages for catalog list, details, create, edit, and archive operations.
- Add copy-management pages for barcode, condition, acquisition information, shelf, and status.
- Add server-side searching, sorting, filtering, and pagination.
- Add cover-image support through an abstraction with file validation, size limits, safe generated names, and a default image.
- Add optional CSV import with preview, row validation, error reporting, and transactional commit.
- Prevent destructive deletion of titles or copies with circulation history; archive or withdraw them instead.

### Acceptance criteria

- Staff can create one title with multiple independently tracked copies.
- Each active copy has a unique barcode.
- Catalog search supports title, ISBN, author, category, and barcode.
- Invalid or conflicting edits return useful validation messages.
- Historical records survive book withdrawal or archival.
- Catalog service and page workflows pass manual and database verification.

### Dependencies

Milestones 1 and 2.

---

## Milestone 4 - Checkout, Return, and Renewal

**Goal:** Deliver the core circulation workflow with transactional integrity.

### Deliverables

- Implement `Loan`, `LoanHistory`, and `LibraryPolicy`.
- Add a checkout service that validates member status, borrowing limit, fines, copy status, and reservation priority.
- Calculate due dates on the server from the applicable member policy and school calendar rules.
- Add a barcode-focused circulation page for finding the member and scanning one or more copies.
- Add a return service that records return time and condition and updates copy status.
- Add renewal rules for maximum renewals, overdue items, outstanding reservations, and member eligibility.
- Wrap each checkout, return, and renewal operation in an explicit transaction.
- Make operations idempotent where repeated submissions are possible.
- Handle optimistic concurrency conflicts with a clear message and refreshed state.
- Create audit and loan-history records within the same transaction.

### Acceptance criteria

- An available copy can be checked out to an eligible member and receives the correct due date.
- A copy cannot be checked out twice, even under concurrent requests.
- An ineligible member receives a specific server-side rejection reason.
- Returning a copy closes the loan and makes the copy available or routes it to the next reservation.
- Renewal respects policy and reservation constraints.
- A failure at any point rolls back the complete circulation operation.
- Manual verification covers business rules, transaction rollback, and concurrency handling.

### Dependencies

Milestones 2 and 3.

---

## Milestone 5 - Reservations and Member Self-Service

**Goal:** Let members manage their own library activity and reserve unavailable titles.

### Deliverables

- Implement `Reservation` with queue position, expiry, status, and concurrency protection.
- Add catalog pages suitable for students and teachers.
- Add a member dashboard showing active loans, due dates, overdue items, reservations, and fines.
- Allow eligible members to place and cancel their own reservations.
- Fulfill reservations by title when a suitable copy becomes available.
- Add staff pages to manage ready-for-pickup and expired reservations.
- Add a secure renewal request/action using the circulation service.
- Ensure members can access only their own account data.

### Acceptance criteria

- A member can reserve an unavailable title only once.
- Reservation order is deterministic and cannot be bypassed during checkout.
- A returned copy is assigned correctly or becomes generally available.
- Expired or cancelled reservations release the next eligible reservation.
- Attempts to access another member's records are denied during authorization verification.

### Dependencies

Milestone 4.

---

## Milestone 6 - Overdue Processing, Fines, and Notifications

**Goal:** Automate overdue management and provide traceable fine handling.

### Deliverables

- Implement `Fine` and `FineTransaction`.
- Calculate overdue status and fines on the server from versioned library policy.
- Define rounding, maximum fine, grace-period, lost-item, and damaged-item rules.
- Add a background job for overdue detection, reservation expiry, and queued notifications.
- Add notification templates for due-soon, overdue, ready-for-pickup, and account events.
- Implement an email abstraction with a development capture provider and production provider configuration.
- Add staff workflows for adjustments and waivers with reason and audit fields.
- If cash payments are recorded, add a payment-entry workflow and immutable transaction history; do not add online payment processing in this milestone.

### Acceptance criteria

- Re-running the background job does not duplicate fines or notifications.
- Fine amounts are reproducible from stored policy and transaction history.
- Waivers and adjustments require authorization and a reason.
- Notification failures are logged and retried without corrupting business state.
- Verification covers grace periods, boundaries, rounding, maximums, and idempotency.

### Dependencies

Milestones 4 and 5.

---

## Milestone 7 - Dashboards, Reports, and Audit

**Goal:** Give staff reliable operational insight without coupling reports to UI controls.

### Deliverables

- Add dashboard metrics for available copies, active loans, overdue loans, reservations, and outstanding fines.
- Add reports for current loans, overdue items, circulation by period, popular books, inactive items, lost/damaged items, member activity, and fine balances.
- Add date, member type, category, and status filters.
- Add CSV export with authorization and safe formula-escaping.
- Add searchable audit-log pages restricted to administrators.
- Use focused read models and `AsNoTracking` queries for reporting.
- Review indexes using representative query plans and data volume.

### Acceptance criteria

- Report totals reconcile with underlying transactional records.
- Reports remain paginated and responsive at the agreed target data volume.
- Exported data respects the same filters and authorization as the screen.
- Audit records identify actor, action, target, timestamp, and relevant outcome.

### Dependencies

Milestones 4 through 6.

---

## Milestone 8 - Production Hardening and Release

**Goal:** Validate security, reliability, operability, and usability before launch.

### Deliverables

- Perform authorization review for every Razor Page and handler.
- Verify antiforgery, secure cookies, HTTPS/HSTS, lockout, data protection, and secret management.
- Add global exception handling, structured logging, correlation IDs, health checks, and database connectivity checks.
- Add rate limiting to authentication and other abuse-sensitive endpoints where appropriate.
- Add backup, restore, migration, rollback, and disaster-recovery procedures.
- Add production seed/onboarding procedure for the first administrator.
- Perform accessibility and responsive-layout review for common school devices.
- Perform performance checks for catalog search and circulation workflows.
- Run dependency vulnerability and license checks.
- Complete user-acceptance testing with librarians and representative members.
- Create deployment, operations, and staff training documentation.

### Acceptance criteria

- No critical or high-severity security findings remain unresolved.
- A database restore and application rollback are demonstrated in a staging environment.
- Critical workflows meet agreed performance targets.
- The release build, migration checks, and critical workflow checks pass.
- Librarian user-acceptance testing is signed off.
- Production monitoring, backups, and administrator access are verified before go-live.

### Dependencies

All earlier milestones.

## 9. Current Implementation Status

Status reviewed on September 13, 2026.

- **Complete:** all currently agreed milestone deliverables are usable.
- **Partial:** meaningful implementation exists, but one or more listed workflows remain.
- **Not started:** no material milestone implementation exists yet.

| Milestone | Status | Already completed | Remaining work |
|---|---|---|---|
| 0. Baseline | Complete | Razor Pages application, coding profile, implementation plan, SQL Server configuration, five-project solution, clean build conventions | Continue maintaining documentation as architecture changes |
| 1. Identity and database | Partial | `IdentityDbContext`, custom user/role types, SQL Server EF configuration/migrations, policies, login/logout, fallback authentication, secure cookies, lockout, guarded administrator seed, persistent keys, time-limited confirmation/reset/email-change tokens, queued confirmation, enumeration-resistant recovery, validated link origin, POST-only recovery throttling, tailored access denied, forgot/reset password, confirmation/resend, confirmed email change, change-password UI, server-enforced first-login password replacement, and global `@nvsu.edu.ph` validation | Finalize school wording/support policy, configure real production SMTP/secrets, and verify confirmation, reset, email change, lockout, first-login, and delivery end to end in staging |
| 2. Members | Partial | Annotated member model, create/update/search service operations, librarian Add Member page, searchable member list, working activation/deactivation and confirmation resend actions, administrator-only service guards for role changes and librarian-account management, audit records, and staff-facing student-email redaction | Add edit/detail screens, administrator role-assignment UI, member loan/reservation/fine summary, and clear obligation checks |
| 3. Catalog | Substantially complete | Annotated title/copy/reference models; catalog services; title search/filtering/pagination; authenticated discovery with availability and shelf display; cover upload; title create/details/edit/archive; copy create/edit/history and guarded lost/damaged/withdrawn transitions; reference-data add/edit/archive; row-version concurrency; and an idempotent 20-title temporary development dataset | Perform multi-user acceptance testing and add the optional validated CSV import only if requirements change |
| 4. Circulation | Partial | Transactional and idempotent checkout, return, renewal, and backend lost-item workflows; loan history; reservation priority; configurable due dates; audited school-calendar service; fine-based checkout blocking; lost/damaged fine integration; concurrency handling; librarian checkout page; and passing isolated database evidence for rollback, stale row versions, and checkout operation/copy uniqueness | Confirm calendar policies and approved closure dates; expose calendar/lost-item management during UI work; repeat simultaneous service-level operations under staging load |
| 5. Reservations | Partial | Reservation queue, duplicate prevention, per-member limits, ready-for-pickup assignment, expiry worker, student/teacher dashboard, read-only catalog search, hold placement/cancellation, queue and pickup-deadline display, ownership-safe renewal, My Books, My Holds, Reading History, and read-only Account pages | Add staff reservation queue and pickup management, complete cross-member authorization verification, and decide whether wishlist, recommendations, and curriculum assignments are required |
| 6. Fines and notifications | Partial | Fine and immutable transaction models; overdue/lost/damaged assessment; payment/adjustment/waiver validation and central auditing; notification outbox and deduplication; serializable multi-worker delivery claims; crash-recovery leases; configurable attempt, batch, and capped backoff boundaries; safe development capture; validated SMTP; account-event queuing; and background processing | Add staff fine UI, finalize policy values/templates, connect production SMTP secrets, and execute controlled staging failure/recovery and concurrency scenarios |
| 7. Dashboards, reports, and audit | Partial | Librarian dashboard with outstanding-fine balance; student dashboard backed by current-user loans, holds, arrivals, annual reading count, fine state, checkout-block state, and a privacy-safe activity feed; shared fixed-size student ribbon; current, overdue, period, popular-book, inactive-book, lost/damaged-copy, member-activity, and fine-balance reporting; filters; pagination; safe CSV export; audit search; report indexes; and a repeatable isolated 1,000-book/10,000-loan/10,000-audit performance drill that passes all query thresholds | Replace any remaining placeholder staff actions, repeat the performance gate against production-shaped staging data, and review execution plans if its distribution or infrastructure differs materially from the synthetic baseline |
| 8. Hardening and release | Partial | Documented security review; production security/logging/health controls; vulnerability and license evidence; production configuration template; opt-in retention cleanup; migration artifacts; guarded backup/restore tooling; a passing disposable restore drill; and reproducible application artifacts with manifests, SHA-256 verification, isolated startup, liveness, and database-readiness testing | Repeat recovery and retained-previous-artifact tests in staging; perform the actual artifact swap and authenticated rollback smoke test; approve retention/legal-hold values; supply production secrets/monitoring; execute performance at representative volume; complete compatibility checks, training, UAT, and sign-off |

Identity and student self-service are substantially complete in implementation. Shared page styling now lives under `wwwroot/css`, static assets are anonymously accessible without bypassing page authorization, and the student ribbon retains consistent sizing across student pages. The next implementation target is the remaining staff UI: member edit/details and obligation summaries, administrator-only role assignment, copy/withdrawal management, the reservation queue, and fine/lost-item actions. Production SMTP setup, staging security verification, and multi-user acceptance remain environment tasks.

## 9.1 Release Candidate and Staff Operations Checklist

Documentation updates are consolidated in this implementation plan. Do not create additional Markdown documents for subsequent milestones unless the project owner changes this decision.

Run the automated release candidate gate against the deployed staging instance:

```powershell
.\scripts\Test-ReleaseCandidate.ps1 -BaseUri https://library-staging.example.edu -TestLoginRateLimit
```

Supply `-Artifact` together with `-ConnectionString` to validate a retained rollback package, and `-PerformanceBackupFile` to include the isolated representative-volume drill. The runner writes machine-readable evidence to `artifacts/release/release-candidate.json`, never records the connection string, and returns a failing exit code when any included gate fails.

Staff training and acceptance must cover:

1. Administrator bootstrap, immediate seed-secret removal, administrator recovery, and role boundaries.
2. Member creation, confirmation, activation/deactivation, and handling unresolved obligations.
3. Title and copy creation, barcode handling, catalog search, archival, damage, loss, and withdrawal rules.
4. Checkout, return, renewal, reservation priority, overdue handling, and concurrency/conflict messages.
5. Fine assessment, payment, adjustment, waiver reasons, immutable transaction history, and audit review.
6. Notification failure/retry handling without exposing message bodies or recovery links in support records.
7. Health/alert interpretation, correlation-ID troubleshooting, backup verification, isolated restore, and application rollback.
8. Incident recording, privacy minimization, legal holds, retention approval, and escalation contacts.

Release sign-off requires the automated JSON evidence plus named operator, librarian, administrator, and representative-member approval for the applicable manual workflows. Failed checks must link to remediation and a later passing run; evidence must not contain passwords, tokens, connection strings, or unnecessary personal information.

Run the destructive-behavior safeguards only in the isolated integrity database:

```powershell
.\scripts\Test-BackendIntegrityDrill.ps1 -BackupFile 'D:\SqlBackups\LibrarySystem.bak' -Server 'SQLSERVER-STAGING' -ConfirmDrill
```

The target name is constrained to `_Integrity_RestoreDrill`, existing targets are refused by the underlying restore guard, and the database is removed afterward. The drill proves transaction rollback, stale row-version rejection, checkout operation/copy uniqueness, loan/type fine uniqueness, and notification-key deduplication without modifying the source database.

## 9.2 Whole-System Gap Register

This register consolidates incomplete work that crosses milestone boundaries. Items are ordered by release risk and dependency.

### Security and identity

- Verify the implemented access-denied, change-password, forgot/reset-password, confirmation/resend, confirmed email-change, and forced first-login workflows against production SMTP and approved school wording in staging.
- Configure a production email sender before enabling token-based account recovery or confirmation.
- Provision the first production administrator through an environment secret store and ensure temporary librarian seeding is not enabled in production.
- Add an administrator-only role-assignment UI; backend guards already separate this permission from ordinary librarian member management.
- Configure `DataProtection:KeysPath` to persistent, access-controlled storage in each deployed environment; production startup rejects a missing path.
- Review the implemented Content Security Policy and response headers for the final deployment; decide whether Google Fonts will remain an external dependency or be hosted locally, and remove `unsafe-inline` when page assets permit.
- Perform a page-by-page and handler-by-handler authorization and antiforgery review.
- Verify account lockout, inactive-user rejection, access denial, recovery-token lifetime, and security-stamp behavior manually.
- Obtain school/privacy-owner approval for the documented retention periods and legal-hold process. Automated bounded retention is implemented for expired audit logs and terminal notification content; accounts, circulation/financial history, exports, backups, and operational logs still require approved rules before broader deletion or anonymization is added.

### Member administration

- Add edit and details pages; member creation, confirmation delivery queuing, activation/deactivation, and confirmation resend are implemented.
- Add administrator-only role assignment UI; the secure initial-password/forced-first-login workflow is implemented.
- Display active loans, reservations, outstanding fines, and other obligations on member details.
- Give staff a clear explanation when deactivation is blocked by unresolved obligations.
- Preserve the implemented privacy boundary: staff may enter a student's school email during provisioning but must not see or search it afterward. The student may still view their own address, and backend identity/notification services retain it only for authentication and delivery.

### Catalog and physical inventory

- Verify the completed title, cover, copy, reference-data, and lost/damaged/withdrawn workflows with representative multi-user staging data.
- Add the optional CSV import only with preview, row validation, conflict reporting, and transactional commit.
- Keep the temporary 20-title catalog seed idempotent and development-only; replace or remove it before production data import.

### Circulation, reservations, fines, and notifications

- Confirm the policy values for the implemented outstanding-fine checkout threshold; the secure default is zero, so any unpaid balance blocks checkout.
- Expose the implemented mark-lost workflow in the staff UI when UI work resumes.
- Confirm which member policies should enable the implemented weekend/holiday-aware due-date calculation and populate the approved school closure dates; calendar-day calculation remains the default.
- Add staff reservation queue, ready-for-pickup, cancellation, and expiry management.
- Add staff fine screens for payment, adjustment, and waiver with immutable history and required reasons.
- Finalize production account-event and circulation notification wording, sender identity, and SMTP secrets; the queueing and SMTP adapter are implemented.
- Execute the documented controlled staging scenarios for concurrency, transaction interruption, reservation ownership, fine maximums, notification failure/recovery, and idempotency; implementation-level fine precision/resolution rules and leased multi-worker notification claims are now in place.

### Dashboard, reporting, and user experience

- Replace any remaining librarian-dashboard placeholder links and buttons with working destinations and actions; student navigation and self-service actions are connected.
- Verify all report totals and CSV output against populated transactional data.
- Repeat the passing isolated synthetic-volume gate against production-shaped staging data and inspect execution plans if data distribution or infrastructure differs materially. The local drill passed at 1,000 books, 10,000 loans, and 10,000 audit records.
- Confirm the intended currency/culture used to display fine balances.
- Complete keyboard, screen-reader, contrast, responsive-layout, browser, and common school-device checks.
- Keep shared visual rules in `wwwroot/css`; avoid page-level `<style>` blocks and inline `style` attributes so ribbon and typography sizing remain consistent.

### Operations, release, and verification

- Translate the implemented platform-neutral alert template into the selected production monitoring platform, configure its notification channel, and verify alert delivery. Machine-readable health probing and structured application logs are implemented.
- Backup, restore, migration, artifact packaging, rollback, and disaster-recovery procedures are documented. SQL backup/restore integrity and rollback-artifact hash/startup/readiness checks pass locally; repeat them with the retained previous version and complete the authenticated artifact-swap drill in staging.
- Execute the documented administrator onboarding and database/SMTP/Data Protection rotation procedures with the selected production platform; explicit bootstrap safeguards and procedures are implemented.
- Create deployment, operations, troubleshooting, and staff-training runbooks.
- Regenerate dependency vulnerability and license evidence for every release candidate; the September 13, 2026 scan found no known vulnerable NuGet packages, and the 92-record license inventory has no unresolved metadata entries.
- Populate a staging environment with representative data and run the critical manual workflows listed below.
- Complete librarian/member user-acceptance testing and obtain release sign-off.
- The repository currently has no automated test projects by explicit project decision; until that changes, every release depends on documented build, database, security, and manual workflow evidence.

## 10. Verification Strategy

The project currently uses class libraries only; no test projects will be added unless that decision is changed explicitly. Each milestone must still be verified proportionally through build, database, and manual workflow checks.

### Build and static verification

- Restore and build the complete solution with zero warnings and zero errors.
- Keep nullable reference analysis and recommended analyzers enabled.
- Run `dotnet ef migrations has-pending-model-changes` after persistence changes.
- Review authorization policies, model annotations, cancellation-token flow, and transaction boundaries.

### Database verification

- Apply named EF Core migrations to the configured SQL Server database.
- Verify constraints, indexes, concurrency tokens, seed data, and migration history.
- Confirm that failed multi-step operations roll back and do not leave partial state.
- Back up non-development databases before migration deployment.

### Manual workflow verification

Run the application against SQL Server and verify these critical journeys:

1. Administrator provisions a librarian.
2. Librarian creates a member, title, and copy.
3. Librarian checks out and returns a copy.
4. Student views their loan and places a reservation.
5. A reservation moves through waiting, ready, fulfilled, cancelled, and expired states correctly.
6. Overdue processing creates the expected result once.

## 11. Definition of Done

A feature is done only when:

- its business rules and authorization are implemented server-side;
- input validation and user-friendly error states are complete;
- nullable analysis and build warnings are clean;
- the solution builds cleanly and relevant manual/database verification passes;
- database changes include a clearly named migration;
- logging and audit requirements are addressed;
- accessibility and responsive behavior are checked;
- no secrets, temporary files, backup files, dead code, or commented experiments are introduced;
- documentation is updated where setup or user behavior changed;
- the feature has been demonstrated against its acceptance criteria.

## 12. Key Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Two librarians try to loan the same copy | Transaction plus concurrency token and database constraints |
| Client manipulates due dates, fines, or member IDs | Recalculate and authorize all values on the server |
| Identity configuration exposes private pages | Fallback authorization policy and handler-level authorization checks |
| Fine or notification jobs run more than once | Idempotency keys/state checks and immutable transaction history |
| Deletion breaks historical reports | Archive titles and withdraw copies instead of hard deletion |
| Large Razor pages become difficult to maintain | Thin PageModels, application services, partials, and feature JS/CSS |
| Layering becomes ceremonial or leaks EF/UI details | Add boundaries only where useful and keep interfaces technology-neutral |
| Multi-step workflows partially save | Explicit application-level transactions |
| Requirements vary by member type | Central, configurable, version-aware `LibraryPolicy` rules |
| Personal data appears in logs or exports | Data minimization, authorization, log review, and export controls |

## 13. Decisions Required Before Release

- Will the catalog be public or require authentication?
- Who creates student and teacher accounts?
- Are school-issued IDs used as member numbers?
- What are the exact loan periods and item limits for students and teachers?
- Are weekends and school holidays excluded from due-date calculations?
- Are fines monetary, non-monetary, or disabled?
- Can members renew their own loans?
- How long are reservations held for pickup?
- Which email service and sender address will be used?
- Is one school/library sufficient for the expected lifetime of the first release?
- What retention period applies to loan history, audit logs, and inactive accounts?

## 14. Recommended Delivery Sequence

Deliver milestones as vertical, demonstrable slices. At the end of each milestone, deploy to a shared test environment, demonstrate the acceptance criteria to a librarian or project owner, record feedback, and update the next milestone's estimates. Avoid starting reports or advanced automation before the catalog and circulation rules have stabilized.

The minimum viable product is complete at the end of Milestone 4: authorized staff can manage members and books and perform secure, transactional checkout, return, and renewal. Milestones 5 through 8 turn that operational core into a complete, production-ready school library service.
