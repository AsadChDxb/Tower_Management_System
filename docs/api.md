# API reference

Base URL (dev): `http://localhost:5080` · Swagger UI: `/swagger` · OpenAPI JSON: `/swagger/v1/swagger.json`

All business endpoints require a JWT: `Authorization: Bearer <accessToken>`.
Errors are RFC 7807 `application/problem+json` with an extra `code` field (see
`docs/architecture.md`).

## Auth

| Method | Route | Access | Notes |
|---|---|---|---|
| POST | `/api/auth/login` | anonymous (rate limited) | `{ email, password }` → access token (15 min), refresh token (7 days), user with roles + permissions |
| POST | `/api/auth/refresh` | anonymous (rate limited) | `{ refreshToken }` → new pair; the old token is revoked (rotation) |
| POST | `/api/auth/logout` | authenticated | `{ refreshToken }` → revokes the refresh token, `204` |
| GET | `/api/auth/me` | authenticated | current user, roles, permissions |

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "commercial@towerco.demo", "password": "TowerCo!2026" }
```

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "accessTokenExpiresAtUtc": "2026-11-04T12:15:00+00:00",
  "refreshToken": "3Jk1s...V0=",
  "refreshTokenExpiresAtUtc": "2026-11-11T12:00:00+00:00",
  "user": {
    "id": "…",
    "email": "commercial@towerco.demo",
    "displayName": "Clara Commercial",
    "isActive": true,
    "roles": ["Commercial"],
    "permissions": ["Dashboard.View", "Operators.Manage", "Portfolio.View", "Tenancies.Manage"],
    "createdAtUtc": "2026-11-04T09:12:41+00:00"
  }
}
```

## Sites (use cases 1 and 3)

| Method | Route | Policy | Notes |
|---|---|---|---|
| GET | `/api/sites` | `Portfolio.View` | `search`, `country`, `status`, `hasCapacity`, `sort` (`code`\|`name`\|`country`\|`utilization`\|`revenue`), `page`, `pageSize` |
| GET | `/api/sites/{id}` | `Portfolio.View` | site facts + towers + tenancies + totals (the "single view") |
| POST | `/api/sites` | `Sites.Manage` | register site, optional nested `tower`; `201` + `Location` |
| PUT | `/api/sites/{id}` | `Sites.Manage` | update mutable details (site code is immutable) |
| PATCH | `/api/sites/{id}/status` | `Sites.Manage` | `{ "status": "Active" }`; validated state machine |
| DELETE | `/api/sites/{id}` | `Sites.Manage` | blocked while active tenancies exist |
| GET | `/api/sites/{id}/towers` | `Portfolio.View` | towers with live capacity |
| POST | `/api/sites/{id}/towers` | `Towers.Manage` | add another tower to a site |

```http
GET /api/sites?country=DE&status=Active&hasCapacity=true&sort=utilization&page=1&pageSize=5
```

```json
{
  "items": [
    {
      "id": "9baecd51-…",
      "siteCode": "DE-STR-0061",
      "name": "Stuttgart Ost",
      "countryCode": "DE",
      "city": "Stuttgart",
      "status": "Active",
      "siteType": "Rooftop",
      "latitude": 48.7823,
      "longitude": 9.205,
      "towerCount": 1,
      "tenantCapacity": 3,
      "activeTenants": 1,
      "availableSlots": 2,
      "utilizationPercent": 33.3,
      "monthlyRevenue": 1410,
      "currency": "EUR",
      "updatedAtUtc": "2026-11-04T09:12:41+00:00"
    }
  ],
  "page": 1, "pageSize": 5, "totalCount": 5, "totalPages": 1,
  "hasPrevious": false, "hasNext": false
}
```

## Towers

| Method | Route | Policy | Notes |
|---|---|---|---|
| GET | `/api/towers/{id}` | `Portfolio.View` | tower + tenant list + capacity |
| PUT | `/api/towers/{id}` | `Towers.Manage` | capacity cannot drop below the active tenant count |
| GET | `/api/towers/{id}/capacity` | `Portfolio.View` | the "can I add a tenant?" answer |

```json
{
  "towerId": "cb7f05a4-…",
  "towerCode": "DE-HAM-0087-T1",
  "siteCode": "DE-HAM-0087",
  "tenantCapacity": 4,
  "activeTenants": 4,
  "availableSlots": 0,
  "utilizationPercent": 100,
  "canAcceptTenant": false,
  "summary": "Tower DE-HAM-0087-T1 is full (4/4 slots used)."
}
```

## Tenancies (use case 2)

| Method | Route | Policy | Notes |
|---|---|---|---|
| GET | `/api/tenancies` | `Portfolio.View` | `towerId`, `operatorId`, `status`, `country`, `search`, `page`, `pageSize` |
| GET | `/api/tenancies/{id}` | `Portfolio.View` | single tenancy |
| POST | `/api/tenancies` | `Tenancies.Manage` | assign an operator; capacity/duplicate/site-status guards |
| PUT | `/api/tenancies/{id}` | `Tenancies.Manage` | update commercial terms |
| POST | `/api/tenancies/{id}/terminate` | `Tenancies.Manage` | frees the slot; `endDate` defaults to today |

```http
POST /api/tenancies
Authorization: Bearer <token>
Content-Type: application/json

{
  "towerId": "…",
  "operatorId": "…",
  "spaceType": "Antenna",
  "antennaCount": 2,
  "monthlyRent": 1750,
  "currency": "EUR",
  "startDate": "2026-01-01",
  "endDate": null,
  "notes": "5G rollout phase 2"
}
```

Success → `201 Created`. Full tower → `409`:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Business rule violated",
  "status": 409,
  "detail": "Tower DE-HAM-0087-T1 is fully utilized (4/4 tenant slots in use). Terminate an existing tenancy or increase the tower capacity before assigning another operator.",
  "instance": "/api/tenancies",
  "code": "tower_capacity_exceeded"
}
```

## Operators

| Method | Route | Policy | Notes |
|---|---|---|---|
| GET | `/api/operators?search=` | `Portfolio.View` | with active tenancy count + MRR |
| GET | `/api/operators/{id}` | `Portfolio.View` | |
| POST | `/api/operators` | `Operators.Manage` | unique name |
| PUT | `/api/operators/{id}` | `Operators.Manage` | |

## Dashboard, lookups, users, diagnostics

| Method | Route | Policy | Notes |
|---|---|---|---|
| GET | `/api/dashboard/summary` | `Dashboard.View` | KPIs, by-country, by-status, utilisation buckets, top operators, under-utilised towers, map pins |
| GET | `/api/lookups` | `Portfolio.View` | countries, enums, currencies, roles, permissions, allowed site status transitions |
| GET | `/api/users` | `Users.Manage` | all users with roles + permissions |
| POST | `/api/users` | `Users.Manage` | create user with roles |
| PATCH | `/api/users/{id}` | `Users.Manage` | display name, active flag, roles (cannot deactivate/demote yourself) |
| POST | `/api/users/{id}/reset-password` | `Users.Manage` | admin password reset |
| POST | `/api/dev/reset-demo` | `Users.Manage` + Development | wipes and re-seeds the portfolio (not users) |
| GET | `/health`, `/health/ready` | anonymous | database health probe |

## Status codes and error codes

| Status | When | `code` examples |
|---|---|---|
| 200 / 201 / 204 | success | – |
| 400 | request shape invalid, or a "bad input" business rule | `invalid_tenancy_term`, `invalid_monthly_rent`, `unknown_role` |
| 401 | missing/expired token, bad credentials, revoked refresh token | `unauthorized` |
| 403 | authenticated but missing the permission (role) | – (authorization middleware) |
| 404 | unknown id | `not_found` |
| 409 | business rule violated | `tower_capacity_exceeded`, `duplicate_active_tenancy`, `site_not_leasable`, `site_has_active_tenancies`, `invalid_site_status_transition`, `duplicate_site_code`, `duplicate_tower_code`, `duplicate_operator_name`, `capacity_below_active_tenancies`, `tenancy_not_active`, `cannot_deactivate_self`, `cannot_remove_own_admin_role` |
| 429 | credential endpoint rate limit hit | – |

## Trying it from Swagger

1. `POST /api/auth/login` with `admin@towerco.demo` / `TowerCo!2026`, copy `accessToken`.
2. Click **Authorize**, paste the token (no `Bearer ` prefix), **Authorize**.
3. Try `GET /api/sites`, then `POST /api/tenancies` against a full tower to see the `409` code.

`src/Tms.Api/Tms.Api.http` contains the same requests for VS Code / Rider REST clients.

