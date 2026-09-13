# Security and Authorization Review

Reviewed: September 14, 2026

## Production-mode HTTPS verification

On September 14, 2026 the application was launched with the Production environment, an explicit non-local host, persistent test key storage, and secret-store-style environment configuration. Live HTTPS responses verified HSTS, strict CSP without `unsafe-inline`, DENY framing, MIME-sniffing protection, referrer/permissions/cross-origin headers, and correlation IDs. The login response issued a Secure, HttpOnly, SameSite=Strict antiforgery cookie and contained a form token. A tokenless login POST returned 400, and direct anonymous access to the administrator role page redirected to login. The solution's application cookie configuration was also reviewed as Secure, HttpOnly, SameSite=Lax, 30-minute fixed expiry, and immediate security-stamp validation.

## Implemented controls

- Authentication is required by the fallback authorization policy. Only the landing, error, Identity recovery/login pages, static assets, and health endpoints are anonymous.
- Staff pages require the appropriate `ManageUsers`, `ManageCatalog`, `ManageCirculation`, or `ViewOperationalReports` policy. Audit logs and role assignment are administrator-only.
- Student and teacher pages require `ReserveBooks`; member-facing services resolve the member from the authenticated user and do not trust a submitted member ID.
- Sensitive services independently enforce staff or ownership boundaries. Report exports, fine lookups, member lookups, circulation, reservations, notifications, roles, inventory, and acquisitions cannot rely only on navigation visibility.
- Razor Pages validates antiforgery tokens on unsafe requests. The deployment probe verifies that a tokenless login POST is rejected.
- Identity requires confirmed accounts, unique school email addresses, strong passwords, five-attempt lockout, one-hour recovery tokens, and immediate security-stamp validation. Deactivation and role changes invalidate sessions.
- Production cookies are Secure and HttpOnly, use SameSite=Lax, expire after 30 minutes, and do not slide. Antiforgery cookies use SameSite=Strict.
- Production startup fails when AllowedHosts, persistent Data Protection storage, trusted reverse proxies, SMTP settings, or identity link settings are unsafe or missing.
- Responses set CSP, frame, MIME-sniffing, referrer, permissions, and cross-origin isolation headers. Authenticated responses are marked `no-store`.
- Authentication endpoints are rate limited. Application logs carry correlation IDs without notification bodies or credentials.

## Route and data boundary

| Area | Required access | Sensitive boundary |
|---|---|---|
| Members and roles | Staff; roles admin-only | Librarians cannot view or manage librarian/admin records |
| Catalog, inventory, acquisitions | Staff | Mutations require staff policy and service checks |
| Checkout, reservations, fines | Staff | Member IDs are accepted only by staff operations |
| Reports and CSV export | Staff | Service repeats the staff check; CSV formula injection is escaped |
| Audit log | Administrator | No librarian access |
| Student dashboard/history/holds | Student or teacher | Member is derived from authenticated identity |

## Release verification

Run this against staging after applying production configuration:

```powershell
.\scripts\Test-SecurityBaseline.ps1 -BaseUri https://library.example.edu -TestLoginRateLimit
```

Then record dated evidence for: inactive login, five-attempt lockout and timed recovery, password reset and expired/reused token rejection, email confirmation token rejection after use, session invalidation after deactivation/password/role change, student denial from every staff route, one member's denial from another member's objects, direct GET/POST handler attempts, and administrator/librarian role transitions.

The remaining credentialed tests require disposable administrator, librarian, student, teacher, inactive, and locked accounts in the actual staging identity/database environment. They must not be marked complete solely from a local source review.
