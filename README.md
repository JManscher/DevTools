---
languages:
- csharp
products:
- dotnet
- dotnet-aspire
page_type: sample
name: ".NET Aspire dapr sample app"
urlFragment: "aspire-dapr"
description: "A sample .NET Aspire app that shows how to use dapr"
---

# .NET Aspire dapr sample app

This is a simple .NET app that shows how to use Dapr with .NET Aspire orchestration.

## Demonstrates

- How to configure a .NET Aspire app to work with Dapr
- How to use CosmosDB
- How to use pubsub with Dapr and Aspire
- How to use workers with Dapr and Aspire

## Sample prerequisites

### Dapr

Dapr installation instructions can be found [here](https://docs.dapr.io/getting-started/install-dapr-cli/). After installing the Dapr CLI, remember to run `dapr init` as described [here](https://docs.dapr.io/getting-started/install-dapr-selfhost/).

### CosmosDB
Additionally, you need to have a CosmosDB account. You can create a CosmosDB account by following the instructions [here](https://docs.microsoft.com/azure/cosmos-db/create-cosmosdb-resources-portal).
Alternatively you can use the CosmosDB emulator by following the instructions [here](https://docs.microsoft.com/azure/cosmos-db/local-emulator).


### Redis viewer
Another Redis Desktop Manager can be used to view the Redis cache. You can download it from [here](https://github.com/qishibo/AnotherRedisDesktopManager)


This sample is written in C# and targets .NET 8.0. It requires the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

## Building the sample

To download and run the sample, follow these steps:

1. Clone the this repository.
2. In Visual Studio (2022 or later):
    1. On the menu bar, choose **File** > **Open** > **Project/Solution**.
    2. Navigate to the folder that holds the sample code, and open the solution (.sln) file.
    3. Right click the _AspireWithDapr.AppHost_ project in the solution explore and choose it as the startup project.
    4. Choose the <kbd>F5</kbd> key to run with debugging, or <kbd>Ctrl</kbd>+<kbd>F5</kbd> keys to run the project without debugging.
3. From the command line:
   1. Navigate to the folder that holds the sample code.
   2. At the command line, type [`dotnet run`](https://docs.microsoft.com/dotnet/core/tools/dotnet-run).

For more information about using dapr, see the [Dapr documentation](https://docs.dapr.io/developing-applications/sdks/dotnet/).
