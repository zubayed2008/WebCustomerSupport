# AI-Powered Customer Support Desk — Implementation Plan

**Status:** Phases 0-5 implemented in this repository  
**Product plan:** [Project plan](project.md)  
**Operations:** [Runbook](docs/operations.md) · [Security notes](SECURITY.md) · [API summary](docs/api.md)

## 1. Architecture Decisions

### Platform

- **Backend:** .NET 10 LTS, ASP.NET Core Web API, ASP.NET Core Identity, and EF Core with Npgsql.
- **Database:** A supported PostgreSQL release pinned consistently in local containers and deployment environments.
- **Frontend:** React and TypeScript, served behind the same production origin as the API.
- **Async work:** A .NET worker for SLA evaluation, notification delivery, and AI jobs; persist job state so retries are observable and idempotent.
- **Integrations:** Interfaces for email, object storage, and AI providers, with local development implementations.
- **Realtime:** SignalR only after the core workflow is stable; polling remains a valid MVP fallback.

### Why PostgreSQL instead of MongoDB

Tickets have requesters, assignees, comments, tags, SLA policies, article links, and activity records. Those relationships, concurrent queue updates, SLA calculations, and dashboard aggregates are a strong relational fit. EF Core migrations and PostgreSQL constraints provide a clear consistency model. Use PostgreSQL full-text search for the initial knowledge base, JSONB only for genuinely variable metadata, and optional `pgvector` for semantic retrieval. MongoDB would not provide enough additional value to justify different transaction and reporting patterns.

### Application shape

Start as a **modular monolith**, not microservices. Separate domain modules by responsibility while sharing one deployment and one PostgreSQL database:

- Identity and access
- Tickets and collaboration
- SLA and calendars
- Knowledge base
- Notifications
- Reporting
- AI assistance

Use a worker for slow or retryable operations. Keep email, files, and AI outside the request transaction; commit the business change first, then enqueue work through a reliable outbox or equivalent persisted mechanism.

## 2. Proposed Solution Layout

```text
SupportDesk.sln
├── src/
│   ├── SupportDesk.Api/           # HTTP endpoints, auth, SignalR, API validation
│   ├── SupportDesk.Application/   # Use cases, authorization policies, DTOs
│   ├── SupportDesk.Domain/        # Entities, state transitions, SLA rules, events
│   ├── SupportDesk.Infrastructure/# EF Core, PostgreSQL, email/files/AI adapters
│   ├── SupportDesk.Worker/        # SLA, notification, and AI background jobs
│   └── supportdesk-web/           # React/TypeScript application
├── tests/
│   ├── SupportDesk.UnitTests/
│   ├── SupportDesk.IntegrationTests/
│   └── SupportDesk.E2ETests/
├── deploy/                        # Compose and deployment configuration
├── docs/                          # Decisions, API notes, runbooks, demo script
└── seed/                          # Repeatable demo fixtures and reset entry point
```

Avoid a generic repository over EF Core. Keep business transitions in application/domain services, use EF Core directly for persistence, and put authorization checks in each use case rather than relying only on controller attributes.

## 3. Core Data Model

| Entity/table | Important fields and relationships |
|---|---|
| Users, roles | Identity data, active state, display name, role membership. |
| Tickets | Readable number, requester, assignee, category, priority, status, subject, timestamps, SLA deadlines, concurrency token. |
| TicketComments | Ticket, author, body, public reply or internal note, creation time; never expose internal notes to customers. |
| TicketAttachments | Ticket/comment, storage key, original filename, content type, size, uploader, scan state. |
| TicketActivities | Ticket, actor, event type, old/new values, timestamp, correlation ID; append only. |
| Categories, Tags, TicketTags | Controlled classification and many-to-many tagging. |
| SlaPolicies | Priority, response target, resolution target, due-soon threshold, calendar, active version. |
| BusinessCalendars, Holidays | Time zone, weekly working intervals, and holiday exclusions. |
| KbArticles | Slug, title, body, status, visibility, author, reviewer, publication and revision timestamps. |
| ArticleTicketLinks | Many-to-many relationship supporting reuse and retrieval evidence. |
| Notifications, OutboxMessages | Recipient, type, payload reference, delivery state, attempts, timestamps, and idempotency key. |
| AiSuggestions | Ticket, kind, provider/model, prompt version, input revision, output, citations, status, latency, feedback. |

### Database rules

- Use database-generated IDs plus a unique, human-readable ticket number; never base authorization solely on predictable numbers.
- Add foreign keys, required-field checks, unique constraints, and appropriate status checks.
- Index queue filters such as `(status, priority, created_at)`, `assignee_id`, `requester_id`, SLA deadlines, and activity/comment ticket IDs. Validate indexes against query plans.
- Use an EF concurrency token for conflicting agent updates. Return a clear conflict response instead of silently overwriting.
- Store timestamps in UTC and the SLA calendar's IANA time zone. Calculate business time with tested timezone-aware code.
- Prefer immutable activities and SLA event history. Do not destructively rewrite past deadlines when a policy changes.

## 4. API and Workflow Contract

Use versioned JSON endpoints such as:

```text
POST   /api/v1/tickets
GET    /api/v1/tickets?status=open&assignee=me&page=1
GET    /api/v1/tickets/{id}
POST   /api/v1/tickets/{id}/comments
POST   /api/v1/tickets/{id}/assignment
POST   /api/v1/tickets/{id}/transitions
POST   /api/v1/tickets/{id}/attachments
POST   /api/v1/tickets/{id}/ai-suggestions
GET    /api/v1/reports/overview
GET    /api/v1/kb/articles
POST   /api/v1/kb/articles
```

Return consistent validation/problem details, enforce pagination limits, and use ETags or a concurrency value for updates. Document the API with OpenAPI. Define one ticket state machine shared by API validation and tests:

```text
New -> Open -> PendingCustomer | PendingInternal -> Resolved -> Closed
  \---------------- Resolved/Closed -> Open (reopen, audited) ------------/
```

A public agent reply records first response once. Resolution time pauses only in `PendingCustomer`; first-response measurement never pauses once achieved. Define who may reopen and the reopen window before implementation.

## 5. Delivery Phases

### Phase 0 — Foundation and Walking Skeleton

**Build**

- Create the solution, projects, frontend, formatting rules, environment-specific settings, and README.
- Add Docker Compose for PostgreSQL, a local email sink, and optional local object storage; never commit secrets.
- Configure EF Core/Npgsql, structured logging, health checks, OpenAPI, and React-to-API communication.
- Add CI for restore, build, unit/integration tests, frontend checks, and migration validation.
- Establish ADRs for authentication, time handling, file storage, error responses, and background processing.

**Exit gate:** A fresh clone can start the full stack with documented commands, reach a health-checked API from the UI, apply a migration, and pass CI.

### Phase 1 — Identity, Domain, and Persistence

**Build**

- Configure ASP.NET Core Identity and Customer, Agent, Manager, and Administrator roles.
- Use secure same-origin HttpOnly cookies for the browser, anti-forgery controls, login throttling, and safe account administration.
- Model tickets, comments, activities, categories, tags, attachments, and concurrency behavior.
- Add initial EF migrations, indexes, constraints, seed users, categories, and realistic ticket fixtures.
- Implement resource authorization for ticket ownership and field-level separation of public replies from internal notes.

**Test first**

- Customer cannot read or mutate another requester's ticket by changing an ID.
- Customer DTOs, exports, and notifications never include internal notes.
- Invalid transitions and inactive-user access fail; concurrency conflicts return a usable response.

**Exit gate:** Authentication, authorization, migrations, and the core ticket lifecycle have automated coverage and repeatable seed data.

### Phase 2 — Ticket Workspace and Customer Portal

**Build**

- Implement server-paginated queues, search, filters, sorting, ticket details, and the activity timeline.
- Add creation, assignment/claiming, priority/category/tag updates, replies, internal notes, resolution, closure, and reopening.
- Build responsive customer and agent screens with clear empty, loading, validation, forbidden, conflict, and failure states.
- Store attachment metadata in PostgreSQL and bytes behind a storage interface. Enforce size/extension/MIME limits and authorize every download.
- Audit important changes in the same transaction as the mutation; enqueue notifications through the outbox.
- Add basic in-app notifications and outbound email templates without processing email inside the web request.

**Exit gate:** The complete non-AI ticket journey works across customer and agent roles, survives concurrent updates, and leaves an understandable audit trail.

### Phase 3 — SLA Engine and Reliable Background Work

**Build**

- Implement priority-based policy versions, weekly business calendars, holidays, IANA time zones, and warning thresholds.
- Calculate and store first-response/resolution commitments; record first response, pauses, resume events, breaches, and resolution cycles.
- Add an injectable clock and a deterministic SLA service independent of ASP.NET and EF Core.
- Build worker schedules for due-soon/overdue processing and outbox dispatch, with leases, bounded retries, backoff, dead-letter visibility, and idempotency keys.
- Build manager SLA configuration and agent due-soon/overdue queues. Ensure reports derive live breach state correctly even between worker runs.

**Test first**

- Weekend, holiday, overnight, month-end, leap-day, and daylight-saving calendar cases.
- Pending-customer pause/resume, reopening, policy changes, duplicate job execution, and worker restart recovery.

**Exit gate:** SLA fixtures match hand-calculated results, notifications do not duplicate under retry tests, and failed jobs are visible and recoverable.

### Phase 4 — Knowledge Base and Operational Reporting

**Build**

- Implement article drafting, review/publish/archive permissions, revisions, visibility, slugs, and ticket links.
- Add PostgreSQL full-text search with ranking and authorized visibility filters. Keep article revisions available for audit.
- Add the create-article-from-resolution flow without copying internal notes or private customer content automatically.
- Implement dashboard queries for volume, queues, due-soon/overdue items, first response, resolution cycles, and agent workload.
- Verify dashboard totals against known fixtures and use `EXPLAIN` on queue, search, and reporting queries before adding indexes.
- Add SignalR ticket/queue updates if time permits, with authorization on connection and group membership and a polling fallback.

**Exit gate:** Knowledge search is permission-safe, published content can be reused from tickets, and every chart reconciles with the same underlying records.

### Phase 5 — AI Assistance with Human Review

**Build**

- Define an AI provider interface and a deterministic fake provider for development, tests, and offline demos.
- Process classification, summarization, retrieval, and reply drafting asynchronously; show queued, running, failed, stale, and completed states.
- Start retrieval with authorized PostgreSQL full-text search. Add `pgvector` embeddings only after measuring a need for semantic retrieval.
- Return structured suggestion outputs with a schema version, ticket input revision, prompt version, model/provider metadata, citations, and latency.
- Require agents to review and edit drafts before converting them into normal public replies. Do not create an autonomous send path.
- Add per-user/IP rate limits, request timeouts, token/cost telemetry, bounded retries, and feature flags.
- Treat ticket and article text as untrusted instructions, restrict retrieved content by role, and never place secrets or unnecessary personal data in prompts.
- Build a small fixed evaluation set for classification accuracy, citation validity, internal-note leakage, unsupported claims, and provider failure.

**Exit gate:** AI adds value in the demo, citations are inspectable, stale suggestions are marked, sensitive-content tests pass, and disabling AI leaves every manual workflow intact.

### Phase 6 — Hardening, Deployment, and Demo Release

**Build and verify**

- Complete unit, PostgreSQL integration, API authorization, concurrency, accessibility, and browser end-to-end suites.
- Review OWASP risks, dependency vulnerabilities, security headers, cookies, CORS, CSRF, rate limits, uploads, HTML sanitization, and log redaction.
- Run an agreed demo-load test and investigate slow queries; do not claim unmeasured scalability.
- Add OpenTelemetry traces, useful metrics, readiness/liveness checks, correlation IDs, and operational dashboards without logging ticket bodies.
- Deploy API, worker, frontend, and managed/containerized PostgreSQL with environment-specific secrets and HTTPS.
- Run migrations as a controlled deployment step with rollback/forward-fix guidance; never silently reset production data.
- Configure automated PostgreSQL backups and perform a timed restore test. Document startup, job recovery, support, and incident runbooks.
- Finalize demo personas and fixtures for new, active, pending, due-soon, overdue, resolved, and reopened tickets.
- Rehearse the five-minute script and an offline path using the fake AI provider and local email sink.

**Exit gate:** A deployed release passes the critical-path suite, backup restoration and demo reset are proven, known limitations are documented, and the client demo works without manual database edits.

## 6. Verification Evidence

- `dotnet build SupportDesk.slnx` succeeds for all seven projects.
- Unit tests cover ticket transitions, SLA business-hours calculations (weekend, holiday, DST), permission catalog integrity, and upload validation.
- PostgreSQL integration tests use a disposable Testcontainers database, apply real migrations, seed data, perform the anti-forgery handshake, and verify customer isolation, internal-note filtering, dynamic role grants, workflow transitions, upload rejection, and knowledge publication.
- `docker compose up --build -d` builds and starts PostgreSQL, API, worker, and web; the API and web health checks report healthy.
- The knowledge-base search is backed by a PostgreSQL GIN full-text index created in the migration.

## 7. Post-MVP Backlog

Prioritize only after client feedback: real inbound email threading, multi-tenant isolation, satisfaction surveys, customer-organization accounts, advanced custom SLAs, localization, semantic-search improvements, live chat, additional channels, and data-warehouse exports. Do not let these delay the reliable core workflow.

## 7. Verification Strategy

| Layer | Required coverage |
|---|---|
| Domain unit tests | Ticket transitions, reopening, first response, SLA calendars and pauses, article visibility, and policy selection using a fixed clock. |
| PostgreSQL integration tests | Real migrations, constraints, transactions, outbox behavior, queue filters, full-text search, report totals, and concurrency. Use disposable PostgreSQL instances rather than an in-memory EF substitute. |
| API tests | Authentication, every role, resource ownership, internal-note filtering, validation/problem responses, pagination, upload/download authorization, and rate limits. |
| Worker tests | Duplicate delivery, retries, backoff, lease expiry, restart recovery, stale AI input, and poison-message handling. |
| Browser E2E tests | Customer submission and reply; agent assignment, note, response, and resolution; manager SLA and dashboard checks; KB publication; AI draft review. |
| Non-functional checks | Keyboard/accessibility review, responsive layouts, dependency and secret scanning, security tests, agreed load profile, and backup restoration. |
| AI evaluation | Versioned fixtures, deterministic fake-provider contract tests, citation checks, leakage tests, human quality review, and explicit failure fallback. Live-model output must not make ordinary CI nondeterministic. |

Run fast checks on every change and the full PostgreSQL/API/E2E suite before a release. Store the seed fixture expected values with reporting tests so dashboard correctness is demonstrable.

## 8. Vibe-Coding Execution Rules

Use AI to accelerate small, inspectable slices rather than generating the entire product in one prompt:

1. Give the agent this plan, the relevant requirement IDs, existing conventions, and acceptance criteria.
2. Ask for a short change plan and identify affected files, migrations, authorization rules, and tests before editing.
3. Implement one vertical slice, such as “customer creates a ticket,” across domain, API, UI, seed data, and tests.
4. Run formatters, builds, unit tests, real-PostgreSQL integration tests, and the relevant browser flow after each slice.
5. Review generated migrations, queries, authorization, state transitions, and error handling manually. Reject UI-only proof that business logic works.
6. Commit only working increments with no secrets, generated junk, disabled tests, or unexplained suppressions.
7. Update documentation and the demo fixtures whenever behavior changes.

### Recommended vertical-slice order

1. Health-checked walking skeleton and CI.
2. Login and role-aware landing pages.
3. Customer creates and views a ticket.
4. Agent queue, assignment, public reply, and internal note.
5. Status transitions, attachments, activity, and notifications.
6. SLA policies, deadlines, and risk queues.
7. Knowledge publication, search, and ticket links.
8. Reconciled operational dashboard.
9. AI classification, then summary/retrieval/drafting.
10. Hardening, deployment, restore rehearsal, and demo reset.

## 9. Release Checklist

- [ ] All must-have requirements in `project.md` are traceable to working features and tests.
- [ ] Fresh setup, migrations, seed, reset, and demo commands are documented and tested.
- [ ] No customer can access another customer's data or any internal note.
- [ ] SLA edge cases, concurrent updates, and background-job retries pass automated tests.
- [ ] Dashboard figures match fixture data and AI can be disabled without breaking support work.
- [ ] Security, accessibility, dependency, migration, load, and backup-restore checks are complete.
- [ ] Production configuration uses HTTPS and external secrets; logs exclude credentials and ticket content.
- [ ] The deployed demo has been rehearsed by someone other than the primary implementer.
- [ ] Known limitations and post-MVP items are clearly separated from delivered functionality.

The phase exit gates and this checklist define implementation completion; visual polish alone is not sufficient.

