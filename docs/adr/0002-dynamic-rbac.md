# ADR 0002: Database-Driven Roles and Permissions

- **Status:** Accepted
- **Context:** Administrators must create roles and grant permissions at runtime without a deployment.
- **Decision:** Store permissions in the database, link them to Identity roles through `RolePermissions`, and materialize granted permission names as claims for the signed-in principal. Endpoints authorize against permission names through a dynamic policy provider. Role and permission mutations update affected users' security stamps so cookie validation refreshes their claims.
- **Consequences:** Authorization stays consistent with database configuration and does not depend on hardcoded role names. Security-stamp refresh interval controls how quickly revoked permissions take effect. Permission changes are audited and must invalidate affected sessions.

**Rejected:** Hardcoded role checks such as `[Authorize(Roles = "Agent")]` alone. They cannot satisfy runtime permission administration.
