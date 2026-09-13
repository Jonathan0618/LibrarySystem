# Library System --- Remaining Implementation Checklist

> Based on `IMPLEMENTATION_PLAN.md`, with the remaining work
> consolidated into an implementation checklist.
>
> **Status key:** ☐ Not finished / still needs implementation or
> verification · ☑ Already substantially implemented

------------------------------------------------------------------------

## 1. Books / Catalog Management — Complete

> **Completed September 13, 2026.** The required current-release catalog,
> title, cover, physical-copy, reference-data, and discovery workflows are
> implemented. Catalog access remains authenticated-only. CSV import was
> reviewed and intentionally deferred because it is optional for this release.

### 1.1 Add Book / Book Title

-   ☑ Backend book/title creation service exists.
-   ☑ Finish/verify the **Add Book** staff UI end-to-end.
-   ☑ Validate required book information on the server.
-   ☑ Add/select author.
-   ☑ Add/select category.
-   ☑ Add/select publisher.
-   ☑ Add description.
-   ☑ Add ISBN and other bibliographic identifiers where applicable.
-   ☑ Add cover upload.
-   ☑ Add/edit shelf or location information where the title-level
    design requires it.
-   ☑ Confirm successful creation redirects to the correct book
    details/copy-management workflow.
-   ☑ Handle duplicate/invalid ISBN or other catalog validation cases
    where applicable.

### 1.2 Edit and View Book Details

-   ☑ Complete **Book Details** page.
-   ☑ Complete **Edit Book** page and all required validation/error
    states.
-   ☑ Display authors, categories, publisher, description, cover, and
    physical-copy summary.
-   ☑ Show available/on-loan/reserved/lost/damaged/withdrawn copy
    counts.
-   ☑ Provide staff actions appropriate to the book's state.
-   ☑ Preserve row-version/concurrency protection.

### 1.3 Physical Book Copies

A library needs to distinguish a bibliographic title from each physical
copy.

-   ☑ Add **physical copy** UI.
-   ☑ Edit physical copy UI.
-   ☑ Assign barcode.
-   ☑ Assign shelf/location.
-   ☑ Set condition.
-   ☑ Record acquisition information where supported.
-   ☑ Display current copy status.
-   ☑ Show copy circulation/history.
-   ☑ Support marking a copy as lost.
-   ☑ Support marking a copy as damaged.
-   ☑ Support withdrawing a copy.
-   ☑ Prevent invalid status transitions.
-   ☑ Prevent withdrawn/lost/damaged copies from being accidentally
    checked out.
-   ☑ Add confirmation dialogs for destructive/irreversible
    operations.
-   ☑ Preserve audit records for copy-management changes.

### 1.4 Catalog Reference Data

-   ☑ Author management UI.
-   ☑ Category management UI.
-   ☑ Publisher management UI.
-   ☑ Shelf/location management UI.
-   ☑ Add/edit/disable/archive reference values as appropriate.
-   ☑ Prevent deletion of reference records that are still referenced
    by books, or provide a safe replacement/archive workflow.

### 1.5 Catalog Search / Discovery

-   ☑ Authenticated catalog search exists.
-   ☑ Catalog remains authenticated-only under the application's fallback
    authorization policy.
-   ☑ Ensure search can display availability.
-   ☑ Display physical location/shelf and relevant bibliographic
    information.
-   ☑ Ensure archived books are excluded from normal active results.
-   ☑ Verify pagination and filtering.

### 1.6 Optional Catalog Import

-   ☑ Scope decision completed: CSV/book import is not required for the
    current release and is deferred unless requirements change.
-   Deferred import requirements, if later approved: upload, preview,
    validation report, transactional commit, duplicate detection, and result
    summary.

------------------------------------------------------------------------

## 2. Members / Patrons — Complete

> **Completed September 14, 2026.** Member creation, immutable system-issued
> member identifiers, profile/details and edit workflows, obligation-aware
> activation controls, staff privacy boundaries, and administrator-only role
> management are implemented. The solution and EF model checks pass locally;
> representative staging acceptance remains part of the release-wide manual
> journeys in section 16.

### 2.1 Member Creation

-   ☑ Member creation backend and librarian Add Member workflow exist.
-   ☑ Verify complete end-to-end member creation.
-   ☑ Confirm member identifier rules: member numbers are immutable,
    system-issued identifiers in `LIB-{year}-{8-character token}` format;
    school profile fields remain separately editable.

### 2.2 Member Edit and Details

-   ☑ Member **Edit** page.
-   ☑ Member **Details/Profile** page.
-   ☑ Display account status.
-   ☑ Display current loans.
-   ☑ Display overdue loans.
-   ☑ Display reservations/holds.
-   ☑ Display outstanding fines/obligations.
-   ☑ Display relevant member activity/history according to the
    privacy policy.
-   ☑ Add clear explanation when a member cannot be deactivated
    because of outstanding obligations.
-   ☑ Enforce privacy boundaries so staff only see information
    appropriate to their role.

### 2.3 Account / Role Management

-   ☑ Administrator UI for assigning/removing library roles.
-   ☑ Verify only authorized administrators can change roles.
-   ☑ Prevent accidental removal of the last required administrator
    access.
-   ☑ Verify role/policy changes take effect correctly, including security
    stamp invalidation and member-type synchronization.

------------------------------------------------------------------------

## 3. Circulation — Complete

> **Completed September 14, 2026.** Staff checkout, return, renewal,
> lost/damaged handling, obligation resolution, member self-renewal ownership,
> copy-state enforcement, configurable member-type policies, and audited school
> closure management are implemented. Local build and EF model verification
> pass; representative staging acceptance remains part of section 16.

### 3.1 Checkout

-   ☑ Transactional/idempotent checkout exists.
-   ☑ Fine blocking exists.
-   ☑ Configurable due dates exist.
-   ☑ Perform implementation-level end-to-end checkout verification; repeat with realistic staging
    data.
-   ☑ Verify barcode/copy status changes correctly.

### 3.2 Return

-   ☑ Return workflow exists.
-   ☑ Verify the complete return workflow; repeat with realistic staging data.
-   ☑ Verify copy becomes available when appropriate.
-   ☑ Verify overdue/fine processing.
-   ☑ Verify reservation queue behavior after return.

### 3.3 Renewal

-   ☑ Backend renewal workflow exists.
-   ☑ Verify librarian renewal UI/workflow.
-   ☑ Verify member self-renewal rules.
-   ☑ Verify ownership authorization.
-   ☑ Verify renewal limits and blocked conditions.

### 3.4 Lost / Damaged Copies

-   ☑ Backend lost/damaged workflows exist.
-   ☑ Add staff UI to mark a copy **Lost**.
-   ☑ Add staff UI to mark a copy **Damaged** through the condition-aware
    return workflow.
-   ☑ Show resulting fine/obligation.
-   ☑ Allow authorized staff to resolve the resulting obligation.
-   ☑ Ensure the copy cannot be normally circulated while
    lost/damaged.

### 3.5 Calendar / Due-Date Policies

-   ☑ Confirm and expose exact loan-period policy by member type.
-   ☑ Confirm and expose loan limits by member type.
-   ☑ Confirm weekend due-date behavior through the per-policy closure setting.
-   ☑ Confirm holiday/closure-date behavior through the audited closure calendar.
-   ☑ Add/expose school calendar/closure-date management UI if staff
    must maintain it.
-   ☑ Verify due-date calculation paths; repeat with production-shaped staging scenarios.

------------------------------------------------------------------------

## 4. Reservations / Holds — Complete

> **Completed September 14, 2026.** Student/teacher placement, ownership-safe
> cancellation, queue position, staff queue management, copy assignment,
> ready-for-pickup, checkout-backed fulfillment, cancellation, expiry/no-show
> handling, concurrency protection, and staff authorization are implemented.
> Wishlist, recommendations, and curriculum assignments are explicitly deferred
> from the current release.

### 4.1 Student/Teacher Reservations

-   ☑ Reservation creation and duplicate prevention exist.
-   ☑ Per-member limits exist.
-   ☑ Ready-for-pickup assignment exists.
-   ☑ Expiry worker exists.
-   ☑ Complete final authorization verification across all reservation
    actions.
-   ☑ Verify reservation ownership.
-   ☑ Verify cancellation rules.
-   ☑ Verify expiry behavior.

### 4.2 Staff Reservation Management

-   ☑ Staff **Reservation Queue** page.
-   ☑ View waiting reservations.
-   ☑ View queue position.
-   ☑ Mark reservation ready for pickup.
-   ☑ Record pickup/fulfillment.
-   ☑ Cancel reservation.
-   ☑ Handle expired reservations.
-   ☑ Handle no-show/expired pickup cases through the pickup-expiry worker.
-   ☑ Prevent unauthorized cross-member actions through staff-only page and
    service authorization.

### 4.3 Product Decision

-   ☑ Wishlist/favorites are deferred from the current release.
-   ☑ Recommendations are deferred from the current release.
-   ☑ Curriculum/course/book assignments are deferred from the current release.

------------------------------------------------------------------------

## 5. Fines / Payments / Adjustments — Complete

> **Completed September 14, 2026.** Authorized staff can search fines, inspect
> reasons and immutable transaction history, and record payments, credit
> adjustments, or full waivers. Member-type monetary rules are editable and
> are consumed consistently by overdue, lost/damaged, and checkout workflows.

### 5.1 Fine Staff UI

-   ☑ Fine assessment and transaction backend exists.
-   ☑ Staff **Fine Management** page.
-   ☑ View member outstanding fines.
-   ☑ View fine details/reasons.
-   ☑ Record payment.
-   ☑ Record adjustment.
-   ☑ Record waiver where authorized.
-   ☑ Show immutable fine transaction history.
-   ☑ Prevent unauthorized modification/deletion of financial history.
-   ☑ Support explicit monetary or non-monetary policy through configurable
    values; zero values disable monetary assessment for that rule.

### 5.2 Fine Rules

-   ☑ Finalize and expose overdue grace period/rate/maximum settings.
-   ☑ Finalize and expose lost-item fine settings.
-   ☑ Finalize and expose damaged-item fine settings.
-   ☑ Finalize and expose checkout-blocking balance settings.
-   ☑ Verify all rules are consistently enforced by backend services.

------------------------------------------------------------------------

## 6. Notifications — Implementation Complete

> **Completed September 14, 2026.** Final school-library templates, sender
> visibility, event-specific test messages, outbox status, failure details,
> manual retry, deduplication, leased delivery, and exponential retry are
> implemented. A real provider, sender-domain approval, secret-store values,
> and live staging delivery evidence remain deployment-environment tasks and
> must not be fabricated or committed.

### 6.1 Notification Content

-   ☑ Notification outbox/deduplication and background processing exist.
-   ☑ Finalize notification templates for due-soon, overdue, ready-for-pickup,
    and account-security events.
-   ☑ Expose the configured sender identity to authorized staff.
-   ☑ Finalize school/library wording.
-   ☑ Confirm which events generate email notifications.
-   ☑ Verify notification links and account-security wording.

### 6.2 Production Email

-   [ ] Supply the selected production SMTP provider values at deployment.
-   ☑ Require SMTP credentials through external production configuration;
    secrets are not committed.
-   [ ] Verify the real sender/domain with the selected provider.
-   ☑ Provide confirmation-email delivery testing through the normal outbox.
-   ☑ Provide password-recovery delivery testing through the normal outbox.
-   ☑ Provide reservation/availability notification testing.
-   ☑ Provide account-related notification testing.
-   ☑ Expose and verify retry/failure behavior through outbox operations.

------------------------------------------------------------------------

## 7. Dashboard and Reports — Complete

> **Completed September 14, 2026.** Staff dashboard placeholders were replaced
> with working routes; database-backed counts, member-scoped self-service,
> filtered/paginated reports, privacy-gated CSV export, empty states, and
> localized date/currency presentation are implemented. Production-shaped data
> validation remains part of release acceptance.

### 7.1 Staff Dashboard

-   ☑ Librarian dashboard largely exists.
-   ☑ Replace placeholder staff actions/links/buttons.
-   ☑ Ensure dashboard links lead to real completed pages.
-   ☑ Verify counts and balances use actual database queries.
-   ☑ Verify empty states and error states.

### 7.2 Student/Teacher Dashboard

-   ☑ Current loans, holds, arrivals, reading count, fine information,
    and activity feed are substantially implemented.
-   ☑ Complete implementation-level end-to-end verification.
-   ☑ Verify all displayed data is scoped to the current user.
-   ☑ Verify no other member's data can be accessed by manipulating
    URLs/IDs.

### 7.3 Reports

-   ☑
    Current/overdue/period/popular/inactive/lost-damaged/member-activity/fine
    reports exist.
-   ☑ Verify all report filters.
-   ☑ Verify pagination.
-   ☑ Verify CSV export.
-   ☑ Verify exported data respects authorization/privacy.
-   ☑ Verify database-backed report paths; repeat against realistic staging data.
-   ☑ Verify date/time and currency formatting.

------------------------------------------------------------------------

## 8. Inventory / Collection Management — Complete

> **Completed September 14, 2026.** Stocktake sessions preserve a fixed
> expected-copy snapshot, accept barcode scans, identify discrepancies, and
> support audited reconciliation. Withdrawal records preserve their reason,
> actor, timestamp, catalog copy, and all circulation history.

> This is an important practical addition if the goal is a more complete
> school-library system.

### 8.1 Physical Inventory / Stocktaking

-   ☑ Add inventory/stocktaking workflow.
-   ☑ Generate fixed expected-copy list.
-   ☑ Scan/search physical copy barcode.
-   ☑ Mark copies as found.
-   ☑ Identify missing/unscanned copies.
-   ☑ Produce inventory discrepancy report.
-   ☑ Support reconciliation by authorized staff.
-   ☑ Record audit history of inventory adjustments.

### 8.2 Collection Maintenance

-   ☑ Support review of inactive/unused materials through operational reports.
-   ☑ Support withdrawal/weeding workflow.
-   ☑ Record withdrawal reason.
-   ☑ Preserve historical records after withdrawal.
-   ☑ Avoid deleting circulation/audit history when a copy is
    withdrawn.

------------------------------------------------------------------------

## 9. Acquisitions / New Materials — Complete

> **Completed September 14, 2026.** The optional expansion is now implemented
> with university purchase/donation records, receiving into physical copies,
> order and receiving statuses, costs, annual budgets, and summary reporting.

### 9.1 Basic Acquisition

-   ☑ Acquisition record.
-   ☑ Purchase/order record.
-   ☑ Receiving record.
-   ☑ Acquisition cost.
-   ☑ Donation/gift source where applicable.
-   ☑ Link received materials to book titles and physical copies.

### 9.2 Budget / Acquisition Management

-   ☑ University purchase and donation-source management.
-   ☑ Acquisition budget tracking.
-   ☑ Order status.
-   ☑ Receiving status.
-   ☑ Basic acquisition reporting.

------------------------------------------------------------------------

## 10. Serials / Periodicals

> Magazines, journals, newspapers, and recurring issues are a standard
> library-system area but are not necessary for the MVP unless required
> by the school.

-   [ ] Decide whether serials/periodicals are in scope.
-   If required:
    -   [ ] Publication/subscription record.
    -   [ ] Issue tracking.
    -   [ ] Expected issue tracking.
    -   [ ] Received/missing issue status.
    -   [ ] Search/display serial issues.
    -   [ ] Basic serials reporting.

------------------------------------------------------------------------

## 11. Security and Authorization

> **Implementation review completed September 14, 2026.** Code-controlled
> protections and operating procedures are implemented. Items explicitly
> labeled staging or university approval remain release gates and require real
> evidence; they cannot be certified by source code alone.

### 11.1 Production Security

-   ☑ Production SMTP configuration without hard-coded secrets.
-   ☑ First production administrator secret/onboarding process.
-   ☑ Persistent ASP.NET Core Data Protection keys.
-   ☑ Content Security Policy and security headers.
-   ☑ Review page-level authorization.
-   ☑ Review handler-level authorization.
-   ☑ Review antiforgery coverage.
-   ☑ Verify cookie/security settings in production-mode HTTPS staging probe.

### 11.2 Manual Security Verification

-   [ ] Test inactive account login.
-   [ ] Test lockout.
-   [ ] Test password recovery.
-   [ ] Test confirmation/recovery token behavior.
-   [ ] Test security-stamp invalidation where applicable.
-   ☑ Test anonymous access to staff pages.
-   [ ] Test unauthorized access to other members' data.
-   ☑ Test anonymous direct URL and tokenless-handler bypass attempts.
-   [ ] Test role changes.

### 11.3 Privacy / Retention

-   ☑ Define technical retention baseline: audit logs 2,555 days and
    terminal notifications 90 days, disabled until approval.
-   [ ] Obtain/record privacy/legal-hold approval.
-   ☑ Define current borrowing history as identifiable while obligations
    remain active.
-   ☑ Define returned-loan history as identifiable pending an approved
    anonymization period; no silent deletion or disconnection.
-   ☑ Document staff privacy responsibilities.

------------------------------------------------------------------------

## 12. Accessibility and UX

-   [ ] Keyboard navigation check.
-   [ ] Screen-reader labels/check.
-   [ ] Color contrast check.
-   [ ] Form validation accessibility.
-   [ ] Error-message accessibility.
-   [ ] Responsive layout check.
-   [ ] Browser compatibility check.
-   [ ] Device-size check.
-   [ ] Verify barcode workflows are practical for librarians.
-   [ ] Verify dialogs/confirmation prompts are clear.

------------------------------------------------------------------------

## 13. Performance / Reliability

-   ☑ Initial isolated performance drill exists.
-   [ ] Repeat performance gate against production-shaped staging.
-   [ ] Test realistic book/member/loan/audit distributions.
-   [ ] Inspect slow database queries if performance differs from the
    existing drill.
-   [ ] Inspect query execution plans where necessary.
-   [ ] Verify indexes under realistic data.
-   [ ] Test concurrent checkout/return/reservation/fine operations.
-   [ ] Repeat idempotency tests under simultaneous requests.
-   [ ] Test notification-worker crash/recovery.
-   [ ] Test reservation-worker crash/recovery.

------------------------------------------------------------------------

## 14. Backup / Restore / Operations

-   ☑ Backup/restore procedure and disposable restore drill exist.
-   [ ] Perform retained-artifact recovery test in staging.
-   [ ] Verify database restore procedure using a realistic backup.
-   [ ] Verify application configuration recovery.
-   [ ] Verify Data Protection key recovery.
-   [ ] Verify operational monitoring/alerts.
-   [ ] Verify health/readiness checks.
-   [ ] Document incident/recovery procedures.

------------------------------------------------------------------------

## 15. Deployment / Release

### 15.1 Release Artifact

-   ☑ Reproducible artifact/manifests/SHA-256/startup/readiness evidence
    exist.
-   [ ] Retain the previous production artifact.
-   [ ] Perform actual artifact swap in staging.
-   [ ] Perform authenticated rollback smoke test.
-   [ ] Verify migrations during deployment.
-   [ ] Verify rollback/recovery procedure.

### 15.2 Production Configuration

-   [ ] Production secrets.
-   [ ] SMTP secrets.
-   [ ] Database connection string/secrets.
-   [ ] Data Protection key storage.
-   [ ] Monitoring configuration.
-   [ ] Logging configuration.
-   [ ] Backup schedule.
-   [ ] Retention configuration.

### 15.3 Remove Development Artifacts

-   [ ] Remove/disable development-only 20-title seed before production.
-   [ ] Remove development-only credentials/configuration.
-   [ ] Review test/sample accounts.
-   [ ] Review debug/development settings.
-   [ ] Review dead/placeholder code.

------------------------------------------------------------------------

## 16. Testing / Verification

Because the current project explicitly avoids adding automated test
projects unless that decision changes:

### Manual Acceptance Journeys

-   [ ] Administrator creates/provisions librarian.
-   [ ] Librarian creates a member.
-   [ ] Librarian edits/views a member.
-   [ ] Librarian adds a book.
-   [ ] Librarian edits/views a book.
-   [ ] Librarian adds a physical copy.
-   [ ] Librarian edits a physical copy.
-   [ ] Librarian checks out a copy.
-   [ ] Librarian returns a copy.
-   [ ] Librarian renews a loan.
-   [ ] Student views current loan.
-   [ ] Student reserves a book.
-   [ ] Librarian processes reservation queue.
-   [ ] Librarian marks reservation ready.
-   [ ] Reservation is picked up/fulfilled.
-   [ ] Reservation expires correctly.
-   [ ] Overdue fine is generated.
-   [ ] Staff records fine payment/adjustment/waiver.
-   [ ] Lost copy workflow works.
-   [ ] Damaged copy workflow works.
-   [ ] Notification is queued and delivered in staging.
-   [ ] Unauthorized user cannot access staff/member data.
-   [ ] Audit record is created for important changes.

### Concurrency / Reliability

-   [ ] Simultaneous checkout test.
-   [ ] Simultaneous reservation test.
-   [ ] Simultaneous fine/payment operation test.
-   [ ] Worker retry test.
-   [ ] Worker crash/recovery test.
-   [ ] Database concurrency/conflict test.

------------------------------------------------------------------------

## 17. Training / UAT / Sign-off

-   [ ] Create librarian quick-start instructions.
-   [ ] Create administrator setup instructions.
-   [ ] Document member-account workflow.
-   [ ] Document adding books.
-   [ ] Document adding physical copies/barcodes.
-   [ ] Document checkout/return/renewal.
-   [ ] Document reservations.
-   [ ] Document fine handling.
-   [ ] Document lost/damaged copy handling.
-   [ ] Conduct librarian UAT.
-   [ ] Conduct student/teacher self-service UAT.
-   [ ] Record UAT issues.
-   [ ] Resolve blocking UAT issues.
-   [ ] Obtain final project acceptance/sign-off.

------------------------------------------------------------------------

# Recommended Implementation Order

## Phase 1 --- Finish the Core Staff UI

These should be implemented first because the backend already exists for
much of them.

1.  [x] Add Book
2.  [x] Book Details
3.  [x] Edit Book
4.  [x] Add Physical Copy
5.  [x] Edit Physical Copy
6.  [x] Copy barcode/status/condition/shelf management
7.  [x] Mark Lost/Damaged/Withdrawn
8.  [x] Member Details
9.  [x] Edit Member
10. [x] Administrator Role Management
11. [x] Author/Category/Publisher/Shelf management

## Phase 2 --- Finish Daily Librarian Operations

12. [x] Staff Reservation Queue
13. [x] Ready-for-Pickup workflow
14. [x] Reservation cancellation/expiry handling
15. [x] Staff Fine Management
16. [x] Fine payment/adjustment/waiver
17. [x] Calendar/closure management
18. [x] Complete checkout/return/renewal UI verification

## Phase 3 --- Improve Discovery and Collection Management

19. [x] Cover upload
20. [x] Finalize catalog search/discovery
21. [x] Keep catalog authenticated-only
22. [x] Physical inventory/stocktaking
23. [x] Collection withdrawal/weeding

## Phase 4 --- Optional ILS Expansion

24. [x] Basic acquisitions
25. [x] Purchase/order/receiving
26. [x] Donation tracking
27. [ ] Serials/periodicals
28. [—] Optional CSV catalog import — deferred unless requirements change
29. [ ] Optional wishlist/recommendations/curriculum features

## Phase 5 --- Production Readiness

30. [ ] Activate selected production SMTP provider
31. [ ] Production secrets
32. [ ] Data Protection keys
33. [ ] Security review
34. [ ] Privacy/retention approval
35. [ ] Accessibility review
36. [ ] Performance testing
37. [ ] Concurrency/reliability testing
38. [ ] Backup/restore testing
39. [ ] Deployment/rollback testing
40. [ ] Monitoring/health checks
41. [ ] Training
42. [ ] UAT
43. [ ] Final sign-off

------------------------------------------------------------------------

# What Should NOT Be Added Unless Required

A standard ILS can contain many more modules, but these are **not
necessary** for this school project unless the requirements change:

-   [ ] Multi-school/multi-branch support
-   [ ] RFID
-   [ ] Native mobile application
-   [ ] SIS integration
-   [ ] Online payment gateway
-   [ ] Interlibrary loan
-   [ ] Full MARC21/RDA/Z39.50 cataloging stack
-   [ ] Full electronic-resource management
-   [ ] Microservices architecture
-   [ ] Advanced recommendation engine

The goal should be a **complete, usable school library system**, not a
clone of a commercial enterprise ILS.

------------------------------------------------------------------------

# Highest-Priority Missing Features

If development time is limited, prioritize these:

1.  **Member Details/Edit**
2.  **Admin Role Assignment**
3.  **Staff Reservation Queue**
4.  **Staff Fine Management**
5.  **Calendar/closure management**
6.  **Inventory/stocktaking**
7.  **Production SMTP/secrets**
8.  **Security verification**
9.  **Staging/UAT/backup/rollback verification**

------------------------------------------------------------------------

# Practical Definition of "Complete"

The system should not be considered fully complete merely because the
backend services work.

A librarian should be able to perform this complete real-world workflow
through the UI:

**Create member → Add book → Add physical copy → Barcode copy → Put copy
on shelf → Search book → Check out → Renew → Return → Reserve → Process
hold → Mark ready → Pick up → Generate fine → Pay/adjust/waive fine →
Mark lost/damaged when necessary → Audit the action → Run reports.**

Once that workflow is fully usable, authorized, validated, audited,
tested, and production-ready, the project is much closer to a genuinely
complete school library system.
