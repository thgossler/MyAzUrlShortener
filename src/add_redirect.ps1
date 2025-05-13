<#
.SYNOPSIS
    Adds a Web Redirect URI to an Entra ID app registration using PowerShell modules.

.DESCRIPTION
    This script accepts EntraTenantID, EntraClientAppId, and ApplicationUrl as parameters, authenticates to Azure using Connect-AzAccount, normalizes the ApplicationUrl to ensure it ends with '/signin-oidc' (appending if necessary without duplicating slashes), retrieves the specified application via the Az.Resources module, preserves existing redirect URIs, appends the new one if it's not already present, and updates the app registration.

.PARAMETER EntraTenantID
    The Tenant ID of the Entra (Azure AD) instance.

.PARAMETER EntraClientAppId
    The Application (Client) ID of the app registration to update.

.PARAMETER ApplicationUrl
    The base application URL to use for the redirect URI. The script will ensure it ends with '/signin-oidc'.

.NOTES
    Requirements:
    - PowerShell 7
    - Az.Accounts and Az.Resources modules
    - Application.ReadWrite.All permission granted for the Tenant
#>

param(
    [Parameter(Mandatory)]
    [string] $EntraTenantID,

    [Parameter(Mandatory)]
    [string] $EntraClientAppId,

    [Parameter(Mandatory)]
    [string] $ApplicationUrl
)

# Normalize ApplicationUrl to ensure single slash and '/signin-oidc' suffix
$normalizedBase = $ApplicationUrl.TrimEnd('/')
$redirectUri = if (-not $normalizedBase.EndsWith('/signin-oidc')) { "$normalizedBase/signin-oidc" } else { $normalizedBase }

# Import required modules
Write-Host "Importing Az.Accounts and Az.Resources modules..."
Import-Module Az.Accounts -ErrorAction Stop
Import-Module Az.Resources -ErrorAction Stop

# Authenticate to Azure
Write-Host "Signing in to Azure for Tenant ID $EntraTenantID..."
Connect-AzAccount -Tenant $EntraTenantID -ErrorAction Stop | Out-Null

# Retrieve the existing application by Client App ID
Write-Host "Retrieving application (ClientAppId: $EntraClientAppId)..."
try {
    $app = Get-AzADApplication -ApplicationId $EntraClientAppId -ErrorAction Stop
} catch {
    Write-Error "Failed to retrieve the application: $_"
    exit 1
}

# Preserve existing redirect URIs
$currentUris = @()
if ($app.Web.RedirectUri) { $currentUris = $app.Web.RedirectUri }

# Check if the normalized redirect URI is already registered
if ($currentUris -icontains $redirectUri) {
    Write-Host "Redirect URI '$redirectUri' is already registered. No changes needed."
    exit 0
} else {
    Write-Host "Adding Redirect URI '$redirectUri'..."
    $updatedUris = $currentUris + $redirectUri
}

# Prepare Web object for update
$webUpdate = @{ RedirectUri = $updatedUris }

# Update the application registration with the new redirect URI
Write-Host "Updating application registration..."
try {
    Update-AzADApplication -ObjectId $app.Id -Web $webUpdate -ErrorAction Stop
    Write-Host "✔️ Successfully added the Redirect URI."
} catch {
    Write-Error "Failed to update the application: $_"
    exit 1
}
