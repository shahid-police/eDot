<#
.SYNOPSIS
  Stops the backend API, frontend dev server, and MySQL container for VemusEcommerce.

.USAGE
  From the project root:
    .\scripts\stop-app.ps1
#>

$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "==> Stopping backend (port 5071) and frontend (port 3000)..."
$ports = @(5071, 3000)
foreach ($port in $ports) {
    $conns = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    foreach ($conn in $conns) {
        $pid_ = $conn.OwningProcess
        Write-Host "Stopping PID $pid_ on port $port"
        Stop-Process -Id $pid_ -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "==> Stopping MySQL container..."
Push-Location $RepoRoot
docker compose stop mysql
Pop-Location

Write-Host "Done."
