# Demo script (7 minutes) + live-change candidates

## Before the session

```powershell
# 1) run the API (migrates + seeds on first start)
dotnet run --project src/Tms.Api

# 2) run the SPA
cd web/tms-web; npm run dev

# 3) sanity check all four use cases + RBAC in ~15 seconds
powershell -File ./scripts/smoke-test.ps1
```

Open: <http://localhost:5173> (the SPA calls the deployed Azure API by default) · Swagger
<https://weatherdemoapp-h0a4ggb7dzh4href.indiasouthcentral-01.azurewebsites.net/swagger> · this repo in
the editor.

## Walkthrough

| Time | Do | Say |
|---|---|---|
| 0:00 | Show the login screen, point at the four demo accounts | "One system, four personas: operations, commercial, manager, admin. The menu and buttons are driven by the permissions the API returns, so each role sees only what it can actually do." |
| 0:30 | Sign in as **Manager** | "Read-only: no Register site, no Assign tenant, no Users. `/users` is not even routable - and the API would return 403 anyway." |
| 1:00 | **Dashboard** | "Use case 4: where our sites are (map coloured by utilisation), how full they are, MRR by country, top tenants, and a sales list of towers with free slots. Every number comes from one endpoint computed from one tower snapshot, so KPIs, map and charts cannot disagree." |
| 2:00 | **Sites** → filter country `DE`, status `Active`, capacity `Has free slots`, sort by utilisation | "Use case 3: server-side search, filtering, sorting and paging. Site code, name and city are all searchable." |
| 2:45 | Open **Hamburg Hafen** (100% utilised) | "The single site view: facts, ground lease cost, map pin, towers, tenants with rents and terms, and the capacity verdict. Two towers per site matters - a site is a location, a tower is the asset we lease." |
| 3:30 | Sign in as **Commercial**, open Berlin Mitte → **+ Assign tenant** | "Use case 2. The modal tells me N slots are free before I commit; I pick an operator and rent, and the capacity bar, site totals and dashboard all update." |
| 4:15 | Try to assign to a **full** tower (Hamburg Hafen) | "The API refuses with 409 `tower_capacity_exceeded` and the exact reason. Capacity is derived from active tenancies - never a stored counter - and a filtered unique index plus a serializable transaction stop two people taking the last slot." |
| 4:45 | **Terminate** a tenancy, then retry the assignment | "Slots free up immediately; the history stays as terminated tenancies." |
| 5:15 | Sign in as **Operations** → **Register site** (site + first tower), then **Activate** it | "Use case 1 with the lifecycle state machine: Planned → Active → Decommissioned. Decommission is blocked while tenants are active - that rule is a unit test too." |
| 6:00 | Sign in as **Admin** → **Users** → create a Commercial user | "Roles are assigned here; permissions, menus and API access follow. We can also deactivate accounts and reset passwords." |
| 6:30 | Swagger → Authorize with a token → `GET /api/sites` | "Everything the SPA does is a documented, JWT-protected endpoint; Swagger has the bearer flow wired up." |

Close with: `dotnet test` (56 green), the domain/architecture docs, and the Azure deployment guide.

## Live-change candidates (pick one, 5-10 minutes)

### 1. Add a "minimum tower height" filter to the site search (vertical slice)

* `src/Tms.Api/Contracts/Sites/SiteListContracts.cs` → add `public int? MinHeightMeters { get; set; }` to `SiteQuery`.
* `src/Tms.Api/Services/SiteService.cs` → after the status filter:
  `if (query.MinHeightMeters is > 0) { sites = sites.Where(s => s.Towers.Any(t => t.HeightMeters >= query.MinHeightMeters.Value)); }`
* `web/tms-web/src/api/api.ts` → add the field to `SiteListParams`.
* `web/tms-web/src/features/sites/SitesPage.tsx` → add a number input, include it in the query key and the
  `api.sites.list` call.
* Demo: `GET /api/sites?minHeightMeters=45` in Swagger, then the same in the UI.
  Test idea: `SitesApiTests.Search_filters_by_minimum_tower_height`.

### 2. Show "months until lease end" on the tenancy tables

* `web/tms-web/src/lib/format.ts` → add `monthsUntil(date)` (pure function, easy to explain).
* `web/tms-web/src/features/sites/SiteDetailPage.tsx` and `.../tenancies/TenanciesPage.tsx` → add a column
  rendering `monthsUntil(tenancy.endDate)` or "open ended".
* Demo: no API change needed; good example of where presentation logic should live.

### 3. Add an operator country filter to the operators endpoint

* `src/Tms.Api/Controllers/OperatorsController.cs` → add `[FromQuery] string? country`.
* `src/Tms.Api/Services/OperatorService.cs` → extend `ListAsync(string? search, string? country)`.
* `web/tms-web/src/features/operators/OperatorsPage.tsx` → add a country `<select>`.
* Demo: `GET /api/operators?country=NL` in Swagger, then the UI.

### 4. Add a "renew tenancy" endpoint (stretch: shows the rule layer)

* New DTO `RenewTenancyRequest { DateOnly NewEndDate }`.
* `TenancyService.RenewAsync` → status must be Active, `NewEndDate` after the current end date, save, return DTO.
* Controller action `POST /api/tenancies/{id}/renew` with `Tenancies.Manage`.
* Rule in `TenancyRules` + a unit test, plus an integration test for the endpoint.

## Likely questions and short answers

| Question | Answer |
|---|---|
| Why is capacity not a column? | A stored counter can drift from the tenancy rows; deriving it from `Status = Active` makes tenancies the single source of truth, and the filtered unique index enforces the duplicate rule in the database. |
| What happens with two users and one free slot? | The assign call runs in a `Serializable` transaction through EF's execution strategy and the DB has a filtered unique index - the second request gets 409 instead of corrupting state. |
| Why both roles and permissions? | Roles are the business vocabulary (Operations, Commercial, Manager, Admin); permissions are the technical contract shared by API policies and the UI menu, so one mapping file controls everything. |
| How does the menu follow the role? | `GET /api/auth/me` returns `permissions[]`; the sidebar filters a declarative `navItems` array by permission. Route guards and buttons use the same check, and the API re-checks independently. |
| Why SQL Server? | The brief asks for an Azure story; the same provider locally (LocalDB) and in the cloud (Azure SQL) removes provider-specific surprises. Only the connection string changes. |
| What would you do with more time? | HttpOnly refresh cookie, audit trail, code splitting, CI/CD, leasing pipeline and forecasting (see `docs/architecture.md`). |

