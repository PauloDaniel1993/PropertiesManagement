$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

dotnet ef database update `
  --project (Join-Path $RepositoryRoot "src/Alsappan.Infrastructure") `
  --startup-project (Join-Path $RepositoryRoot "src/Alsappan.Api")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
