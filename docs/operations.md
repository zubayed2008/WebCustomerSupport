# Operations Runbook

## Startup order

1. PostgreSQL starts and passes its readiness check.
2. The API applies migrations when `Database:AutoMigrate=true`, then seeds permissions, system roles, the initial administrator, reference data, and optional demo content.
3. The worker starts after the API is healthy. It processes the outbox, SLA evaluation, and queued AI suggestions.
4. The web container serves the React application and proxies `/api` to the API.

## Routine commands

```bash
docker compose ps
docker compose logs -f api worker
docker compose restart api worker
docker compose down
docker compose down -v   # destructive: removes database and attachment volumes
```

## Database migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/SupportDesk.Infrastructure --startup-project src/SupportDesk.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/SupportDesk.Infrastructure --startup-project src/SupportDesk.Api
```

- Apply migrations as a controlled deployment step in production. Keep `Database:AutoMigrate=false` there.
- Review every generated migration, especially column drops and data transformations.
- Back up the database before applying a migration that changes or removes data.

## Backup and restore

```bash
docker compose exec -T postgres pg_dump -U supportdesk -d supportdesk -Fc > backup.dump
docker compose exec -T postgres pg_restore -U supportdesk -d supportdesk --clean --if-exists < backup.dump
```

Rehearse the restore into a scratch database before relying on it.

## Outbox and failed notifications

Failed messages remain in `OutboxMessages` with `Status='Failed'`, the attempt count, and the last error. Investigate the cause, fix the email configuration, then requeue:

```sql
UPDATE "OutboxMessages"
SET "Status" = 'Pending', "Attempts" = 0, "NextAttemptAt" = now(), "LastError" = NULL
WHERE "Status" = 'Failed';
```

## SLA monitoring

- The worker marks response and resolution breaches and emits due-soon or overdue notifications.
- Notification idempotency keys include the deadline, so retries and repeated scans do not duplicate messages.
- Reports also evaluate current deadlines, so a delayed worker cannot make the dashboard report stale breach state.

## AI provider

- `Ai:Provider=Fake` is deterministic and used for tests and offline demonstrations.
- `Ai:Provider=OpenAi` requires `Ai:BaseUrl`, `Ai:ApiKey`, and `Ai:Model`. Content is treated as untrusted input, citations are restricted by permission, and no reply is sent automatically.
- Disable or misconfigure the provider and verify that manual ticket handling still works.

## Rate limits and scaling

- Rate limits are in-memory per instance: 300 requests per minute globally, 10 sign-in attempts per minute per address, and 20 AI requests per minute per user.
- Replace the in-memory limiter with a distributed implementation before running multiple API replicas behind a load balancer.
- The worker uses database leases for outbox messages, so multiple worker replicas can run safely, but keep the batch size conservative.

## Account lockout and sign-in problems

ASP.NET Core Identity locks an account for 15 minutes after five failed sign-in attempts. While locked, even the correct password returns `401` with the message “The account is temporarily locked. Try again later.”

Recovery options:

1. Wait 15 minutes and sign in again.
2. As an administrator, open **Administration → Users** and use **Reset password**. A password reset also clears the lockout and the failed-attempt counter. Reactivating a deactivated user clears the lockout too.
3. Directly in the database during local development:

```sql
UPDATE "AspNetUsers"
SET "LockoutEnd" = NULL, "AccessFailedCount" = 0
WHERE "Email" = 'customer@example.com';
```

Run it through the container:

```bash
docker compose exec -T postgres psql -U supportdesk -d supportdesk -c "update \"AspNetUsers\" set \"LockoutEnd\"=null, \"AccessFailedCount\"=0 where \"Email\"='customer@example.com';"
```

If a sign-in fails while the account is not locked, confirm that the account exists (`SUPPORTDESK_DEMO_PASSWORD` must be set for the demo users to be seeded), that it is active, and that the password matches the value in `.env`. Demo users are seeded only when the database is created; after changing `.env`, recreate the stack with `docker compose down -v` and `docker compose up --build -d`.

## Incident checklist

1. Check `docker compose ps` and `/health/ready`.
2. Inspect API and worker logs with the correlation ID from the failing response.
3. Confirm database connectivity and disk space for attachments.
4. Check `OutboxMessages` for accumulated failures.
5. If a permission change appears not to apply, wait for the security-stamp refresh interval (two minutes) or sign out and back in.
