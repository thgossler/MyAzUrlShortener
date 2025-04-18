# Azure Url Shortener (AzUrlShortener)

![GitHub Release](https://img.shields.io/github/v/release/microsoft/AzUrlShortener)  ![.NET](https://img.shields.io/badge/9.0-512BD4?logo=dotnet&logoColor=fff) [![Build](https://github.com/microsoft/AzUrlShortener/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/microsoft/AzUrlShortener/actions/workflows/build.yml) ![GitHub License](https://img.shields.io/github/license/microsoft/AzUrlShortener)

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-23-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->


![UrlShortener][UrlShortener]

A simple and easy to use and to deploy budget-friendly Url Shortener for everyone. It runs in your Azure (Microsoft cloud) subscription.  

> If you don't own an Azure subscription already, you can create your **free** account today. It comes with 200$ credit, so you can experience almost everything without spending a dime. [Create your free Azure account today](https://azure.microsoft.com/free?WT.mc_id=dotnet-0000-frbouche)

Features:

- Redirect different destination base on schedules.
- Keep Statistics of your clicks.
- Budget-friendly and 100% open-source.
- Extensible for more enterprise-friendly configurations
- Simple step by step deployment. 
  

## How To Deploy

👉 **[Step by Step Deployment](doc/how-to-deploy.md)** 👈 documentation is available here.

If you want to **Update** or **Upgrade**, please refer to [the faq page](doc/faq.md). 

## How To Use It

Once deployed, use the admin webApp (aka TinyBlazorAdmin) to create new short URLs. 

![Tiny Blazor Admin looks](images/tinyblazyadmin-tour.gif)


### Alternative Admin Tool

By default, all the required resources are deployed into Azure. However you can decide to run the [API](src/Cloud5mins.ShortenerTools.Api/) locally, in a container or somewhere else. You can than use an API client like [Postman](https://www.postman.com/) or a plugin to VSCode like [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client), to manage your URLs. We've included simple API calls via a postman collection and environment [here](./src/tools/).

You can also directly update the tables in storage using [Azure Storage Explorer](doc/how-to-use-azure-storage-explorer.md). 


---


## Contributing

If you find a bug or would like to add a feature, check out those resources:

Check out our [Code of Conduct](CODE_OF_CONDUCT.md) and [Contributing](CONTRIBUTING.md) docs. This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification.  Contributions of any kind welcome!


[UrlShortener]: images/UrlShortener_600.png
