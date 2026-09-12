# School Library System Implementation Plan

## 1. Purpose

This document defines the implementation roadmap for a school library management system built with ASP.NET Core Razor Pages, Entity Framework Core, SQL Server, and ASP.NET Core Identity.

The plan is based on the conventions and lessons documented in `PROJECT_CODING_PROFILE.md`: use a recognizable layered structure, dependency injection, feature-oriented organization, explicit DTO mapping, asynchronous database operations, strong server-side business rules, secure defaults, transactions for multi-step workflows, and automated tests.

The current repository is a .NET 9 ASP.NET Core MVC starter. The first milestone therefore converts the application to Razor Pages and establishes the persistence and Identity foundations.

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

Use a modular monolith. Keep one deployable web application while separating application behavior, domain concepts, and infrastructure clearly. Do not introduce extra projects until their boundaries provide practical value.

```text
Razor Pages
    -> Application services and DTOs
        -> Domain entities and business rules
        -> Repository/query and notification abstractions
            <- EF Core, Identity, email, and file-storage implementations
                -> SQL Server
```

### Suggested solution structure

```text
LibrarySystem.sln
|-- LibrarySystem.Web
|   |-- Areas/Identity
|   |-- Pages
|   |   |-- Account
|   |   |-- Books
|   |   |-- Catalog
|   |   |-- Circulation
|   |   |-- Members
|   |   |-- Reservations
|   |   |-- Reports
|   |   `-- Settings
|   |-- ViewComponents
|   `-- wwwroot
|-- LibrarySystem.Application
|   |-- DTOs
|   |-- Interfaces
|   |-- Services
|   `-- Validation
|-- LibrarySystem.Domain
|   |-- Entities
|   |-- Enums
|   `-- Rules
|-- LibrarySystem.Infrastructure
|   |-- Data
|   |-- Identity
|   |-- Configurations
|   |-- Migrations
|   `-- Services
|-- LibrarySystem.UnitTests
`-- LibrarySystem.IntegrationTests
```

For the earliest milestones, these folders may remain in the existing web project to avoid premature restructuring. Extract projects only before feature volume makes a single project difficult to navigate.

### Request flow

```text
Razor PageModel
    -> application service interface
        -> service/use case
            -> ApplicationDbContext or focused persistence abstraction
                -> SQL Server
```

PageModels own HTTP and presentation concerns. Application services own circulation and catalog workflows. EF Core owns persistence. Business calculations and status transitions must be authoritative on the server.

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
- Add unit-test and integration-test projects.
- Add a CI workflow that restores, builds, tests, and fails on new warnings.
- Document local setup, secrets, migrations, and database reset instructions.

### Acceptance criteria

- The application starts and displays a Razor Page as its home page.
- The solution builds with no errors or compiler warnings.
- A smoke test runs successfully in CI.
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
- Authentication and authorization integration tests pass.

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
- Unit and integration tests cover provisioning, role assignment, validation, and deactivation.

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
- Catalog service and page integration tests pass.

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
- Unit tests cover all rules; integration tests cover transaction rollback and concurrency.

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
- Attempts to access another member's records are denied and tested.

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
- Tests cover grace periods, boundaries, rounding, maximums, and idempotency.

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
- Perform performance tests for catalog search and circulation workflows.
- Run dependency vulnerability and license checks.
- Complete user-acceptance testing with librarians and representative members.
- Create deployment, operations, and staff training documentation.

### Acceptance criteria

- No critical or high-severity security findings remain unresolved.
- A database restore and application rollback are demonstrated in a staging environment.
- Critical workflows meet agreed performance targets.
- All automated tests and the release pipeline pass.
- Librarian user-acceptance testing is signed off.
- Production monitoring, backups, and administrator access are verified before go-live.

### Dependencies

All earlier milestones.

## 9. Milestone Summary

| Milestone | Outcome | Suggested effort |
|---|---|---|
| 0. Baseline | Razor Pages shell, standards, tests, CI | 3-5 working days |
| 1. Identity and database | Secure login, roles, policies, EF foundation | 5-8 working days |
| 2. Members | Staff-managed accounts and library membership | 5-8 working days |
| 3. Catalog | Titles, copies, classification, search | 8-12 working days |
| 4. Circulation | Transactional checkout, return, renewal | 8-12 working days |
| 5. Reservations | Queues and member self-service | 6-9 working days |
| 6. Fines and notifications | Automated overdue workflows | 7-10 working days |
| 7. Reports and audit | Operational dashboards and exports | 6-10 working days |
| 8. Hardening and release | Security, operations, UAT, deployment | 7-12 working days |

These are planning ranges, not commitments. They assume one experienced full-time developer, timely requirements decisions, an available SQL Server environment, and no external system integration. Re-estimate each milestone after the preceding milestone demonstrates working software.

## 10. Testing Strategy

### Unit tests

Prioritize business rules that must remain stable:

- borrowing eligibility and limits;
- due-date calculation;
- renewal rules;
- reservation ordering and expiry;
- fine calculation, caps, grace periods, and rounding;
- book-copy status transitions;
- member deactivation rules.

### Integration tests

Use the real ASP.NET Core pipeline and a relational test database for:

- login, logout, authorization, and ownership checks;
- EF mappings, indexes, and constraints;
- checkout/return transaction rollback;
- concurrent checkout attempts;
- Razor Page handler validation and antiforgery behavior;
- background-job idempotency.

Do not rely only on EF Core's in-memory provider for relational or transaction behavior. Use SQL Server in CI when feasible, or a clearly documented relational substitute for fast tests plus SQL Server release tests.

### End-to-end and acceptance tests

Automate a small set of critical user journeys:

1. Administrator provisions a librarian.
2. Librarian creates a member, title, and copy.
3. Librarian checks out and returns a copy.
4. Student views their loan and places a reservation.
5. Overdue processing creates the expected result once.

## 11. Definition of Done

A feature is done only when:

- its business rules and authorization are implemented server-side;
- input validation and user-friendly error states are complete;
- nullable analysis and build warnings are clean;
- relevant unit and integration tests pass;
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
| Identity configuration exposes private pages | Fallback authorization policy and handler-level integration tests |
| Fine or notification jobs run more than once | Idempotency keys/state checks and immutable transaction history |
| Deletion breaks historical reports | Archive titles and withdraw copies instead of hard deletion |
| Large Razor pages become difficult to maintain | Thin PageModels, application services, partials, and feature JS/CSS |
| Layering becomes ceremonial or leaks EF/UI details | Add boundaries only where useful and keep interfaces technology-neutral |
| Multi-step workflows partially save | Explicit application-level transactions |
| Requirements vary by member type | Central, configurable, version-aware `LibraryPolicy` rules |
| Personal data appears in logs or exports | Data minimization, authorization, log review, and export controls |

## 13. Decisions Required Before Milestone 1 Ends

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
