param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Path = "/api/todos",
    [int]$TimeoutSeconds = 180,
    [int]$PollIntervalSeconds = 2
)

$ErrorActionPreference = "Stop"

$normalizedBaseUrl = $BaseUrl.TrimEnd("/")
$normalizedPath = if ($Path.StartsWith("/")) { $Path } else { "/$Path" }
$url = "$normalizedBaseUrl$normalizedPath"

$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$httpClient = [System.Net.Http.HttpClient]::new($handler)
$httpClient.Timeout = [TimeSpan]::FromSeconds(10)

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$lastError = $null

while ((Get-Date) -lt $deadline) {
    try {
        $response = $httpClient.GetAsync($url).GetAwaiter().GetResult()
        $statusCode = [int]$response.StatusCode

        # 2xx/3xx/4xx means the app is reachable; only 5xx is considered unhealthy.
        if ($statusCode -ge 200 -and $statusCode -lt 500) {
            Write-Host "Smoke test succeeded: $url returned HTTP $statusCode." -ForegroundColor Green
            return
        }

        $lastError = "HTTP $statusCode"
    }
    catch {
        $lastError = $_.Exception.Message
    }

    Start-Sleep -Seconds $PollIntervalSeconds
}

throw "Smoke test failed for $url within $TimeoutSeconds seconds. Last error: $lastError"
