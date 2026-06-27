$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

dotnet restore (Join-Path $RepositoryRoot "Alsappan.slnx")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

npm install --prefix $RepositoryRoot --workspaces
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
