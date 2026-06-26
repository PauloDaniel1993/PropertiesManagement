$ErrorActionPreference = "Stop"

dotnet build "$PSScriptRoot/../Alsappan.slnx" --no-restore
npm run build --prefix "$PSScriptRoot/.." --workspace src/Alsappan.Web
