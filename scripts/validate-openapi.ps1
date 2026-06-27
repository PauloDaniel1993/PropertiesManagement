param(
  [switch]$NoBuild,
  [int]$Port = 5299,
  [int]$TimeoutSeconds = 60,
  [string]$OutputPath
)

$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$ApiProject = Join-Path $RepositoryRoot "src/Alsappan.Api/Alsappan.Api.csproj"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
  $OutputPath = Join-Path $RepositoryRoot ".tmp/ci/openapi/v1.json"
}

if (-not $NoBuild) {
  dotnet build $ApiProject
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFullPath)
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
  New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$baseUrl = "http://127.0.0.1:$Port"
$documentUrl = "$baseUrl/openapi/v1.json"
$logDirectory = Join-Path $RepositoryRoot ".tmp/ci/openapi"
$stdoutPath = Join-Path $logDirectory "api.stdout.log"
$stderrPath = Join-Path $logDirectory "api.stderr.log"

New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
Remove-Item -LiteralPath $stdoutPath, $stderrPath -ErrorAction SilentlyContinue

$previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
$previousAspNetCoreUrls = $env:ASPNETCORE_URLS

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = $baseUrl

$process = $null

try {
  $startProcessArguments = @{
    FilePath = "dotnet"
    ArgumentList = @("run", "--project", $ApiProject, "--no-build", "--no-launch-profile")
    WorkingDirectory = $RepositoryRoot
    PassThru = $true
    RedirectStandardOutput = $stdoutPath
    RedirectStandardError = $stderrPath
  }

  if ($env:OS -eq "Windows_NT") {
    $startProcessArguments.WindowStyle = "Hidden"
  }

  $process = Start-Process @startProcessArguments
  $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
  $content = $null
  $lastError = $null

  while ([DateTimeOffset]::UtcNow -lt $deadline) {
    if ($process.HasExited) {
      throw "API exited before OpenAPI generation completed. Exit code: $($process.ExitCode)"
    }

    try {
      $requestArguments = @{
        Uri = $documentUrl
        TimeoutSec = 5
        Headers = @{ Accept = "application/json" }
      }

      if ($PSVersionTable.PSVersion.Major -lt 6) {
        $requestArguments.UseBasicParsing = $true
      }

      $response = Invoke-WebRequest @requestArguments
      if ($response.StatusCode -eq 200 -and -not [string]::IsNullOrWhiteSpace($response.Content)) {
        $content = $response.Content
        break
      }
    }
    catch {
      $lastError = $_
      Start-Sleep -Seconds 1
    }
  }

  if ([string]::IsNullOrWhiteSpace($content)) {
    $message = "Timed out waiting for $documentUrl."
    if ($null -ne $lastError) {
      $message += " Last error: $($lastError.Exception.Message)"
    }

    throw $message
  }

  $document = $content | ConvertFrom-Json
  if ([string]::IsNullOrWhiteSpace($document.openapi)) {
    throw "Generated document is missing the OpenAPI version field."
  }

  if ($document.info.title -ne "Alsappan API") {
    throw "Generated document title was '$($document.info.title)', expected 'Alsappan API'."
  }

  if ($document.paths.PSObject.Properties.Count -eq 0) {
    throw "Generated document does not contain any API paths."
  }

  if ($null -eq $document.components.securitySchemes.Bearer) {
    throw "Generated document is missing the Bearer security scheme."
  }

  Set-Content -LiteralPath $outputFullPath -Value $content -Encoding utf8
  Write-Host "Generated and validated OpenAPI document at $outputFullPath"
}
catch {
  Write-Host "OpenAPI validation failed: $($_.Exception.Message)"
  Write-Host "API stdout:"
  if (Test-Path -LiteralPath $stdoutPath) {
    Get-Content -Raw -LiteralPath $stdoutPath | Write-Host
  }

  Write-Host "API stderr:"
  if (Test-Path -LiteralPath $stderrPath) {
    Get-Content -Raw -LiteralPath $stderrPath | Write-Host
  }

  exit 1
}
finally {
  if ($null -ne $process -and -not $process.HasExited) {
    if ($env:OS -eq "Windows_NT") {
      taskkill /PID $process.Id /T /F | Out-Null
    }
    else {
      $process.Kill()
    }
  }

  $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
  $env:DOTNET_ENVIRONMENT = $previousDotnetEnvironment
  $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
}
