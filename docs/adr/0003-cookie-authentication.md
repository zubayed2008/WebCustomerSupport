# ADR 0003: Same-Origin Cookie Authentication with Anti-Forgery Tokens

- **Status:** Accepted
- **Context:** The React application is served from the same origin as the API in production. Tokens stored in JavaScript are exposed to cross-site scripting, and unprotected cookie-authenticated writes are exposed to cross-site request forgery.
- **Decision:** Authenticate browser sessions with an HttpOnly, SameSite cookie issued by ASP.NET Core Identity. Require the anti-forgery header on every unsafe request. Return 401/403 JSON instead of redirects so the API behaves consistently. Keep `Secure` enabled by default and disable it only for local HTTP demonstrations through explicit configuration.
- **Consequences:** The browser never handles credentials directly, and CSRF is mitigated. The frontend must fetch the anti-forgery token before the first write, which the API client handles automatically. Cross-origin deployments must configure allowed origins explicitly and use HTTPS.
