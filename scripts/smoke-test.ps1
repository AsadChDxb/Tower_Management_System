#requires -Version 5.1
<#
.SYNOPSIS
    End-to-end smoke test for the Tower Management System API.

.DESCRIPTION
    Exercises all four use cases plus the JWT/RBAC rules against a running API and prints one
    line per check. Start the API first (dotnet run --project src/Tms.Api), then run:

        powershell -File ./scripts/smoke-test.ps1

.PARAMETER BaseUrl
    API root, defaults to http://localhost:5080
#>
param(
    [string]$BaseUrl = 'http://localhost:5080'
)

$script:Failures = 0

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [hashtable]$Headers = @{},
        [object]$Body,
        [int[]]$Expect = @(200)
    )

    $params = @{
        Method          = $Method
        Uri             = "$BaseUrl$Path"
        Headers         = $Headers
        UseBasicParsing = $true
    }

    if ($PSBoundParameters.ContainsKey('Body') -and $null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = ($Body | ConvertTo-Json -Depth 6)
    }

    try {
        $response = Invoke-WebRequest @params
        $status = [int]$response.StatusCode
        $content = $response.Content
    }
    catch {
        $webResponse = $_.Exception.Response
        if ($null -eq $webResponse) {
            throw "  FAIL $Method $Path -> $($_.Exception.Message)"
        }

        $status = [int]$webResponse.StatusCode
        $reader = New-Object System.IO.StreamReader($webResponse.GetResponseStream())
        $content = $reader.ReadToEnd()
    }

    if ($Expect -notcontains $status) {
        $script:Failures++
        Write-Host ("  FAIL {0,-6} {1,-56} -> {2} (expected {3}) {4}" -f $Method, $Path, $status, ($Expect -join '/'), $content) -ForegroundColor Red
        return $null
    }

    Write-Host ("  ok   {0,-6} {1,-56} -> {2}" -f $Method, $Path, $status) -ForegroundColor DarkGray

    if ([string]::IsNullOrWhiteSpace($content)) {
        return $null
    }

    try {
        return $content | ConvertFrom-Json
    }
    catch {
        return $content
    }
}

function Login {
    param([string]$Email, [string]$Password)

    $response = Invoke-Api -Method Post -Path '/api/auth/login' -Body @{ email = $Email; password = $Password }

    return @{ Authorization = "Bearer $($response.accessToken)" }
}


Write-Host ''
Write-Host '=== Tower Management System - API smoke test ===' -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl`n"

Write-Host '[0] health + swagger'
Invoke-Api -Method Get -Path '/health' | Out-Null
Invoke-Api -Method Get -Path '/swagger/v1/swagger.json' | Out-Null

Write-Host '[1] authentication'
$admin = Login -Email 'admin@towerco.demo' -Password 'TowerCo!2026'
$operations = Login -Email 'ops@towerco.demo' -Password 'TowerCo!2026'
$commercial = Login -Email 'commercial@towerco.demo' -Password 'TowerCo!2026'
$manager = Login -Email 'manager@towerco.demo' -Password 'TowerCo!2026'
Invoke-Api -Method Post -Path '/api/auth/login' -Body @{ email = 'admin@towerco.demo'; password = 'wrong-password' } -Expect 401 | Out-Null
Invoke-Api -Method Get -Path '/api/sites' -Expect 401 | Out-Null

$me = Invoke-Api -Method Get -Path '/api/auth/me' -Headers $admin
Write-Host ("       admin permissions: {0}" -f ($me.permissions -join ', ')) -ForegroundColor DarkGray

Write-Host '[2] use case 3 - search & filter sites'
$paged = Invoke-Api -Method Get -Path '/api/sites?country=DE&status=Active&page=1&pageSize=5&sort=utilization' -Headers $admin
Write-Host ("       {0} of {1} German active sites on page 1" -f $paged.items.Count, $paged.totalCount) -ForegroundColor DarkGray
$detail = Invoke-Api -Method Get -Path "/api/sites/$($paged.items[0].id)" -Headers $admin
Write-Host ("       {0} ({1}) - {2} tower(s), {3}/{4} slots, {5}% utilized" -f $detail.siteCode, $detail.name, $detail.totals.towerCount, $detail.totals.activeTenants, $detail.totals.tenantCapacity, $detail.totals.utilizationPercent) -ForegroundColor DarkGray

Write-Host '[3] use case 4 - dashboard'
$dashboard = Invoke-Api -Method Get -Path '/api/dashboard/summary' -Headers $manager
Write-Host ("       sites={0} towers={1} tenants={2} MRR={3} util={4}% map pins={5}" -f $dashboard.kpis.siteCount, $dashboard.kpis.towerCount, $dashboard.kpis.activeTenants, $dashboard.kpis.monthlyRevenue, $dashboard.kpis.utilizationPercent, $dashboard.mapSites.Count) -ForegroundColor DarkGray

Write-Host '[4] use case 2 - capacity rules'
$fullSite = (Invoke-Api -Method Get -Path '/api/sites?hasCapacity=false&pageSize=100' -Headers $commercial).items | Select-Object -First 1
$fullTower = (Invoke-Api -Method Get -Path "/api/sites/$($fullSite.id)" -Headers $commercial).towers | Where-Object { -not $_.canAcceptTenant } | Select-Object -First 1
$capacity = Invoke-Api -Method Get -Path "/api/towers/$($fullTower.id)/capacity" -Headers $commercial
Write-Host ("       {0}" -f $capacity.summary) -ForegroundColor DarkGray

$operators = Invoke-Api -Method Get -Path '/api/operators' -Headers $commercial
$existingTenantIds = @($fullTower.tenancies | ForEach-Object { $_.operatorId })
$newOperator = $operators | Where-Object { $existingTenantIds -notcontains $_.id } | Select-Object -First 1
$overbook = @{
    towerId      = $fullTower.id
    operatorId   = $newOperator.id
    spaceType    = 'Antenna'
    antennaCount = 1
    monthlyRent  = 1500
    currency     = 'EUR'
    startDate    = (Get-Date).ToString('yyyy-MM-dd')
}
Invoke-Api -Method Post -Path '/api/tenancies' -Headers $commercial -Body $overbook -Expect 409 | Out-Null

Write-Host '[5] use case 1 - create a site and its tower (Operations role)'
$newSiteCode = 'DE-SMK-' + (Get-Random -Minimum 1000 -Maximum 9999)
$createSite = @{
    siteCode        = $newSiteCode
    name            = 'Smoke Test Site'
    countryCode     = 'DE'
    city            = 'Berlin'
    address         = 'Teststrasse 1'
    latitude        = 52.52
    longitude       = 13.405
    siteType        = 'Greenfield'
    status          = 'Planned'
    acquiredOn      = (Get-Date).ToString('yyyy-MM-dd')
    monthlySiteCost = 500
    tower           = @{
        towerCode            = "$newSiteCode-T1"
        structureType        = 'Lattice'
        heightMeters         = 40
        tenantCapacity       = 3
        antennaMountCapacity = 9
    }
}

$created = Invoke-Api -Method Post -Path '/api/sites' -Headers $operations -Body $createSite -Expect 201
Write-Host ("       created {0} with tower {1}" -f $created.siteCode, $created.towers[0].towerCode) -ForegroundColor DarkGray
Invoke-Api -Method Post -Path '/api/sites' -Headers $operations -Body $createSite -Expect 409 | Out-Null

$activated = Invoke-Api -Method Patch -Path "/api/sites/$($created.id)/status" -Headers $operations -Body @{ status = 'Active' }
Write-Host ("       status is now {0}" -f $activated.status) -ForegroundColor DarkGray

Write-Host '[6] use case 2 - assign a tenant and observe capacity'
$tenancyBody = @{
    towerId      = $created.towers[0].id
    operatorId   = $operators[0].id
    spaceType    = 'Antenna'
    antennaCount = 2
    monthlyRent  = 1750
    currency     = 'EUR'
    startDate    = (Get-Date).ToString('yyyy-MM-dd')
}

$tenancy = Invoke-Api -Method Post -Path '/api/tenancies' -Headers $commercial -Body $tenancyBody -Expect 201
Write-Host ("       {0} now leases {1}" -f $tenancy.operatorName, $tenancy.towerCode) -ForegroundColor DarkGray
Invoke-Api -Method Post -Path '/api/tenancies' -Headers $commercial -Body $tenancyBody -Expect 409 | Out-Null

$afterAssign = Invoke-Api -Method Get -Path "/api/towers/$($created.towers[0].id)/capacity" -Headers $commercial
Write-Host ("       {0}" -f $afterAssign.summary) -ForegroundColor DarkGray

Invoke-Api -Method Post -Path "/api/tenancies/$($tenancy.id)/terminate" -Headers $commercial -Body @{ endDate = (Get-Date).ToString('yyyy-MM-dd') } | Out-Null

Write-Host '[7] RBAC - role based access'
Invoke-Api -Method Get -Path '/api/sites' -Headers $manager | Out-Null
Invoke-Api -Method Post -Path '/api/sites' -Headers $manager -Body $createSite -Expect 403 | Out-Null
Invoke-Api -Method Get -Path '/api/users' -Headers $manager -Expect 403 | Out-Null
Invoke-Api -Method Post -Path '/api/tenancies' -Headers $operations -Body $tenancyBody -Expect 403 | Out-Null
Invoke-Api -Method Get -Path '/api/users' -Headers $admin | Out-Null

$newUserBody = @{
    email       = "smoke.$((Get-Random -Minimum 100 -Maximum 999))@towerco.demo"
    displayName = 'Smoke Tester'
    password    = 'TowerCo!2026'
    roles       = @('Commercial')
}
$newUser = Invoke-Api -Method Post -Path '/api/users' -Headers $admin -Body $newUserBody -Expect 201
Write-Host ("       created user {0} with roles {1}" -f $newUser.email, ($newUser.roles -join ', ')) -ForegroundColor DarkGray

Write-Host '[8] cleanup'
Invoke-Api -Method Delete -Path "/api/sites/$($created.id)" -Headers $operations -Expect 204 | Out-Null

Write-Host ''
if ($script:Failures -eq 0) {
    Write-Host 'ALL CHECKS PASSED' -ForegroundColor Green
    exit 0
}

Write-Host ("{0} CHECK(S) FAILED" -f $script:Failures) -ForegroundColor Red
exit 1
