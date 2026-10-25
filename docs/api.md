# API Summary

Base path: `/api/v1`. All endpoints require authentication unless noted. Unsafe methods require the `X-CSRF-TOKEN` header obtained from `GET /api/v1/auth/antiforgery`.

## Authentication

| Method | Path | Permission | Notes |
|---|---|---|---|
| GET | `/auth/antiforgery` | Anonymous | Returns the request token and sets the token cookie. |
| POST | `/auth/login` | Anonymous, rate limited | Sets the session cookie; returns the current user with permissions. |
| POST | `/auth/logout` | Authenticated | Clears the session. |
| GET | `/auth/me` | Authenticated | Returns the current user, roles, and permissions. |
| POST | `/auth/change-password` | Authenticated | Requires the current password and a 12+ character new password. |

## Tickets

| Method | Path | Permission |
|---|---|---|
| GET | `/tickets` | `tickets.view.own` or `tickets.view.all` |
| GET | `/tickets/{id}` | ownership or `tickets.view.all` |
| POST | `/tickets` | `tickets.create` |
| POST | `/tickets/{id}/comments` | `tickets.reply`; internal notes also require `tickets.note` |
| POST | `/tickets/{id}/transitions` | `tickets.update-status` or a permitted customer reopen |
| POST | `/tickets/{id}/assignment` | `tickets.assign` |
| PUT | `/tickets/{id}` | `tickets.update-priority`, `tickets.update-category`, and/or `tickets.assign` |
| POST | `/tickets/{id}/attachments` | `tickets.attach` |
| GET | `/tickets/{id}/attachments/{attachmentId}` | `tickets.attach` plus visibility |
| POST | `/tickets/{id}/articles` | `knowledge.view.published` plus article visibility |
| DELETE | `/tickets/{id}/articles/{articleId}` | `knowledge.view.published` |
| GET | `/tickets/{id}/ai-suggestions` | `ai.use` |
| POST | `/tickets/{id}/ai-suggestions` | `ai.use`, rate limited |
| POST | `/ai-suggestions/{id}/feedback` | `ai.review` |

## Knowledge

| Method | Path | Permission |
|---|---|---|
| GET | `/knowledge` | `knowledge.view.published`; drafts require `knowledge.view.all` |
| GET | `/knowledge/{id}` | published and customer-visible, or `knowledge.view.all` |
| POST | `/knowledge` | `knowledge.create` |
| PUT | `/knowledge/{id}` | `knowledge.create` |
| POST | `/knowledge/{id}/publish` | `knowledge.publish` |
| POST | `/knowledge/{id}/archive` | `knowledge.archive` |

## Reporting and notifications

| Method | Path | Permission |
|---|---|---|
| GET | `/reports/overview` | `reports.view.own` or `reports.view.all` |
| GET | `/notifications` | own notifications |
| GET | `/notifications/unread-count` | own notifications |
| POST | `/notifications/{id}/read` | own notifications |
| POST | `/notifications/read-all` | own notifications |

## Service levels

| Method | Path | Permission |
|---|---|---|
| GET | `/sla/policies`, `/sla/calendars` | `sla.view` |
| POST/PUT | `/sla/policies`, `/sla/calendars` | `sla.manage` |

## Administration

| Method | Path | Permission |
|---|---|---|
| GET | `/admin/users` | `users.view` |
| POST/PUT | `/admin/users`, `/admin/users/{id}/password` | `users.manage` |
| GET | `/admin/roles` | `roles.view` |
| POST/PUT/DELETE | `/admin/roles` | `roles.manage` |
| PUT | `/admin/roles/{id}/permissions` | `permissions.manage` |
| GET | `/admin/permissions` | `roles.view` |
| POST | `/admin/permissions` | `permissions.manage` |
| GET | `/admin/audit` | `audit.view` |

## Lookups

| Method | Path | Permission |
|---|---|---|
| GET | `/lookups/categories` | Authenticated |
| GET | `/lookups/assignees` | `tickets.assign` |
| GET | `/lookups/tags` | Authenticated |

## Health

| Method | Path | Notes |
|---|---|---|
| GET | `/health/live` | Anonymous liveness probe. |
| GET | `/health/ready` | Anonymous readiness probe including database connectivity. |
