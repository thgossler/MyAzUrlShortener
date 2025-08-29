#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

$tenantId = $env:URL_SHORTENER_TENANT_ID
$azdEnvName = $env:URL_SHORTENER_AZD_ENV_NAME

if ([string]::IsNullOrWhiteSpace($tenantId)) {
    Write-Error "Environment variable 'URL_SHORTENER_TENANT_ID' is not set."
    exit 1
}
if ([string]::IsNullOrWhiteSpace($azdEnvName)) {
    Write-Error "Environment variable 'URL_SHORTENER_AZD_ENV_NAME' is not set."
    exit 1
}

try {
    Write-Host "Authenticating with Azure Developer CLI..." -ForegroundColor Blue
    azd auth login --tenant-id $tenantId
    if ($LASTEXITCODE -ne 0) {
        throw "Azure authentication failed with exit code $LASTEXITCODE"
    }

    Write-Host "Configuring Azure Container Apps persistent domains..." -ForegroundColor Blue
    azd config set alpha.aca.persistDomains on
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set ACA persistent domains configuration with exit code $LASTEXITCODE"
    }

    Write-Host "Setting .NET configuration to Release..." -ForegroundColor Blue
    $env:AZD_DOTNET_CONFIGURATION = "Release"

    Write-Host "Deploying to the environment '$azdEnvName'..." -ForegroundColor Blue
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
