# How to deploy your AzUrlShortener

## First thing first

### 👉 **Fork this repository** into your own account

> To Fork GitHub repository click on the Fork button on the top right of the screen.

![Click on the button Fork](../images/click-fork.png)

Provide a name. It can be anything, it just needs to be unique in your account (you can keep AzUrlShortener if you want). Add a description it you want and click the button **Create repository from template**. 

![Give it a name description and click Create](../images/fork-details.png)

After a few seconds, you should now be in your version of the AzUrlShortener project. If you need more detail have a look to this GitHub doc: [Fork a repo](https://docs.github.com/en/free-pro-team@latest/github/getting-started-with-github/fork-a-repo).

> Make sure you are currently in YOUR GitHub repository.
>
>![This should be YOUR repo](../images/your-account.png)


## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) or [Podman](https://podman.io/getting-started/installation)
- [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)


## Deploying to Azure

1. Make sure Docker (or Podman) is currently started. It will be used to package the Apps into containers before being 
1. In a terminal, navigate to the `src` directory of your project.

	```bash
	cd src
	```
1. Let's initialize your environment with the following command:

	```bash
	azd init'
      or
	azd init -s '<subscriptionId>' -l '<location>' -e '<environmentName>'
      e.g.
    azd init -s '693b4f92-8e56-....-b7f2-74770ba7842a' -l 'westeurope' -e 'dev'
	```

	- You will be asked "How do you want to initialize your app?" Select  **Use code in the current directory**.
	- azd will says it will generates files for using "Azure Container Apps". Select **Confirm and continue initializing my app**
	- Give a name to your environment (ex: azUrlShortener-prod). This will be used as Azure resource group name.
1. If you haven't already, log in to your Azure account with 

    ```
    azd auth login
      or
    azd auth login --tenant-id '<entraTenantId>'
    ```

1. To avoid affecting custom domains when deploying Azure Container Apps use the following command. This mostly useful if you are re-deploying, or updating an existing application. If you don't have any custom domain assign to the Azure Container Apps, you can still execute the command, but it's optional.
   
	```bash
	azd config set alpha.aca.persistDomains on
	```

1. To ensure that the following command is deploying the application in Release mode, execute:

    ```
	set AZD_DOTNET_CONFIGURATION=Release
	```

1. Provision all required Azure resources and deploy the application with the following command:

	```bash
	azd up
      or
    azd up -e '<environmentName>'
      e.g.
    azd up -e 'dev'
	```

	You will be asked where to deploy Azure Subscription and location select what make sense for you. You will be asked for more information, default are used from appsettings.json is provided, e.g.:
	- **The Custom domain**: This should be the complete (including the https part) domain name for your short URLs ex: https://c5m.ca.
	- **Default Redirect Url**: This is the URL used if the vanity doesn't exist, or if no vanity is used. It must include the https part.
	- **Entra ID application registration** details
	- Other optional parameter for influencing the Admin UI behavior

1. Get the application URL

After the deployment is complete, you will see the URLs of your applications in the terminal; the one starting by `https://admin` is the admin tools (aka AdminUI), and the one starting with `https://azfunc-light` is the redicrect service. There is also many details about the resources created in Azure, and a link to the .NET Aspire dashboard.

![azd deployment result](../images/deployment-result.png)

## Add authentication to the admin website

The app is now deployed. It does have authentication automatically enabled, but the sign-in redirect URL 'https://admin-ui...westeurope.azurecontainerapps.io/signin-oidc' (or with your custom domain) needs to be added to the registered Entra ID app. You can use the following command for that:

	```
	add_redirect.ps1 -EntraTenantID <tenantId> -EntraClientAppId <clientAppId> -ApplicationUrl <url>
	  e.g.
	add_redirect.ps1 -EntraTenantID 7f6aae9c-....-48f3-....-1ea307971ea1 -EntraClientAppId 25a7c875-....-4730-....-2ce049638534 -ApplicationUrl https://admin-ui...westeurope.azurecontainerapps.io/
	```

Navigate to the [Azure Portal](https://portal.azure.com/), and find the Resource Group you just deployed (e.g., rg-azUrl-dev). From there, select the Container App named **admin**.

![select the Container App admin](../images/select-admin-container-app.png)

From the left menu, select **Authentication** and click **Add identity provider**.

![select Authentication](../images/auth-and-provider.png)

You can choose between multiple providers, but let's use Microsoft since it's deployed in Azure and you are already logged in. Once Microsoft is chosen, you will see many configuration options. Select the recommended client secret expiration (e.g., 180 days).

You can keep all the other default settings. Click **Add**. After a few seconds, you should see a notification in the top right corner that the identity provider was added successfully.

Voila! Your app now has authentication.

Next time you navigate to the app, you will be prompted to log in with your Microsoft account. Notice that your entire app is protected. No page is accessible without authentication.

The first time you log in, you will see a Permissions requested screen. Check the **Consent** checkbox, and click **Accept**.
