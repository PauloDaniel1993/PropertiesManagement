$ErrorActionPreference = "Stop"

dotnet ef database update `
  --project "$PSScriptRoot/../src/Alsappan.Infrastructure" `
  --startup-project "$PSScriptRoot/../src/Alsappan.Api"
