# Verification Record

Environment: Windows, .NET SDK 10.0.402, Docker Desktop 29.7.2, Node 24, PostgreSQL 17.

## Automated checks

| Check | Command | Result |
|---|---|---|
| Solution build | `dotnet build SupportDesk.slnx --configuration Release` | Succeeded, 7 projects, 0 warnings |
| Unit tests | `dotnet test tests/SupportDesk.UnitTests` | 30 passed, 0 failed |
| Integration tests | `dotnet test tests/SupportDesk.IntegrationTests` | 9 passed, 0 failed |
| Frontend lint | `npm run lint` (in `src/supportdesk-web`) | Passed |
| Frontend build | `npm run build` | Passed |
| Container build and start | `docker compose up --build -d` | PostgreSQL, API, worker, and web started; API and web healthy |
| Web health | `GET http://localhost:8080/healthz` | 200 |
| API anti-forgery | `GET http://localhost:8080/api/v1/auth/antiforgery` | 200 |

## Integration coverage

- Health endpoint is anonymous and returns 200.
- Ticket endpoints require authentication and return 401 without a session.
- Sign-in failure returns a generic message and does not reveal whether the account exists.
- A customer cannot read another user's ticket.
- Internal notes are visible to support staff and absent for the requester.
- A dynamically created role with granted permissions gives a new user access without a redeploy.
- The full ticket workflow creates, comments, records the first response, resolves, and reopens.
- Upload validation rejects a disallowed file type.
- A knowledge article is hidden from customers while a draft and visible after publication.

## Browser walkthrough (Chrome)

- Administrator sign-in, dashboard metrics, ticket queue, ticket detail, and activity timeline.
- Internal note visible to the administrator and hidden from the customer.
- AI reply draft processed by the worker, labelled as assistive, with review and feedback controls.
- Customer sign-in shows only their tickets, no internal notes, no AI panel, and no administration navigation.
- Administrator creates a custom `Auditor` role, grants `tickets.view.all` and `reports.view.all`, creates a user with that role, and the new user sees the dashboard without administration access.
- No console errors or warnings were reported during the walkthrough.

## Known limitations

- Rate limiting is in-memory per instance.
- Data Protection keys are persisted in a Docker volume for the demo; production should use a shared key store.
- Live AI providers require configuration and are not exercised by automated tests; the deterministic fake provider is the default.
- Multi-tenancy, inbound email parsing, and email address verification are post-MVP.
