<#
.SYNOPSIS
  Takes a full backup (schema + data) of the VemusEcommerce MySQL database into dbBackup/.

.USAGE
  From the project root:
    .\scripts\backup-db.ps1

  Or from anywhere:
    powershell -File "D:\Shahid Anjum\VemusEcommerce\scripts\backup-db.ps1"

  Keeps only the last N backups (default 10, see -KeepLast) so dbBackup/ doesn't grow forever.
#>

param(
    [int]$KeepLast = 10
)

$RepoRoot = Split-Path -Parent $PSScriptRoot
$BackupDir = Join-Path $RepoRoot "dbBackup"
$ContainerName = "elegance-mysql-dev"

# Same defaults docker-compose.yml falls back to - overridden by a real .env file if one exists.
$MysqlDatabase = "elegance_dev"
$MysqlRootPassword = "elegance_root_dev_only"

$EnvFile = Join-Path $RepoRoot ".env"
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)=(.*)$') {
            $key = $Matches[1].Trim()
            $value = $Matches[2].Trim()
            if ($key -eq "MYSQL_DATABASE") { $MysqlDatabase = $value }
            if ($key -eq "MYSQL_ROOT_PASSWORD") { $MysqlRootPassword = $value }
        }
    }
}

Write-Host "==> Checking Docker..."
try { docker info > $null 2>&1 } catch {}
if (-not $?) {
    Write-Error "Docker isn't running. Start Docker Desktop first."
    exit 1
}

$containerState = docker inspect --format='{{.State.Status}}' $ContainerName 2>$null
if (-not $containerState) {
    Write-Error "Container '$ContainerName' doesn't exist. Run docker compose up -d mysql first."
    exit 1
}

if ($containerState -ne "running") {
    Write-Host "==> Starting MySQL container..."
    docker start $ContainerName | Out-Null
    $elapsed = 0
    while ($true) {
        $pingOk = docker exec $ContainerName mysqladmin ping -h localhost -u root -p$MysqlRootPassword 2>$null | Select-String "mysqld is alive"
        if ($pingOk) { break }
        if ($elapsed -ge 30) {
            Write-Error "MySQL container did not become ready in time."
            exit 1
        }
        Start-Sleep -Seconds 2
        $elapsed += 2
    }
}

if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
}

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$OutFile = Join-Path $BackupDir "${MysqlDatabase}_${Timestamp}.sql"

Write-Host "==> Dumping database '$MysqlDatabase' to $OutFile ..."
# Routed through cmd.exe: PowerShell's own ">" redirection re-encodes captured stdout as UTF-16,
# which corrupts the dump. cmd's ">" writes mysqldump's raw UTF-8 bytes straight to disk.
$dumpCommand = "docker exec $ContainerName mysqldump -u root -p$MysqlRootPassword --routines --triggers --single-transaction --databases $MysqlDatabase > `"$OutFile`""
cmd.exe /c $dumpCommand 2>$null

if ($LASTEXITCODE -ne 0 -or -not (Test-Path $OutFile) -or (Get-Item $OutFile).Length -eq 0) {
    Write-Error "Backup failed - mysqldump did not produce output."
    Remove-Item $OutFile -ErrorAction SilentlyContinue
    exit 1
}

$sizeKb = [math]::Round((Get-Item $OutFile).Length / 1KB, 1)
Write-Host "Backup complete: $OutFile ($sizeKb KB)"

# Prune older backups beyond $KeepLast, oldest first.
$allBackups = Get-ChildItem -Path $BackupDir -Filter "${MysqlDatabase}_*.sql" | Sort-Object LastWriteTime -Descending
if ($allBackups.Count -gt $KeepLast) {
    $toDelete = $allBackups | Select-Object -Skip $KeepLast
    foreach ($file in $toDelete) {
        Write-Host "Removing old backup: $($file.Name)"
        Remove-Item $file.FullName -Force
    }
}
