$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

npm run dev --prefix $RepositoryRoot --workspace src/Alsappan.Web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
