# WebCustomerSupport

AI-assisted customer support desk built with .NET 10, PostgreSQL, and React. Customers submit and track tickets, agents collaborate with public replies and internal notes, managers control SLAs and knowledge, and administrators manage roles and permissions dynamically.

Planning and operations documents:

- [Project plan](project.md)
- [Implementation plan](implementation-plan.md)
- [Ideas catalogue](project-idea.md)
- [API summary](docs/api.md)
- [Operations runbook](docs/operations.md)
- [Verification record](docs/verification.md)
- [Security notes](SECURITY.md)
- [Five-minute demo script](docs/demo-script.md)
- Architecture decisions: [PostgreSQL](docs/adr/0001-use-postgresql.md), [dynamic RBAC](docs/adr/0002-dynamic-rbac.md), [cookie authentication](docs/adr/0003-cookie-authentication.md), [outbox worker](docs/adr/0004-outbox-and-workers.md)

## Capabilities

- Customer portal and authenticated ticket intake.
- Agent queues with assignment, claiming, priorities, categories, and tags.
- Public replies, internal notes, attachments, and an append-only activity timeline.
- Business-hours SLA policies with first-response and resolution deadlines.
- Knowledge base with publication workflow and PostgreSQL full-text search.
- Live operational reporting reconciled with ticket data.
- Human-reviewed AI classification, summary, knowledge retrieval, and reply drafting.
- Dynamic role and permission administration. Permissions are database-driven and enforced in the API.

## Security Baseline

- Same-origin HttpOnly, SameSite cookies and anti-forgery tokens for browser requests.
- Server-side permission authorization on every endpoint and object-level ownership checks.
- Rate limiting on authentication, intake, and AI endpoints.
- Upload allowlist, size limits, signature checks, random storage names, and authorized downloads.
- Security headers, strict CORS, parameterized EF Core queries, and safe output encoding.
- Secrets supplied through environment variables; `.env` is ignored by Git.
- Audit activity for ticket, role, permission, and knowledge changes.
- AI content is untrusted input, grounded in authorized articles, and never sent without human review.

## Local Development

Prerequisites: .NET 10 SDK, Node 24+, Docker (for PostgreSQL).

```bash
# 1. Create your local environment file
cp .env.example .env
# Edit .env and replace every placeholder password.

# 2. Start PostgreSQL only
docker compose up -d postgres

# 3. Restore tools, packages, and build
dotnet tool restore
dotnet restore SupportDesk.slnx
dotnet build SupportDesk.slnx

# 4. Run the API (applies migrations and seeds when Database:AutoMigrate is true).
#    Provide the seed credentials through environment variables.
set Seed__AdminEmail=admin@example.com
set Seed__AdminPassword=<your-long-random-password>
set Seed__DemoPassword=<your-long-random-password>
set ConnectionStrings__SupportDesk=Host=localhost;Port=5432;Database=supportdesk;Username=supportdesk;Password=<postgres-password>
dotnet run --project src/SupportDesk.Api

# 5. Run the worker in another terminal (same connection string environment variable)
dotnet run --project src/SupportDesk.Worker

# 6. Run the frontend in another terminal
cd src/supportdesk-web
npm install
npm run dev
```

The Vite development server proxies `/api` to `http://localhost:5080`.

## Containerized Demonstration

```bash
cp .env.example .env
# Edit .env before continuing.
docker compose up --build -d
docker compose ps
```

Open `http://localhost:8080` and sign in with `SUPPORTDESK_ADMIN_EMAIL` and `SUPPORTDESK_ADMIN_PASSWORD` from your local `.env`. When `SUPPORTDESK_DEMO_PASSWORD` is set, the stack also seeds `manager@example.com`, `agent@example.com`, and `customer@example.com` with that password for the demonstration.

Never commit `.env`. Use unique long random values for every password, and change them before exposing the stack to anyone outside your machine.

### Local demonstration accounts

The values below are the **local-only demo credentials** currently used by this machine's ignored `.env` file. They are published here only so the demonstration can be reproduced. They must never be used outside a local machine.

| Role | Email | Password | Controlled by |
|---|---|---|---|
| Administrator | `admin@example.com` | `Admin!Passw0rd#2026` | `SUPPORTDESK_ADMIN_PASSWORD` |
| Manager | `manager@example.com` | `Demo!Passw0rd#2026` | `SUPPORTDESK_DEMO_PASSWORD` |
| Agent | `agent@example.com` | `Demo!Passw0rd#2026` | `SUPPORTDESK_DEMO_PASSWORD` |
| Customer | `customer@example.com` | `Demo!Passw0rd#2026` | `SUPPORTDESK_DEMO_PASSWORD` |

The PostgreSQL superuser for the local container is `supportdesk` / `Postgres!Passw0rd#2026`, controlled by `POSTGRES_PASSWORD`.

Because the README is public, treat these passwords as compromised as soon as they are committed. Before any client-facing or shared environment, replace all of them in `.env`, then recreate the stack so the database and seeded users are rebuilt with the new secrets:

```bash
docker compose down -v
docker compose up --build -d
```

Useful commands:

```bash
docker compose logs -f api worker
docker compose ps
docker compose down
docker compose down -v   # destructive: removes database and file volumes
```

## Tests

```bash
dotnet test SupportDesk.slnx
```

- Unit tests cover domain transitions, SLA calendar behavior (including weekends, holidays, and daylight saving), permissions, and upload validation.
- Integration tests use a disposable PostgreSQL container through Testcontainers, apply real migrations, complete the anti-forgery handshake, and verify customer isolation, internal-note filtering, dynamic role grants, workflow transitions, upload rejection, and knowledge publication. Docker must be running.
- Frontend checks run with `npm run lint` and `npm run build`.
- The containerized stack is verified with `docker compose ps`, the web health endpoint, and a browser walkthrough of the demo script.

## Configuration

| Variable | Purpose |
|---|---|
| `POSTGRES_*` | Database name, user, and password. |
| `SUPPORTDESK_ADMIN_EMAIL` / `SUPPORTDESK_ADMIN_PASSWORD` | Initial administrator created only when no administrator exists. |
| `SUPPORTDESK_AUTO_MIGRATE` | Applies migrations at API startup. Use only for controlled environments. |
| `SUPPORTDESK_AI_PROVIDER` | `Fake` for deterministic local behavior or `OpenAi` for a configured compatible endpoint. |
| `SUPPORTDESK_AI_API_KEY` / `SUPPORTDESK_AI_BASE_URL` / `SUPPORTDESK_AI_MODEL` | Live AI provider configuration. |
| `SUPPORTDESK_PUBLIC_URL` | Base URL used in notification links. |

## Repository Layout

```text
src/SupportDesk.Api              HTTP API, authentication, security middleware
src/SupportDesk.Application      Use cases, authorization, DTOs, abstractions
src/SupportDesk.Domain           Entities, workflow, SLA, and permission rules
src/SupportDesk.Infrastructure   EF Core, PostgreSQL, files, email, AI providers
src/SupportDesk.Worker           SLA, outbox, notification, and AI background jobs
src/supportdesk-web              React and TypeScript application
tests                            Unit, integration, and end-to-end tests
docs/adr                         Architecture decision records
```

## Phase Status

- **Phase 0 — Foundation:** solution, Compose environment, health checks, OpenAPI (development), CI, and documentation.
- **Phase 1 — Identity, domain, and persistence:** dynamic roles and permissions, EF Core model, migrations, constraints, seed data, and ownership authorization.
- **Phase 2 — Ticket workspace:** customer portal, agent queues, comments, internal notes, attachments, workflow transitions, audit activity, and notifications.
- **Phase 3 — SLA and background work:** business calendars, SLA policies, breach detection, transactional outbox, retries, and the worker host.
- **Phase 4 — Knowledge and reporting:** article lifecycle, PostgreSQL full-text search, ticket links, and a reconciled operational dashboard.
- **Phase 5 — AI assistance:** provider abstraction, deterministic fake provider, optional OpenAI-compatible provider, grounded citations, queued processing, stale detection, review, and feedback.

Phase 6 items still recommended before a production launch: multi-replica rate limiting, external secret management, TLS termination, secret and image scanning, load testing, and a rehearsed restore in the target environment.
