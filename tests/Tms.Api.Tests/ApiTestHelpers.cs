using System.Net.Http.Json;
using System.Text.Json;
using Tms.Api.Contracts.Sites;

namespace Tms.Api.Tests;

internal static class ApiTestHelpers
{
    private static int _siteCounter;

    public static string NextSiteCode() => $"DE-TST-{Interlocked.Increment(ref _siteCounter):D4}";

    /// <summary>Creates a site (with one tower) through the API so tests stay independent.</summary>
    public static async Task<SiteDetailDto> CreateSiteAsync(
        HttpClient client,
        string status = "Active",
        int tenantCapacity = 2,
        string? siteCode = null)
    {
        var code = siteCode ?? NextSiteCode();

        var body = new
        {
            siteCode = code,
            name = $"Test site {code}",
            countryCode = "DE",
            city = "Berlin",
            address = "Teststrasse 1",
            latitude = 52.52,
            longitude = 13.405,
            siteType = "Greenfield",
            status,
            acquiredOn = "2024-01-15",
            monthlySiteCost = 450,
            tower = new
            {
                towerCode = $"{code}-T1",
                structureType = "Lattice",
                heightMeters = 35,
                tenantCapacity,
                antennaMountCapacity = 8
            }
        };

        var response = await client.PostAsJsonAsync("/api/sites", body);
        var payload = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Creating the test site failed ({(int)response.StatusCode}): {payload}");
        }

        return JsonSerializer.Deserialize<SiteDetailDto>(payload, TmsApiFactory.Json)!;
    }

    /// <summary>Reads the machine-readable error code from an RFC 7807 problem response.</summary>
    public static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(payload);

        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
