param(
    [ValidateSet('BeforeProcessing', 'AfterCommit', 'Health')]
    [string]$Mode = 'Health',

    [string]$InventoryBaseUrl = 'http://localhost:5001'
)

$ErrorActionPreference = 'Stop'

if ($Mode -eq 'Health') {
    Invoke-RestMethod -Method Get -Uri "$InventoryBaseUrl/health"
    exit 0
}

Invoke-RestMethod `
    -Method Post `
    -Uri "$InventoryBaseUrl/debug/fail-next-stock-debit" `
    -ContentType 'application/json' `
    -Body (@{ mode = $Mode } | ConvertTo-Json)

Write-Host "Failure simulation armed: $Mode"
