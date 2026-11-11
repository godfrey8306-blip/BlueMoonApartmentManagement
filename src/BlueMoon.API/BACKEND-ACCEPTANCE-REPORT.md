# Backend Acceptance Report — BlueMoon Apartment Management v1.0

## Conclusion

**PASS WITH LIMITATIONS**

The authenticated core financial workflow was executed successfully against the dedicated test database `bluemoon_db`. The result is not a full production-readiness PASS because the repository has no automated test project, no frontend, and several non-financial management controllers are not protected by `[Authorize]`.

## Environment

- Database: `bluemoon_db` on local PostgreSQL (`localhost:5432`), confirmed by the user as a dedicated test database.
- API project: `BlueMoon.API.csproj`, target `net10.0`.
- Authentication: JWT bearer with issuer, audience, lifetime, and signing-key validation.
- Existing migrations: `InitialCreate`, `AddResidentHistoryBeforeAfterValues`.
- Migration result: database was already up-to-date; no migration was created.
- Test Admin: created as `acceptance_admin` using the application's PBKDF2-SHA256 password format. The password is intentionally not recorded here.
- Existing `probe` account: not modified, reset, or deleted.

## Automated and technical checks

| Check | Result |
|---|---|
| `dotnet build BlueMoon.API.csproj --no-restore` | PASS |
| `dotnet test BlueMoon.API.csproj --no-build` | PASS, no tests discovered |
| EF migration update | PASS, database already up-to-date |
| EF pending model changes | PASS, no pending changes |
| API startup | PASS |
| Admin login and JWT issuance | PASS, HTTP 200 |
| OpenAPI route registration | PASS, 35 routes previously discovered |
| `git diff --check` | PASS |

No automated test project exists in the repository. Acceptance execution was performed through HTTP against the isolated test database.

## Acceptance workflow results

A unique test dataset was created in the dedicated test database:

1. Admin login — **PASS**, HTTP 200, JWT returned, Admin role confirmed.
2. Create apartment — **PASS**.
3. Create household — **PASS**.
4. Create resident — **PASS**.
5. Create fee category — **PASS**.
6. Create fee collection — **PASS**.
7. Create fee charge — **PASS**; quantity 2 and unit price 100 produced total 200.
8. Initial debt — **PASS**; PaidAmount 0, OutstandingAmount 200, status Unpaid.
9. Duplicate fee charge — **PASS**; rejected with HTTP 409.
10. Partial payment of 100 — **PASS**; PaidAmount 100, OutstandingAmount 100, status PartiallyPaid.
11. Remaining payment of 100 — **PASS**; PaidAmount 200, OutstandingAmount 0, status Paid.
12. Payment history — **PASS**; two payment details returned for the charge.
13. Financial statistics — **PASS**; one charge, PaidAmount 200, OutstandingAmount 0.
14. Household search — **PASS**; test household returned by search.
15. Paid-charge search — **PASS**; one paid charge returned.
16. Payment search — **PASS**; two payment records returned.
17. Overpayment — **PASS**; rejected with HTTP 409.
18. FeeCharge without JWT — **PASS**; rejected with HTTP 401.

## Data consistency verified

- `Payment.TotalAmount` matched the sum of payment details.
- `FeeCharge.PaidAmount` increased after each payment.
- `FeeCharge.Status` changed from `Unpaid` to `PartiallyPaid` to `Paid`.
- `OutstandingAmount` reached zero after full payment.
- Payment history and financial statistics reflected both payment details.

## Negative and authorization coverage

- Duplicate charge: PASS, HTTP 409.
- Overpayment: PASS, HTTP 409.
- Missing JWT on fee-charge endpoint: PASS, HTTP 401.
- Missing related IDs and validation attributes were reviewed in DTO/controller code; not every invalid-input combination was executed.
- Wrong-role authorization was not fully executed because no non-Admin test account was created; creating extra accounts was outside the minimum acceptance dataset.

## Findings

### Medium — management endpoints lack explicit authorization

`ApartmentsController`, `HouseholdsController`, and `ResidentsController` do not declare `[Authorize]`, unlike fee, payment, history, user, and search/statistics controllers. Read and write access for these endpoints should be reviewed before production handoff. No change was made during acceptance testing to avoid silently changing established phase behavior.

### Low — no automated test project

`dotnet test` completes successfully but discovers no tests. The core workflow was validated with live HTTP calls against the isolated database instead.

### Low — local HTTPS warning

Starting the API on an HTTP-only validation URL can log an HTTPS redirection warning when no HTTPS port is configured. This did not prevent API startup or HTTP testing.

## Frontend

No frontend project or frontend assets were found in the repository. Frontend/API integration was not testable.

## Unverified items

- Full matrix of invalid DTO payloads.
- Non-Admin authenticated authorization behavior for every management endpoint.
- Frontend integration.
- Load, concurrency, and performance testing.

## Handoff recommendation

Before declaring v1.0 production-ready:

1. Decide and enforce the intended authorization policy for apartment, household, and resident management endpoints.
2. Add an automated integration test project with isolated database setup and cleanup.
3. Execute the remaining invalid-input and wrong-role matrix.
4. Provide and validate the frontend separately, if required.
