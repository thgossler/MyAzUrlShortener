#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

$tenantId = $env:URL_SHORTENER_TENANT_ID
$subscriptionId = $env:URL_SHORTENER_SUBSCRIPTION_ID
$location = if ([string]::IsNullOrWhiteSpace($env:URL_SHORTENER_LOCATION)) { "westeurope" } else { $env:URL_SHORTENER_LOCATION }
$azdEnvName = if ([string]::IsNullOrWhiteSpace($env:URL_SHORTENER_AZD_ENV_NAME)) { "urlshortener-prod" } else { $env:URL_SHORTENER_AZD_ENV_NAME }

if ([string]::IsNullOrWhiteSpace($tenantId)) {
    Write-Error "Environment variable 'URL_SHORTENER_TENANT_ID' is not set."
    exit 1
}
if ($tenantId -notmatch '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') {
    Write-Error "URL_SHORTENER_TENANT_ID '$tenantId' is not a valid Azure GUID."
    exit 1
}
if ([string]::IsNullOrWhiteSpace($subscriptionId)) {
    Write-Error "Environment variable 'URL_SHORTENER_SUBSCRIPTION_ID' is not set."
    exit 1
}
if ($subscriptionId -notmatch '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') {
    Write-Error "URL_SHORTENER_SUBSCRIPTION_ID '$subscriptionId' is not a valid Azure GUID."
    exit 1
}
if ($azdEnvName -notmatch '^[a-zA-Z0-9][a-zA-Z0-9._-]{0,87}[a-zA-Z0-9_]$') {
    Write-Error "azdEnvName '$azdEnvName' is not valid as an Azure resource group name suffix (alphanumeric, hyphens, underscores, periods; must start and end with alphanumeric or underscore)."
    exit 1
}

# --- CustomDomain conflict check ---
$scriptDir = $PSScriptRoot

function Get-JsonValue($path, $key) {
    if (Test-Path $path) {
        try { return (Get-Content $path -Raw | ConvertFrom-Json).$key } catch {}
    }
    return $null
}

function Get-DotEnvValue($path, $key) {
    if (Test-Path $path) {
        $line = Get-Content $path | Where-Object { $_ -match "^${key}=" } | Select-Object -First 1
        if ($line) { return $line -replace "^${key}=""?|""?$", "" }
    }
    return $null
}

$sources = [ordered]@{
    "appsettings.json"                      = Get-JsonValue "$scriptDir/AppHost/appsettings.json" "CustomDomain"
    "appsettings.local.json"                = Get-JsonValue "$scriptDir/AppHost/appsettings.local.json" "CustomDomain"
    ".azure/$azdEnvName/.env"               = Get-DotEnvValue "$scriptDir/.azure/$azdEnvName/.env" "AZURE_CUSTOM_DOMAIN"
    "Env var AZURE_CUSTOM_DOMAIN"           = $env:AZURE_CUSTOM_DOMAIN
}

# Effective value = highest-precedence non-empty source (bottom wins)
$effectiveValue = $null
$effectiveSource = $null
foreach ($entry in $sources.GetEnumerator()) {
    if (-not [string]::IsNullOrWhiteSpace($entry.Value)) {
        $effectiveValue = $entry.Value
        $effectiveSource = $entry.Key
    }
}

$distinctValues = $sources.Values | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique
if ($distinctValues.Count -gt 1) {
    Write-Host ""
    Write-Host "WARNING: CustomDomain values differ across sources!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Resolution order (lowest → highest precedence):" -ForegroundColor Cyan
    $i = 1
    $options = @()
    foreach ($entry in $sources.GetEnumerator()) {
        $display = if ([string]::IsNullOrWhiteSpace($entry.Value)) { "(not set)" } else { $entry.Value }
        $marker  = if ($entry.Key -eq $effectiveSource) { " <-- effective" } else { "" }
        Write-Host "  [$i] $($entry.Key): $display$marker"
        $options += $entry.Value
        $i++
    }
    Write-Host ""
    Write-Host "Effective value that will be used: $effectiveValue (from: $effectiveSource)" -ForegroundColor Green
    Write-Host ""
    $choice = Read-Host "Enter number to override, or press Enter to use effective value [$effectiveValue]"
    if (-not [string]::IsNullOrWhiteSpace($choice)) {
        $idx = [int]$choice - 1
        if ($idx -ge 0 -and $idx -lt $options.Count -and -not [string]::IsNullOrWhiteSpace($options[$idx])) {
            $effectiveValue = $options[$idx]
            Write-Host "Using: $effectiveValue" -ForegroundColor Green
        } else {
            Write-Error "Invalid selection."
            exit 1
        }
    }
    # Sync the chosen value into the .env file so azd uses it
    $envFile = "$scriptDir/.azure/$azdEnvName/.env"
    if (Test-Path $envFile) {
        $envContent = Get-Content $envFile
        $envContent = $envContent | ForEach-Object {
            if ($_ -match "^AZURE_CUSTOM_DOMAIN=") { "AZURE_CUSTOM_DOMAIN=`"$effectiveValue`"" } else { $_ }
        }
        Set-Content $envFile $envContent
        $env:AZURE_CUSTOM_DOMAIN = $effectiveValue
        Write-Host "Updated AZURE_CUSTOM_DOMAIN in $envFile" -ForegroundColor DarkGray
    }
    Write-Host ""
}
# --- end CustomDomain conflict check ---

try {
    Write-Host "Authenticating with Azure Developer CLI..." -ForegroundColor Blue
    azd auth login --tenant-id "$tenantId"
    if ($LASTEXITCODE -ne 0) {
        throw "Azure authentication failed with exit code $LASTEXITCODE"
    }

    Write-Host "Configuring Azure Container Apps persistent domains..." -ForegroundColor Blue
    azd config set alpha.aca.persistDomains on
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set ACA persistent domains configuration with exit code $LASTEXITCODE"
    }

    Write-Host "Setting .NET configuration to Release..." -ForegroundColor Blue
    if ([string]::IsNullOrWhiteSpace($env:AZD_DOTNET_CONFIGURATION)) {
        $env:AZD_DOTNET_CONFIGURATION = "Release"
    }

    Write-Host "Deploying to the environment '$azdEnvName'..." -ForegroundColor Blue
    $env:AZURE_TENANT_ID = $tenantId
    $env:AZURE_SUBSCRIPTION_ID = $subscriptionId
    $env:AZURE_LOCATION = $location
    azd up -e $azdEnvName
    if ($LASTEXITCODE -ne 0) {
        throw "Azure deployment failed with exit code $LASTEXITCODE"
    }

    Write-Host "Deployment completed successfully!" -ForegroundColor Green
}
catch {
    Write-Error "Deployment failed: $_"
    exit 1
}
