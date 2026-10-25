# ADR 0004: Transactional Outbox and Background Worker

- **Status:** Accepted
- **Context:** Email delivery, SLA evaluation, and AI generation must not run inside the HTTP request. Naive background work can lose notifications on failure or duplicate them on retry.
- **Decision:** Persist outbox rows in the same transaction as the business change. A worker leases pending rows, delivers them with bounded exponential retries, records the last error, and moves exhausted messages to a failed state. SLA evaluation and AI processing run in the same worker on independent intervals. Idempotency keys prevent duplicate notifications for the same event.
- **Consequences:** Delivery is eventually consistent and observable. Operators can inspect failed messages and retry them. The worker must be deployed alongside the API, and duplicate-safe handlers are required for any new outbox message type.
