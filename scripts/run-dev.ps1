<#
.SYNOPSIS
    Starts the API (http://localhost:5080) and the Vite dev server (http://localhost:5173)
    in separate windows for the demo.

.DESCRIPTION
    Requires: .NET 10 SDK, Node 20+, SQL Server LocalDB (or set ConnectionStrings__Default).
    Press Ctrl+C in each window to stop.
#>

Write-Host 'Starting Tower Management System (API + SPA)...' -ForegroundColor Cyan

Start-Process -FilePath 'dotnet' `
    -ArgumentList 'run', '--project', 'src/Tms.Api', '--urls', 'http://localhost:5080' `
    -WorkingDirectory (Split-Path -Parent $PSScriptRoot) `
    -WindowStyle Normal

Start-Sleep -Seconds 6

Start-Process -FilePath 'cmd.exe' `
    -ArgumentList '/c', 'npm run dev -- --port 5173 --strictPort' `
    -WorkingDirectory (Join-Path (Split-Path -Parent $PSScriptRoot) 'web/tms-web') `
    -WindowStyle Normal

Start-Sleep -Seconds 4

Write-Host ''
Write-Host '  SPA          http://localhost:5173' -ForegroundColor Green
Write-Host '  Swagger      http://localhost:5080/swagger' -ForegroundColor Green
Write-Host '  Health       http://localhost:5080/health' -ForegroundColor Green
Write-Host ''
Write-Host 'Demo users (password TowerCo!2026):' -ForegroundColor Yellow
Write-Host '  admin@towerco.demo       Admin      - everything incl. user management'
Write-Host '  ops@towerco.demo         Operations - register/update sites and towers'
Write-Host '  commercial@towerco.demo  Commercial - operators and tenancies'
Write-Host '  manager@towerco.demo     Manager    - read-only portfolio + dashboard'
