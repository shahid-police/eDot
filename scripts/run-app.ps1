<#
.SYNOPSIS
  Starts Docker (MySQL), the backend API, and the frontend dev server for VemusEcommerce.

.USAGE
  From the project root:
    .\scripts\run-app.ps1

  Or from anywhere:
    powershell -File "D:\Shahid Anjum\VemusEcommerce\scripts\run-app.ps1"
#>

$RepoRoot = Split-Path -Parent $PSScriptRoot
$BackendDir = Join-Path $RepoRoot "backend\Elegance.Api"
$FrontendDir = Join-Path $RepoRoot "frontend"
$DockerExe = "C:\Program Files\Docker\Docker\Docker Desktop.exe"

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutSeconds = 120,
        [int]$IntervalSeconds = 5,
        [string]$Message = "Waiting..."
    )
    $elapsed = 0
    while (-not (& $Condition)) {
        if ($elapsed -ge $TimeoutSeconds) {
            Write-Warning "Timed out after ${TimeoutSeconds}s: $Message"
            return $false
        }
        Write-Host "$Message ($elapsed s)"
        Start-Sleep -Seconds $IntervalSeconds
        $elapsed += $IntervalSeconds
    }
    return $true
}

# 1. Docker Desktop
Write-Host "==> Checking Docker..."
$dockerRunning = $false
try {
    docker info > $null 2>&1
    $dockerRunning = $?
} catch { $dockerRunning = $false }

if (-not $dockerRunning) {
    Write-Host "Starting Docker Desktop..."
    Start-Process -FilePath $DockerExe
    Wait-Until -Message "Waiting for Docker daemon" -TimeoutSeconds 120 -Condition {
        try { docker info > $null 2>&1; $? } catch { $false }
    } | Out-Null
}
Write-Host "Docker is ready."

# 2. MySQL container
Write-Host "==> Starting MySQL container..."
Push-Location $RepoRoot
docker compose up -d mysql
Pop-Location

Wait-Until -Message "Waiting for MySQL to become healthy" -TimeoutSeconds 60 -IntervalSeconds 3 -Condition {
    $status = docker inspect --format='{{.State.Health.Status}}' elegance-mysql-dev 2>$null
    $status -eq "healthy"
} | Out-Null
Write-Host "MySQL is healthy."

# 3. Backend API
Write-Host "==> Starting backend API..."
Start-Process -FilePath "dotnet" -ArgumentList "run" `
    -WorkingDirectory $BackendDir `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $RepoRoot "backend-run.log") `
    -RedirectStandardError (Join-Path $RepoRoot "backend-run.err.log")

# 4. Frontend dev server
Write-Host "==> Starting frontend dev server..."
Start-Process -FilePath "cmd.exe" -ArgumentList "/c", "npm run dev" `
    -WorkingDirectory $FrontendDir `
    -WindowStyle Hidden `
    -RedirectStandardOutput (Join-Path $RepoRoot "frontend-run.log") `
    -RedirectStandardError (Join-Path $RepoRoot "frontend-run.err.log")

# 5. Wait for both to respond
Write-Host "==> Waiting for backend (http://localhost:5071) and frontend (http://localhost:3000)..."
$ready = Wait-Until -Message "Waiting for app to come up" -TimeoutSeconds 120 -IntervalSeconds 5 -Condition {
    $be = $false
    $fe = $false
    try { $be = (Invoke-WebRequest -Uri "http://localhost:5071/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch {}
    try { $fe = (Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 3 -MaximumRedirection 5).StatusCode -eq 200 } catch {}
    $be -and $fe
}

if ($ready) {
    Write-Host ""
    Write-Host "App is up:"
    Write-Host "  Backend:  http://localhost:5071"
    Write-Host "  Frontend: http://localhost:3000"
} else {
    Write-Warning "Backend/frontend did not both come up in time. Check backend-run.log / frontend-run.log for details."
}
