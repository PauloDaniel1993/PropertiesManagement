$ErrorActionPreference = "Stop"

dotnet test "$PSScriptRoot/../Alsappan.slnx" --no-build
npm run test --prefix "$PSScriptRoot/.." --workspace src/Alsappan.Web
