# Tower Management System

TowerCo is a fictional European tower company that builds and leases telecom towers to mobile
network operators (MNOs). This repository contains a working **Tower Management System** that
covers the four use cases from the assessment brief:

| # | Use case | Where to see it |
|---|----------|-----------------|
| 1 | **Manage tower sites** - register a site and its tower, update details over time | `Sites → Register site`, `Sites → <site> → Edit site` |
| 2 | **Assign tenants** - add an operator to a tower and see whether it still has capacity | `Sites → <site> → + Assign tenant` (capacity bar + 409 guard) |
| 3 | **Find sites** - search/filter by country or status, open a single view with everything | `Sites` filters + `Sites → <site>` detail |
| 4 | **Visualise the portfolio** - where the sites are and how well utilised they are | `Dashboard` (Leaflet map, KPIs, charts, sales leads) |

Plus the extra requirements agreed for the session: **SQL Server** (LocalDB locally, Azure SQL in
the cloud), **JWT authentication**, **users with roles** where the **menu follows the role**, and
**Swagger enabled**.

---

## Tech stack

| Layer | Choice |
|---|---|
| Frontend | React 19 + TypeScript + Vite, React Router, TanStack Query, Tailwind CSS, Leaflet (map), Recharts (charts) |
| Backend | .NET 10 Web API (controllers), EF Core 10, ASP.NET Core Identity, JWT bearer auth, Swagger (Swashbuckle) |
| Database | SQL Server (LocalDB locally - `TowerManagementDb`), Azure SQL ready (same provider, connection string only) |
| Testing | xUnit (domain rules + API integration via `WebApplicationFactory`), PowerShell smoke script, `tsc` + Vite build |

---

## Quick start

Prerequisites: **.NET 10 SDK**, **Node 20+**, **SQL Server LocalDB** (installed with Visual Studio or
the SQL Server Express installer). Nothing else - no Docker, no external services.

```powershell
# 0) Local configuration - appsettings*.json is gitignored because it holds the SQL connection
#    string and the JWT signing key. Start from the committed template:
Copy-Item src/Tms.Api/appsettings.Example.json src/Tms.Api/appsettings.json

# 1) API - applies EF migrations, seeds roles/users and 24 demo sites on first run
dotnet run --project src/Tms.Api            # http://localhost:5080/swagger

# 2) SPA (second terminal) - Vite proxies /api to the API
cd web/tms-web
npm install
npm run dev                                 # http://localhost:5173
```

> **Configuration is not in the repository.** `src/Tms.Api/appsettings*.json` is **gitignored**
> (only `appsettings.Example.json` is committed) because it holds the SQL connection string and the
> JWT signing key. The template points at LocalDB and uses a `__SET_JWT_SIGNING_KEY_MIN_32_CHARS__`
> placeholder - replace it with a real key of at least 32 characters. When you add a new setting,
> add it to `appsettings.Example.json` too so the template stays current.

Or start both automatically:

```powershell
powershell -File ./scripts/run-dev.ps1
```

### Run against the Azure SQL database

The connection string for the provisioned Azure database
(`asadlearning.database.windows.net` / `free-sql-db-6822046`) lives in
**`src/Tms.Api/appsettings.Azure.json`**, which is **gitignored** because it contains the SQL
password. Start the API with the `azure-db` launch profile:

```powershell
dotnet run --project src/Tms.Api --launch-profile azure-db    # http://localhost:5081/swagger
```

`ASPNETCORE_ENVIRONMENT=Azure` layers `appsettings.Azure.json` over `appsettings.json`, so the API
applies the EF migrations and seeds the demo data into that Azure database on first start.

Point the SPA at that API (it proxies to 5080 by default):

```powershell
cd web/tms-web
$env:VITE_API_TARGET='http://localhost:5081'; npm run dev
```

The Azure database is already migrated and seeded (24 sites, 30 towers, 56 tenancies, 8 operators,
4 demo users).

* SQL authentication is used (`User ID=saadmin; Password=***`).
* The file also keeps a `DefaultAad` variant using `Authentication="Active Directory Default"` for
  environments where the caller has an Entra identity (`az login`, Visual Studio, or an App Service
  managed identity) - no password in that case.
* The database must allow your client IP: **Azure Portal → SQL server → Networking → Add current
  client IP**.


### Demo users

All demo users share the password **`TowerCo!2026`** (also shown on the login screen).

| E-mail | Role | What the menu shows |
|---|---|---|
| `admin@towerco.demo` | Admin | Dashboard, Sites, Operators, Tenancies, **Users** |
| `ops@towerco.demo` | Operations | Dashboard, Sites, Operators, Tenancies - can create/edit sites and towers |
| `commercial@towerco.demo` | Commercial | Dashboard, Sites, Operators, Tenancies - can assign/terminate tenancies |
| `manager@towerco.demo` | Manager | Dashboard, Sites, Operators, Tenancies - read only |

Sign in as different users to see the **role-based menu and buttons change**: the sidebar is
filtered by the permissions returned from `GET /api/auth/me`, and the API enforces the same
permissions as policies (a Manager calling `POST /api/sites` gets `403`).

---

## Repository layout

```
c:\IHS
├─ TmsAssessment.slnx                 solution (Domain, Infrastructure, Api + 2 test projects)
├─ src
│  ├─ Tms.Domain                      entities, enums, capacity value object, business rules
│  ├─ Tms.Infrastructure              EF Core context, configurations, migrations, demo data seed
│  └─ Tms.Api                         controllers, contracts (DTOs), services, JWT auth, Swagger
├─ tests
│  ├─ Tms.Domain.Tests                28 unit tests (capacity, tenancy rules, status machine)
│  └─ Tms.Api.Tests                   28 integration tests (auth/RBAC, sites, tenancies, dashboard)
├─ web/tms-web                        React SPA (Vite + TS + Tailwind)
├─ scripts
│  ├─ run-dev.ps1                     starts API + SPA
│  └─ smoke-test.ps1                  end-to-end checks against a running API
└─ docs
   ├─ explanation.md                  one-page domain-model & architecture talk track (start here)
   ├─ architecture.md                 layers, auth flow, RBAC matrix, decisions, tradeoffs
   ├─ domain-model.md                 entities, ER diagram, business rules, seed data
   ├─ api.md                          endpoint reference with sample requests/responses
   ├─ demo-script.md                  7-minute walkthrough + ready "live change" candidates
   └─ azure-deployment.md             App Service + Azure SQL deployment guide
```

---

## Running the tests

```powershell
dotnet test                                   # 56 tests: domain rules + API integration
cd web/tms-web; npm run build                 # tsc typecheck + production Vite build

# End-to-end smoke test against a running API (it starts nothing itself)
dotnet run --project src/Tms.Api
powershell -File ./scripts/smoke-test.ps1
```

The integration tests boot the real pipeline with `WebApplicationFactory`, create a throw-away
LocalDB database per run, migrate + seed it, and drop it afterwards.

---

## API in one screen

Full reference: [`docs/api.md`](docs/api.md). Swagger UI: <http://localhost:5080/swagger>
(use `POST /api/auth/login` first, then the **Authorize** button with the access token).

```
POST   /api/auth/login              -> JWT access token + rotating refresh token + permissions
POST   /api/auth/refresh            -> rotate refresh token
POST   /api/auth/logout             -> revoke refresh token
GET    /api/auth/me                 -> current user, roles, permissions

GET    /api/sites                   ?search&country&status&hasCapacity&sort&page&pageSize
GET    /api/sites/{id}              -> full site: towers, tenants, capacity, revenue
POST   /api/sites                   -> register site (+ optional first tower)
PUT    /api/sites/{id}              -> update site details
PATCH  /api/sites/{id}/status       -> Planned -> Active -> Decommissioned (validated)
DELETE /api/sites/{id}              -> blocked while active tenancies exist
GET    /api/sites/{id}/towers       POST /api/sites/{id}/towers

GET    /api/towers/{id}             PUT /api/towers/{id}      GET /api/towers/{id}/capacity

GET    /api/operators               POST /api/operators       PUT /api/operators/{id}
GET    /api/tenancies               POST /api/tenancies       PUT /api/tenancies/{id}
POST   /api/tenancies/{id}/terminate

GET    /api/dashboard/summary       -> KPIs, by-country, by-status, utilisation buckets, map pins
GET    /api/lookups                 -> countries, enums, currencies, roles, permissions
GET    /health                      GET /health/ready          -> DB health probe (App Service ready)
```

---

## Design decisions in one minute

* **Capacity is derived, never stored.** `Tower.TenantCapacity` vs. the count of *active*
  tenancies drives every "can this tower take another tenant?" answer, so a counter can never
  drift. The database also enforces "one active tenancy per operator per tower" with a filtered
  unique index, and the assign endpoint runs in a `Serializable` transaction so two commercial
  users cannot grab the last slot simultaneously.
* **Business rules live in the domain** (`Tms.Domain/Rules`) and are shared by the API, the seeder
  and the unit tests: capacity, duplicate tenancy, site readiness, tenancy term, rent, and the
  `Planned → Active → Decommissioned` state machine (with "no decommission while tenants are
  active").
* **Identity + our own JWTs.** ASP.NET Core Identity handles users, hashing, lockout and unique
  e-mail; the API issues short-lived (15 min) access tokens plus rotating, hashed, revocable
  refresh tokens. Roles map to fine-grained **permissions** in one file, and the SPA's menu is
  driven by those permissions - so UI and API can never disagree.
* **SQL Server everywhere.** The same EF Core model runs on LocalDB today and Azure SQL tomorrow;
  only the connection string changes (see `docs/azure-deployment.md`).
* **Small, explainable layers.** Domain / Infrastructure / Api plus a Vite SPA - enough structure
  to be testable, without Clean-Architecture ceremony that would need a slide deck.

---

## Hosting on Azure (documented, ready to run)

`docs/azure-deployment.md` walks through App Service (Linux, .NET 10) + Azure SQL + Key Vault:
app settings, CORS origin, idempotent migration script, `/health/ready` probe, Application
Insights, and an optional GitHub Actions pipeline. Deployment itself is not executed from this
machine (no `az` CLI / Docker here), which the doc states explicitly.

## Known limitations / next steps

* Refresh tokens live in `localStorage` for demo convenience; production hardening is an
  HttpOnly + Secure cookie (documented tradeoff in `docs/architecture.md`).
* The Vite bundle is ~890 kB (Leaflet + Recharts); route-level code splitting would trim it.
* No pagination on operators/users (small reference lists), no audit trail table, no e-mail
  delivery for password resets (Admin resets in-app).
* Azure deployment config is documented, not applied - it needs a subscription.
