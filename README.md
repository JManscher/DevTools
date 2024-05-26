# DevTools

## Table of contents:
- [Tools](#tools)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Running the tool](#running-the-tool)

______________

## Prerequisites
For this tool to work, you need to have az cli installed, and aspnet 8.0.0 or higher installed.

1. Install az cli: [See guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
2. Install aspnet: [See guide](https://dotnet.microsoft.com/download/dotnet/8.0)

## Installation
1. Clone the repository
2. Change directory to DevTools: `cd .\DevTools\`
3. Run `dotnet pack` and afterwards `dotnet tool install --global --add-source ./nupkg DevTools`
4. 
## Running the tool
1. Run the following command: `devt`
2. If you are not logged in through az login already, the tool will prompt you to do so.
3. use ctrl + c to exit the tool.
________________