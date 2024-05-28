using Azure.ResourceManager.ResourceGraph.Models;
using DevTools.Models;
using DevTools.Services.Azure;
using Spectre.Console;

namespace DevTools;

public class ResourceExplorer
{
    private AzureResourceManagerService _resourceManagerService = new();

    private const string GoBack = "[bold darkorange]Go back[/]";

    public async Task Run()
    {
        while (true)
        {
            var resourceGroups = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
                .StartAsync<List<ResourceGroupQueryResult>>($"Retrieving resource groups from {SelectedSubscription?.DisplayName}", (_) => _resourceManagerService.GetResourceGroups());

            var choices = resourceGroups.Select(rg => rg.Name).Prepend(GoBack);
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select a resource group")
                .EnableSearch()
                .PageSize(10)
                .AddChoices(choices));

            if (choice == GoBack)
            {
                return;
            }

            var resourceGroup = resourceGroups.First(rg => rg.Name == choice);
            var resources = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
                .StartAsync<List<ResourcesQueryResult>>($"Retrieving resources from {resourceGroup.Name}", (_) => _resourceManagerService.GetResourcesInResourceGroup(resourceGroup.Name));
            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Kind");
            table.AddColumn("Sku");

            foreach (var resource in resources)
            {
                table.AddRow(resource.Name, resource.Type, resource.Kind ?? "N/A", resource.Sku?.ToString() ?? "N/A");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("[bold darkorange]Press any key to continue[/]");
            await AnsiConsole.Console.Input.ReadKeyAsync(true, CancellationToken.None);

        }

    }

}
