$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

dotnet build (Join-Path $RepositoryRoot "Alsappan.slnx") --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

npm run build --prefix $RepositoryRoot --workspace src/Alsappan.Web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
