# Short explanation - domain model & architecture

The one-pager to talk to in the session (detail lives in `domain-model.md` and `architecture.md`).

## 30-second version

> "TowerCo owns **sites**; a site hosts one or more **towers**; a tower has a fixed number of tenant
> slots and leases them to **operators** (the MNOs) through **tenancies**. Everything the business
> asks - where are my sites, who pays for what, can I take another tenant - falls out of those four
> entities. Capacity is never stored, it is counted from active tenancies, so it cannot drift. The
> system is a React SPA on a .NET 10 API with JWT + role-based permissions, EF Core on SQL Server
> (LocalDB locally, Azure SQL in the cloud)."

## Domain model

```
   SITE 1 ──── * TOWER 1 ──── * TENANCY * ──── 1 OPERATOR
  (location)   (leasable asset)  (lease)       (tenant / MNO)
```

| Entity | What it is | Key attributes |
|---|---|---|
| **Site** | A physical location we operate | site code (unique), country, city, lat/long, site type, status (`Planned → Active → Decommissioned`), ground lease cost |
| **Tower** | The asset we lease space on | tower code (unique), structure type, height, **tenant capacity (slots)**, antenna mounts, commissioned date |
| **Operator** | The tenant company (MNO) | name (unique), home market, contact details |
| **Tenancy** | The lease agreement | space type, antennas, monthly rent + currency, start/end date, status (`Active`/`Terminated`), notes |

**Derived, never stored:** active tenants = count of `Active` tenancies; free slots = capacity −
active tenants; utilisation %; monthly revenue.

**Rules (one line):** a tower cannot exceed its capacity, an operator can hold only one active
tenancy per tower, only `Active` sites can be leased, the site lifecycle only moves forward, and a
site cannot be decommissioned or deleted while tenants are active.

**Why these four:** a site is a location (coords, ground lease, lifecycle), a tower is the steel we
lease (structure, capacity), an operator is a company that can rent many towers, and a tenancy is
where the commercial facts (rent, term) live - so history and capacity both work without stored
counters.

## Architecture

```
┌─────────────────────────────┐        ┌──────────────────────────────────────────────┐
│  Browser - React 19 SPA     │        │  .NET 10 Web API                             │
│  Vite + TypeScript          │  JWT   │  Controllers → Services → Domain             │
│  Router · TanStack Query    │ ─────► │  JWT auth · RBAC policies · ProblemDetails   │
│  Tailwind · Leaflet · Charr │  JSON  │  Swagger · health checks · rate limiting     │
└─────────────────────────────┘        └───────────────┬──────────────────────────────┘
                                                       │ EF Core 10
                                                       ▼
                                     ┌──────────────────────────────────────────────┐
                                     │ SQL Server  (LocalDB dev / Azure SQL prod)    │
                                     │ Sites · Towers · Operators · Tenancies        │
                                     │ Users · Roles · RefreshTokens                 │
                                     └──────────────────────────────────────────────┘
```

| Layer | Responsibility |
|---|---|
| `Tms.Domain` | Entities + business rules (`TenancyRules`, `SiteStatusRules`, `TowerCapacity`) - no framework code |
| `Tms.Infrastructure` | `TmsDbContext`, EF configurations/indexes, migrations, demo seed, Identity store, refresh tokens |
| `Tms.Api` | Controllers, DTOs, services, JWT issuing/rotation, permissions → policies, Swagger, error mapping |
| `web/tms-web` | SPA: login, role-filtered menu, dashboard/map, sites, site detail, operators, tenancies, users |
| `tests/` | 28 domain unit tests + 28 API integration tests (real pipeline, throw-away DB) |

**Auth in one breath:** ASP.NET Core Identity stores users and hashes passwords; the API issues
15-minute JWTs plus rotating, hashed, revocable refresh tokens. Roles map to **permissions** in one
file (`Permissions.cs`); the API enforces them as policies, `GET /api/auth/me` returns them, and the
SPA renders the menu and buttons from the same list - so the menu follows the role and the API stays
the authority.

**Data in one breath:** one `DbContext` for portfolio + identity, migrations applied on startup in
dev/demo, SQL Server locally and Azure SQL in the cloud with only the connection string changing.

## Choices worth defending

| Choice | Reason |
|---|---|
| Capacity derived from tenancy rows | No drift; one source of truth; DB filtered unique index + serializable transaction protect the last slot |
| Rules in the domain project | API, seeder and tests share the same rules; easy to change and demo live |
| Identity + self-issued JWT | Standard hashing/lockout without hosting an identity server |
| Permissions as policies | One mapping file controls API and UI; no scattered role string checks |
| Controllers + explicit mapping | Easy to walk through live; no AutoMapper/repository indirection to explain |
| SQL Server (not SQLite) | Requirement is Azure hosting; same provider locally removes surprises |
