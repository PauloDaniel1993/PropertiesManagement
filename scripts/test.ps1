$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

dotnet test (Join-Path $RepositoryRoot "Alsappan.slnx") --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

npm run test --prefix $RepositoryRoot --workspace src/Alsappan.Web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
