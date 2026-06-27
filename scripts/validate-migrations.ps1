param(
  [switch]$NoBuild,
  [string]$Configuration = "Debug",
  [string]$OutputPath
)

$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$InfrastructureProject = Join-Path $RepositoryRoot "src/Alsappan.Infrastructure/Alsappan.Infrastructure.csproj"
$ApiProject = Join-Path $RepositoryRoot "src/Alsappan.Api/Alsappan.Api.csproj"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
  $OutputPath = Join-Path $RepositoryRoot ".tmp/ci/migrations/alsappan-idempotent.sql"
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFullPath)
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
  New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

function Invoke-DotNet {
  param([string[]]$Arguments)

  dotnet @Arguments
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }
}

$commonEfArguments = @(
  "--project",
  $InfrastructureProject,
  "--startup-project",
  $ApiProject,
  "--context",
  "AlsappanDbContext",
  "--configuration",
  $Configuration
)

if ($NoBuild) {
  $commonEfArguments += "--no-build"
}

Invoke-DotNet @("ef", "--version")

if (-not $NoBuild) {
  Invoke-DotNet @("build", (Join-Path $RepositoryRoot "Alsappan.slnx"), "--configuration", $Configuration)
}

Write-Host "Checking for pending EF model changes..."
Invoke-DotNet (@("ef", "migrations", "has-pending-model-changes") + $commonEfArguments)

Write-Host "Generating idempotent EF migration script..."
Invoke-DotNet (@("ef", "migrations", "script", "--idempotent", "--output", $outputFullPath) + $commonEfArguments)

if (-not (Test-Path -LiteralPath $outputFullPath)) {
  throw "Migration script was not generated at $outputFullPath."
}

$scriptInfo = Get-Item -LiteralPath $outputFullPath
if ($scriptInfo.Length -le 0) {
  throw "Migration script at $outputFullPath is empty."
}

Write-Host "Generated and validated EF migration script at $outputFullPath"
