# AI-Powered Customer Support Desk — Project Plan

**Status:** Phase 0-5 implemented, Phase 6 hardening recommended before production  
**Delivery target:** Client-demonstrable MVP  
**Related document:** [Implementation plan](implementation-plan.md)

## 1. Product Overview

Build a customer support desk where customers submit and track tickets, agents collaborate and respond, and managers control SLAs, knowledge, and operational reporting. AI assists with classification, summaries, knowledge retrieval, and draft replies, but people remain responsible for sending responses and resolving tickets.

### Technology decision

Use **.NET 10 LTS, ASP.NET Core, Entity Framework Core, and PostgreSQL**, with a **React/TypeScript** frontend. PostgreSQL is preferred over MongoDB because the core domain is relational, ticket transitions require consistent transactions, and management reporting relies on joins and aggregates. PostgreSQL full-text search and optional `pgvector` support the knowledge base without adding another datastore.

### Assumptions

- This is a greenfield, single-organization MVP, not a multi-tenant SaaS launch.
- The MVP intake channels are the customer portal and an authenticated API. Real inbound mailbox parsing is post-MVP; outbound email notifications are included.
- AI is an assistive, feature-flagged enhancement. Core support workflows must work when the AI provider is unavailable.
- Dates and duration estimates will be added after team size and availability are known; milestones below use objective exit criteria.

## 2. Goals and Success Measures

1. Let a customer move from ticket submission to resolution without staff using manual database changes.
2. Give agents one reliable workspace for queues, conversations, internal notes, attachments, and related knowledge.
3. Make SLA risk visible before breaches occur and preserve an auditable history of important actions.
4. Give managers accurate live operational metrics calculated from application data.
5. Demonstrate useful AI with human review, citations for retrieved articles, and a manual fallback.

### MVP acceptance measures

- A customer can create a ticket, receive a readable ticket number, reply, upload an attachment, and track its status.
- Customers cannot access another customer's tickets or internal notes through the UI or API.
- Agents can filter queues, claim or assign tickets, respond, add internal notes, and resolve or reopen tickets according to workflow rules.
- Configured SLA policies produce testable first-response and resolution deadlines, including business-hours behavior.
- Managers can view open, due-soon, overdue, resolved, and agent-workload metrics from real ticket data.
- Every AI draft is clearly labeled, reviewable, and editable; sending always requires an authorized user.
- The five-minute demo can be reset and repeated using seeded data.

## 3. Users and Permissions

| Capability | Customer | Agent | Manager | Administrator |
|---|---:|---:|---:|---:|
| View and reply to own tickets | Yes | Yes | Yes | Yes |
| View team queues | No | Yes | Yes | Yes |
| View internal notes | No | Yes | Yes | Yes |
| Claim or update assigned tickets | No | Yes | Yes | Yes |
| Assign any ticket and override priority | No | No | Yes | Yes |
| Manage SLA policies and categories | No | No | Yes | Yes |
| Manage users and roles | No | No | No | Yes |
| View operational dashboards | Own summary | Yes | Yes | Yes |
| Publish knowledge-base articles | No | Draft only | Yes | Yes |

Authorization must be enforced in the API for every request. Hiding UI controls is not a security boundary.

## 4. Scope

### Must have for the MVP

| ID | Requirement |
|---|---|
| AUTH-01 | Secure login, logout, role assignment, and account management by an administrator. |
| TCK-01 | Create, list, search, filter, and view tickets with number, subject, description, requester, assignee, category, priority, tags, status, and timestamps. |
| TCK-02 | Public replies, private internal notes, attachments, and an immutable activity timeline. |
| TCK-03 | Controlled status transitions, assignment, claiming, resolution, and reopening. |
| TCK-04 | Server-side filtering and pagination for open, unassigned, assigned-to-me, due-soon, overdue, and resolved queues. |
| SLA-01 | Priority-based first-response and resolution targets with business-hours calendars and due-soon thresholds. |
| SLA-02 | Record first response, resolution, breach state, and relevant SLA pauses without rewriting history. |
| KB-01 | Draft, review, publish, archive, search, and link knowledge-base articles to tickets. |
| NTF-01 | In-app and outbound email notifications for replies, assignments, and status changes, with safe retry behavior. |
| AI-01 | Suggest category and priority, summarize a conversation, retrieve relevant articles, and draft a reply on demand. |
| RPT-01 | Live counts and trend views for ticket volume, SLA risk, resolution activity, and agent workload. |
| OPS-01 | Seed/reset scripts, structured logs, health checks, audit records, backups, and automated critical-path tests. |

### Should have if the MVP exit criteria remain satisfied

- SignalR updates for ticket lists and conversations.
- Saved agent filters and article feedback.
- Basic customer satisfaction rating after resolution.
- AI suggestion feedback and quality review dashboard.

### Explicitly out of scope for the MVP

- CRM, billing, accounting, LMS, telephony, live chat, and social-media channels.
- Multi-tenant administration, custom workflow designers, and a public plugin marketplace.
- Autonomous AI responses or automatic ticket closure.
- Native mobile applications; the portal must be responsive instead.
- Complex data warehousing and executive forecasting.

## 5. Key Business Rules

### Ticket lifecycle

Use `New`, `Open`, `PendingCustomer`, `PendingInternal`, `Resolved`, and `Closed`. Customers may reply to their own active tickets and reopen according to a configured reopen window. Agents may claim and work tickets; managers can reassign or override priority. Every transition, assignment, and priority change creates an activity record.

Internal notes are never included in customer responses, notifications, exports, or AI draft citations intended for customers. Resolved tickets retain their resolution timestamp; reopening and resolving again creates a new measurable resolution cycle rather than erasing history.

### SLA behavior

- SLA policy selection is based on priority and is recorded on the ticket.
- First response means the first public reply by an authorized support user, not an internal note or automated acknowledgement.
- First-response and resolution deadlines are calculated using the selected business calendar and stored in UTC.
- Resolution elapsed time pauses in `PendingCustomer` and resumes when work continues.
- Changing a policy affects new tickets by default; recalculating existing tickets requires an explicit, audited manager action.
- A scheduled job updates due-soon and overdue indicators, but reports must remain correct if the job is temporarily delayed.

### Knowledge and AI behavior

- Articles follow `Draft -> Published -> Archived`; only published, appropriately visible articles appear in customer search.
- AI suggestions apply to a specific ticket revision so users can tell when source content has changed.
- Retrieved articles must appear as citations; unsupported AI output is not verified knowledge.
- Prompts, model configuration, and article content are treated as untrusted input where prompt injection is possible.
- AI failures produce a retryable message and never block manual ticket handling.

## 6. Reporting Definitions

Define metrics before building charts so every screen uses the same rules:

- **Open tickets:** Active tickets not resolved or closed at the reporting instant.
- **Unassigned tickets:** Active tickets with no current assignee.
- **First-response time:** First valid public support response minus ticket creation.
- **Resolution time:** Elapsed working time across each resolution cycle, excluding `PendingCustomer` pauses.
- **Due soon:** Active ticket with an unmet SLA deadline inside its warning threshold.
- **Overdue:** Active ticket with an unmet SLA deadline in the past; achieved commitments remain historically compliant.
- **Agent workload:** Active assigned tickets grouped by agent, with due-soon and overdue breakdowns.

## 7. Non-Functional Requirements

- **Security:** Secure HttpOnly cookies for the same-origin browser app, anti-forgery protection where applicable, restrictive CORS, server-side authorization, rate limits, safe HTML rendering, upload validation, and secret management.
- **Privacy:** Minimize personal data, redact sensitive logs, define retention rules, and disable provider training on support content where supported.
- **Performance:** Target p95 under 500 ms for ordinary API reads/writes under an agreed demo load, excluding AI, email, and file transfers. Load-test before treating this as a commitment.
- **Accessibility:** Target WCAG 2.2 AA for core workflows, including keyboard operation, labels, focus management, and contrast.
- **Reliability:** Transactions for state changes, idempotent background processing, bounded retries, health checks, and tested backup restoration.
- **Observability:** Structured logs without message bodies or secrets, correlation IDs, exception tracking, and metrics for queues, SLA breaches, job failures, and AI usage.

## 8. Milestones and Deliverables

| Milestone | Main deliverable | Exit criteria |
|---|---|---|
| M0 — Foundation | Development environment and CI | App, API, worker, and PostgreSQL start locally; build and initial-test pipeline pass. |
| M1 — Secure domain | Identity, schema, and ticket lifecycle | Roles and ownership tests pass; migrations and basic seed data are repeatable. |
| M2 — Agent workflow | Usable customer and agent experience | A ticket moves through replies, notes, files, assignment, resolution, and reopening with audit history. |
| M3 — SLA and notifications | Operational control | Calendar tests, breach detection, idempotent retries, and visible SLA queues pass. |
| M4 — Knowledge and reports | Reusable content and live dashboard | Search respects visibility; dashboard values match known fixture results. |
| M5 — AI assistance | Reviewable suggestions with fallback | Classification, summary, citations, and drafting work with a test provider and fail safely when it is unavailable. |
| M6 — Demo release | Deployed, repeatable client demo | Security, accessibility, integration, and end-to-end checks pass; restore and demo reset are rehearsed. |

Each milestone produces a runnable vertical slice. Do not wait until M6 to integrate the UI, API, and database.

## 9. Client Demo Script

1. Sign in as a customer and submit a ticket with an attachment.
2. Sign in as an agent, locate it in the unassigned queue, and inspect the due-soon SLA.
3. Request AI classification and a grounded draft; edit the draft before sending it.
4. Add an internal note and demonstrate that the customer cannot see it.
5. Link a knowledge-base article, resolve the ticket, and publish a reusable article from the solution.
6. Sign in as a manager and show updated workload, resolution, and SLA views.
7. Disable the AI test provider and show that manual ticket handling still works.

## 10. Main Risks and Responses

| Risk | Response |
|---|---|
| SLA calculations fail around weekends or daylight-saving changes | Centralize time rules, use an injectable clock, and build calendar-boundary tests before UI indicators. |
| Internal notes or other customers' tickets leak | Test object-level and field-level authorization; use separate customer DTOs. |
| Concurrent agents overwrite updates | Use concurrency tokens, explicit conflict responses, and assignment tests. |
| Duplicate email or background-job execution | Use an outbox, idempotency keys, bounded retries, and delivery-state history. |
| AI invents answers or exposes private content | Ground drafts in authorized articles, show citations, require review, redact logs, and evaluate fixed fixtures. |
| Demo depends on external services | Provide local email/file adapters, an AI stub, seeded data, and an offline demo path. |
| Scope expands into a CRM or omnichannel suite | Enforce the out-of-scope list and move additions to a post-MVP backlog. |

## 11. Definition of Done

A feature is done only when its authorization and validation rules are implemented, success and failure paths are usable, relevant audit activity exists, automated tests pass, accessibility is checked, documentation and seed data are updated, and no sensitive data appears in logs. The MVP is done when all must-have requirements and M6 exit criteria are met, not merely when the demo screens render.
