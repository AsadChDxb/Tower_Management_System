using Microsoft.EntityFrameworkCore;
using Tms.Domain.Entities;
using Tms.Domain.Enums;

namespace Tms.Infrastructure.Persistence.Seed;

/// <summary>
/// Fictional but realistic TowerCo portfolio: 24 sites, 34 towers and ~56 tenancies across
/// six European markets. Deterministic so demos are reproducible; includes fully utilized
/// towers (capacity rule), planned/decommissioned sites (status rules) and terminated
/// tenancies (history).
/// </summary>
public static class DemoDataSeeder
{
    private sealed record OperatorSeed(string Name, string Country, string Email, string Phone);

    private sealed record SiteSeed(
        string Code,
        string Name,
        string Country,
        string City,
        string Address,
        double Latitude,
        double Longitude,
        SiteType Type,
        SiteStatus Status,
        double Height,
        (int Capacity, int ActiveTenants)[] Towers);

    private static readonly OperatorSeed[] OperatorSeeds =
    [
        new("NordTel GmbH", "DE", "leasing@nordtel.demo", "+49 30 9000 100"),
        new("RheinFunk AG", "DE", "infra@rheinfunk.demo", "+49 221 9000 200"),
        new("Batavia Mobile B.V.", "NL", "rollout@bataviamobile.demo", "+31 20 9000 300"),
        new("CanalCom", "NL", "sites@canalcom.demo", "+31 10 9000 400"),
        new("Lumiere Telecom", "FR", "deploiement@lumiere.demo", "+33 1 9000 500"),
        new("IberiaCel S.A.", "ES", "red@iberiacel.demo", "+34 91 9000 600"),
        new("PolNet S.A.", "PL", "network@polnet.demo", "+48 22 9000 700"),
        new("Crown Mobile Ltd", "GB", "estates@crownmobile.demo", "+44 20 9000 800")
    ];

    private static readonly SiteSeed[] SiteSeeds =
    [
        new("DE-BER-0142", "Berlin Mitte", "DE", "Berlin", "Chausseestrasse 112", 52.5306, 13.3847,
            SiteType.Greenfield, SiteStatus.Active, 42, [(4, 3), (3, 2)]),
        new("DE-HAM-0087", "Hamburg Hafen", "DE", "Hamburg", "Am Sandtorkai 24", 53.5438, 9.9886,
            SiteType.Greenfield, SiteStatus.Active, 48, [(4, 4)]),
        new("DE-MUC-0210", "Muenchen Sued", "DE", "Muenchen", "Tegernseer Landstrasse 88", 48.1186, 11.5810,
            SiteType.Rooftop, SiteStatus.Active, 33, [(3, 2)]),
        new("DE-CGN-0033", "Koeln Nord", "DE", "Koeln", "Innere Kanalstrasse 45", 50.9580, 6.9490,
            SiteType.Greenfield, SiteStatus.Active, 39, [(4, 2), (2, 1)]),
        new("DE-FRA-0155", "Frankfurt West", "DE", "Frankfurt", "Mainzer Landstrasse 210", 50.1098, 8.6397,
            SiteType.Greenfield, SiteStatus.Planned, 45, [(4, 0)]),
        new("DE-STR-0061", "Stuttgart Ost", "DE", "Stuttgart", "Hackstrasse 7", 48.7823, 9.2050,
            SiteType.Rooftop, SiteStatus.Active, 30, [(3, 1)]),
        new("NL-AMS-0011", "Amsterdam Zuidas", "NL", "Amsterdam", "Gustav Mahlerplein 3", 52.3370, 4.8720,
            SiteType.Rooftop, SiteStatus.Active, 36, [(4, 3), (3, 3)]),
        new("NL-RTM-0042", "Rotterdam Haven", "NL", "Rotterdam", "Waalhaven Oostzijde 81", 51.8930, 4.4230,
            SiteType.Greenfield, SiteStatus.Active, 51, [(4, 2)]),
        new("NL-UTR-0009", "Utrecht Centraal", "NL", "Utrecht", "Jaarbeursplein 6", 52.0890, 5.1060,
            SiteType.Indoor, SiteStatus.Active, 24, [(2, 1)]),
        new("NL-EIN-0027", "Eindhoven Noord", "NL", "Eindhoven", "De Hurk 12", 51.4600, 5.4500,
            SiteType.Greenfield, SiteStatus.Decommissioned, 40, [(2, 0)]),
        new("FR-PAR-0301", "Paris La Defense", "FR", "Courbevoie", "12 Esplanade du General de Gaulle", 48.8920, 2.2380,
            SiteType.Rooftop, SiteStatus.Active, 34, [(4, 3), (3, 2)]),
        new("FR-LYO-0120", "Lyon Part-Dieu", "FR", "Lyon", "Rue Servient 15", 45.7620, 4.8560,
            SiteType.Greenfield, SiteStatus.Active, 44, [(4, 2)]),
        new("FR-MRS-0078", "Marseille Port", "FR", "Marseille", "Boulevard du Littoral 60", 43.3150, 5.3620,
            SiteType.Greenfield, SiteStatus.Active, 47, [(3, 2)]),
        new("FR-TLS-0044", "Toulouse Sud", "FR", "Toulouse", "Avenue de Rangueil 118", 43.5620, 1.4620,
            SiteType.Greenfield, SiteStatus.Planned, 38, [(3, 0)]),
        new("ES-MAD-0201", "Madrid Norte", "ES", "Madrid", "Calle de Alcala 540", 40.4480, -3.6280,
            SiteType.Greenfield, SiteStatus.Active, 41, [(4, 2), (2, 1)]),
        new("ES-BCN-0105", "Barcelona Litoral", "ES", "Barcelona", "Carrer de la Marina 200", 41.3980, 2.1980,
            SiteType.Rooftop, SiteStatus.Active, 32, [(3, 2)]),
        new("ES-VLC-0056", "Valencia Puerto", "ES", "Valencia", "Avenida del Puerto 210", 39.4580, -0.3320,
            SiteType.Greenfield, SiteStatus.Active, 43, [(3, 1)]),
        new("ES-SEV-0018", "Sevilla Este", "ES", "Sevilla", "Avenida de la Innovacion 8", 37.3980, -5.9360,
            SiteType.Greenfield, SiteStatus.Decommissioned, 36, [(2, 0)]),
        new("PL-WAW-0110", "Warszawa Mokotow", "PL", "Warszawa", "ul. Domaniewska 44", 52.1830, 21.0030,
            SiteType.Greenfield, SiteStatus.Active, 46, [(4, 3)]),
        new("PL-KRK-0047", "Krakow Podgorze", "PL", "Krakow", "ul. Wielicka 28", 50.0330, 19.9640,
            SiteType.Rooftop, SiteStatus.Active, 29, [(2, 1)]),
        new("PL-GDN-0023", "Gdansk Wrzeszcz", "PL", "Gdansk", "ul. Grunwaldzka 82", 54.3820, 18.6220,
            SiteType.Greenfield, SiteStatus.Active, 41, [(3, 2)]),
        new("GB-LON-0450", "London Docklands", "GB", "London", "1 Harbour Exchange Square", 51.5040, -0.0190,
            SiteType.Rooftop, SiteStatus.Active, 35, [(4, 3), (3, 1)]),
        new("GB-MAN-0188", "Manchester Central", "GB", "Manchester", "Whitworth Street West 44", 53.4740, -2.2470,
            SiteType.Greenfield, SiteStatus.Active, 40, [(3, 2)]),
        new("GB-BRS-0072", "Bristol Temple", "GB", "Bristol", "Temple Way 12", 51.4520, -2.5850,
            SiteType.Greenfield, SiteStatus.Active, 37, [(2, 2)])
    ];

    public static async Task SeedAsync(TmsDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Sites.AnyAsync(cancellationToken))
        {
            return;
        }

        Populate(db);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Wipes the portfolio (not users) and re-seeds it - used by the demo reset endpoint.</summary>
    public static async Task ResetAsync(TmsDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Tenancies.ExecuteDeleteAsync(cancellationToken);
        await db.Towers.ExecuteDeleteAsync(cancellationToken);
        await db.Operators.ExecuteDeleteAsync(cancellationToken);
        await db.Sites.ExecuteDeleteAsync(cancellationToken);

        Populate(db);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Populate(TmsDbContext db)
    {
        var operators = OperatorSeeds
            .Select(o => new Operator
            {
                Name = o.Name,
                CountryCode = o.Country,
                ContactEmail = o.Email,
                ContactPhone = o.Phone
            })
            .ToList();

        db.Operators.AddRange(operators);

        var acquiredBase = new DateOnly(2018, 4, 1);
        var tenancyBase = new DateOnly(2022, 6, 1);
        var operatorCursor = 0;
        var tenancyCursor = 0;
        var seededTowers = new List<Tower>();

        for (var siteIndex = 0; siteIndex < SiteSeeds.Length; siteIndex++)
        {
            var seed = SiteSeeds[siteIndex];

            var site = new Site
            {
                SiteCode = seed.Code,
                Name = seed.Name,
                CountryCode = seed.Country,
                City = seed.City,
                Address = seed.Address,
                Latitude = seed.Latitude,
                Longitude = seed.Longitude,
                SiteType = seed.Type,
                Status = seed.Status,
                AcquiredOn = seed.Status == SiteStatus.Planned ? null : acquiredBase.AddDays(siteIndex * 23),
                MonthlySiteCost = 420m + ((decimal)seed.Height * 3m),
                Notes = seed.Status == SiteStatus.Decommissioned
                    ? "Decommissioned during the 2025 portfolio cleanup."
                    : null
            };

            for (var towerIndex = 0; towerIndex < seed.Towers.Length; towerIndex++)
            {
                var (capacity, activeTenants) = seed.Towers[towerIndex];

                var tower = new Tower
                {
                    TowerCode = $"{seed.Code}-T{towerIndex + 1}",
                    StructureType = seed.Type == SiteType.Rooftop
                        ? TowerStructureType.RooftopMast
                        : towerIndex % 2 == 0 ? TowerStructureType.Lattice : TowerStructureType.Monopole,
                    HeightMeters = Math.Round(seed.Height + (towerIndex * 6), 1),
                    TenantCapacity = capacity,
                    AntennaMountCapacity = 8 + (towerIndex * 4),
                    CommissionedOn = seed.Status == SiteStatus.Planned
                        ? null
                        : new DateOnly(2020, 3, 1).AddDays((siteIndex * 31) + (towerIndex * 7))
                };

                site.Towers.Add(tower);
                seededTowers.Add(tower);

                for (var i = 0; i < activeTenants; i++)
                {
                    var tenant = operators[operatorCursor % operators.Count];
                    operatorCursor++;

                    db.Tenancies.Add(new Tenancy
                    {
                        Tower = tower,
                        Operator = tenant,
                        SpaceType = (SpaceType)(tenancyCursor % 3),
                        AntennaCount = (tenancyCursor % 3) + 1,
                        MonthlyRent = 700m + (capacity * 260m) + ((decimal)tower.HeightMeters * 4m) + ((tenancyCursor % 4) * 37.5m),
                        Currency = "EUR",
                        StartDate = tenancyBase.AddDays((tenancyCursor * 11) % 700),
                        Status = TenancyStatus.Active
                    });

                    tenancyCursor++;
                }
            }

            db.Sites.Add(site);
        }

        // A little churn history: three towers that used to host operators who moved out.
        var history = new (DateOnly Start, DateOnly End, string Note)[]
        {
            (new DateOnly(2019, 5, 1), new DateOnly(2022, 4, 30), "Operator merged into a group network."),
            (new DateOnly(2020, 1, 15), new DateOnly(2023, 8, 31), "Coverage strategy change."),
            (new DateOnly(2021, 9, 1), new DateOnly(2024, 2, 28), "Site modernisation - equipment removed.")
        };

        for (var i = 0; i < history.Length; i++)
        {
            var (start, end, note) = history[i];

            db.Tenancies.Add(new Tenancy
            {
                Tower = seededTowers[i],
                Operator = operators[(operatorCursor + i) % operators.Count],
                SpaceType = SpaceType.Antenna,
                AntennaCount = 1,
                MonthlyRent = 950m + (i * 125m),
                Currency = "EUR",
                StartDate = start,
                EndDate = end,
                Status = TenancyStatus.Terminated,
                Notes = note
            });
        }
    }
}
