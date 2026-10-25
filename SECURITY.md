# Security Notes

This project applies the OWASP ASVS-style controls below. Security-relevant behavior is covered by tests where practical.

## Authentication and session management

- Browser sessions use ASP.NET Core Identity with an HttpOnly, SameSite cookie. JavaScript never reads the credential.
- `Secure` cookies are enabled by default and disabled only for local HTTP demonstrations through explicit configuration.
- Sign-in uses a generic failure message, lockout after five failed attempts for fifteen minutes, and a rate limit of ten attempts per minute per client address.
- Security-stamp validation refreshes a session within two minutes of a permission or account change.
- Self-registration is disabled. Administrators create accounts and assign roles.

## Authorization

- Authorization is deny-by-default: the API has no anonymous endpoints except health checks, the anti-forgery token endpoint, sign-in, and the development OpenAPI document.
- Endpoints declare a database permission through `[HasPermission(...)]`; use cases re-check permissions and ownership before acting.
- Customers can read only their own tickets and never see internal notes, internal activities, or internal attachments.
- Role and permission changes are stored in PostgreSQL, audited, and invalidate affected users' security stamps.
- System roles cannot be renamed or deleted, and the Administrator role must retain role and permission management.

## Input, output, and data access

- All data access uses EF Core parameterized queries. Raw SQL is limited to a fixed sequence call with no user input.
- DTOs use data annotations and explicit length limits; invalid input returns a problem response without internal detail.
- Knowledge search uses PostgreSQL full-text search with a parameterized query term.
- HTML in stored content is encoded on output; the frontend renders text nodes rather than raw HTML.
- Uploads are limited to 10 MB, checked by extension, declared content type, and file signature, stored under random names outside the web root, and served only through an authorized endpoint.

## Cross-site request forgery and browser protections

- Every unsafe request requires the `X-CSRF-TOKEN` anti-forgery header validated by ASP.NET Core.
- SameSite cookies, a restrictive CORS policy (empty by default), and explicit allowed origins when configured.
- Responses include `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and cross-origin isolation headers.
- HSTS is enabled outside development unless explicitly disabled for a controlled environment.

## Secrets, logging, and AI

- Secrets come from environment variables or a secret store. `.env` is ignored by Git and only `.env.example` is committed.
- Logs avoid ticket bodies, credentials, and personal data; email logging records routing metadata only.
- AI providers receive only authorized content, are instructed to treat ticket text as untrusted data, must cite knowledge articles, and never send replies autonomously.
- AI output is stored with provider, model, prompt version, input revision, and latency for review and evaluation.

## Operations

- Background work uses a transactional outbox with idempotency keys, bounded retries, and visible failure state.
- Health endpoints distinguish liveness from database readiness.
- Containers run as a non-root user; PostgreSQL and file volumes are separate.
- Dependency updates and container image builds run in CI. Add secret scanning and image scanning before production use.

## Known limitations for production hardening

- Inbound email parsing, multi-tenancy, and email address verification are post-MVP.
- Rate limits are in-memory and per instance; use a distributed limiter when scaling horizontally.
- The demonstration uses HTTP behind a reverse proxy; production must terminate TLS and keep secure cookies enabled.
- Audit records are append-only in the application but not tamper-evident; consider hash chaining or write-once storage for regulated environments.
