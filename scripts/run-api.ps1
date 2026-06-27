$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

dotnet run --project (Join-Path $RepositoryRoot "src/Alsappan.Api")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
