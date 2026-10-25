# ADR 0001: Use PostgreSQL as the Primary Datastore

- **Status:** Accepted
- **Context:** Tickets, comments, assignments, SLA policies, audit events, and knowledge links are strongly relational. Queue filtering, concurrency control, and reporting aggregates require transactions and joins.
- **Decision:** Use PostgreSQL with EF Core and Npgsql. Use relational tables for the domain, JSONB only for genuinely variable metadata, PostgreSQL full-text search for knowledge search, and optional `pgvector` only when semantic retrieval is measured as necessary.
- **Consequences:** Migrations and constraints provide a clear consistency model. Reporting stays in one datastore. Schema changes require migrations, and full-text search behavior is PostgreSQL-specific.

**Rejected:** MongoDB as the primary store. It would add different transaction and reporting patterns without clear benefit for this domain.
