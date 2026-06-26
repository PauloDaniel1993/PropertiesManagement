$ErrorActionPreference = "Stop"

dotnet restore "$PSScriptRoot/../Alsappan.slnx"
npm install --prefix "$PSScriptRoot/.." --workspaces
