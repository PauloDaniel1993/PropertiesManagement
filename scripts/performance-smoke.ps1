param(
  [string]$BaseUrl = "https://localhost:7083",
  [string]$BearerToken = $env:ALSAPPAN_PERF_BEARER_TOKEN,
  [string]$OrganizationId = $env:ALSAPPAN_PERF_ORGANIZATION_ID,
  [int]$MaxMilliseconds = 750,
  [switch]$FailOnPending
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BearerToken)) {
  throw "Set ALSAPPAN_PERF_BEARER_TOKEN or pass -BearerToken with an access token."
}

if ([string]::IsNullOrWhiteSpace($OrganizationId)) {
  throw "Set ALSAPPAN_PERF_ORGANIZATION_ID or pass -OrganizationId with the active organization id."
}

$headers = @{
  Accept = "application/json"
  Authorization = "Bearer $BearerToken"
  "X-Alsappan-Organization-Id" = $OrganizationId
  "X-Request-ID" = "perf-smoke-$([Guid]::NewGuid().ToString("N"))"
}

$checks = @(
  @{ Name = "properties-large-list"; Path = "/v1/properties?page=1&pageSize=100&sort=name"; Required = $true },
  @{ Name = "properties-indexed-status-filter"; Path = "/v1/properties?page=1&pageSize=100&status=available&sort=name"; Required = $true },
  @{ Name = "residents-large-list"; Path = "/v1/residents?page=1&pageSize=100&sort=name"; Required = $true },
  @{ Name = "payments-indexed-status-filter"; Path = "/v1/payments?page=1&pageSize=100&status=pending&sort=dueDate"; Required = $true },
  @{ Name = "occurrences-unresolved-filter"; Path = "/v1/occurrences?page=1&pageSize=100&unresolved=true&sort=dueDate"; Required = $true },
  @{ Name = "inspections-pending-filter"; Path = "/v1/inspections?page=1&pageSize=100&pending=true&sort=scheduledDate"; Required = $true },
  @{ Name = "documents-large-list"; Path = "/v1/documents?page=1&pageSize=100&sort=uploadedAt"; Required = $true },
  @{ Name = "timeline-date-filter"; Path = "/v1/timeline?page=1&pageSize=100&sort=occurredAt"; Required = $true },
  @{ Name = "audit-date-filter"; Path = "/v1/audit?page=1&pageSize=100&sort=occurredAt"; Required = $true },
  @{ Name = "dashboard-summary"; Path = "/v1/dashboard"; Required = $false },
  @{ Name = "global-search"; Path = "/v1/search?query=a&pageSize=20"; Required = $false }
)

$results = foreach ($check in $checks) {
  $uri = [Uri]::new(([Uri]$BaseUrl), $check.Path)
  $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
  $statusCode = $null
  $status = "passed"
  $errorMessage = $null

  try {
    $response = Invoke-WebRequest -Uri $uri -Headers $headers -Method Get -SkipCertificateCheck
    $statusCode = [int]$response.StatusCode
  }
  catch {
    $response = $_.Exception.Response
    if ($response -and $response.StatusCode) {
      $statusCode = [int]$response.StatusCode
    }

    if (($statusCode -eq 404 -or $statusCode -eq 501) -and -not $check.Required) {
      $status = "pending"
      $errorMessage = "Endpoint is not implemented yet."
    }
    else {
      $status = "failed"
      $errorMessage = $_.Exception.Message
    }
  }
  finally {
    $stopwatch.Stop()
  }

  if ($status -eq "passed" -and $stopwatch.ElapsedMilliseconds -gt $MaxMilliseconds) {
    $status = "failed"
    $errorMessage = "Elapsed time $($stopwatch.ElapsedMilliseconds) ms exceeded $MaxMilliseconds ms."
  }

  [pscustomobject]@{
    Name = $check.Name
    Path = $check.Path
    Required = $check.Required
    Status = $status
    StatusCode = $statusCode
    ElapsedMilliseconds = $stopwatch.ElapsedMilliseconds
    Error = $errorMessage
  }
}

$results | Format-Table -AutoSize

$failed = @($results | Where-Object { $_.Status -eq "failed" })
$pending = @($results | Where-Object { $_.Status -eq "pending" })

if ($failed.Count -gt 0) {
  throw "$($failed.Count) performance smoke check(s) failed."
}

if ($FailOnPending -and $pending.Count -gt 0) {
  throw "$($pending.Count) performance smoke check(s) are pending."
}
