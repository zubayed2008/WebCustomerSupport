# Five-Minute Client Demonstration Script

## Preparation

1. Copy `.env.example` to `.env` and set strong values for `POSTGRES_PASSWORD`, `SUPPORTDESK_ADMIN_PASSWORD`, and `SUPPORTDESK_DEMO_PASSWORD`.
2. Run `docker compose up --build -d` and wait until `docker compose ps` shows healthy services.
3. Open `http://localhost:8080` and confirm the seeded tickets are visible after signing in as the administrator.
4. Keep the worker logs open with `docker compose logs -f worker`.

## Demonstration Steps

1. **Customer intake** — Sign in as `customer@example.com`. Open Tickets, then create a ticket describing a blocked month-end export. Show that the ticket receives a readable number and SLA deadlines.
2. **Agent queue** — Sign in as `agent@example.com`. Open Tickets, filter the unassigned queue, claim the new ticket, and point out the due-soon indicator.
3. **AI assistance with review** — In the ticket, request a classification and a reply draft. Show the generated text, the citations, and that the draft must be copied or edited by the agent before sending.
4. **Collaboration and audit** — Add a public reply, then add an internal note. Show the activity timeline. Sign back in as the customer and demonstrate that the internal note is not visible.
5. **SLA behaviour** — Move the ticket to Pending Customer, then reply as the customer to resume it. Point out the paused business minutes and the recalculated resolution commitment.
6. **Knowledge reuse** — Link a published knowledge article to the ticket, resolve it, then open the knowledge base and publish a reusable article.
7. **Management view** — Sign in as `manager@example.com`. Show the operational dashboard: open, unassigned, due-soon, overdue, resolution time, agent workload, and the 14-day trend.
8. **Dynamic administration** — Sign in as the administrator. Create a role such as `Auditor`, grant only `tickets.view.all` and `reports.view.all`, create a user with that role, then sign in as that user and show that the queue and dashboard are available while administration is not.
9. **Safe fallback** — Set `SUPPORTDESK_AI_PROVIDER=Fake` (the default) or stop the AI provider and show that ticket handling still works without AI.

## Talking Points

- Roles and permissions are stored in PostgreSQL and enforced on every API endpoint.
- Customers can only reach their own tickets; internal notes never leave the support side.
- SLA deadlines use business calendars with time zones, weekends, and holidays.
- Attachments are validated by extension, declared content type, and file signature, stored outside the web root, and downloaded only through an authorized endpoint.
- Email and AI work is queued through an outbox with retries, so failures do not break ticket handling.
