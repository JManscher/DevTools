using System.Text.Json;
using Azure.ResourceManager.Resources;
using DevTools.Models;
using DevTools.Services.Azure;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace DevTools;

public class ResourceExplorer
{
    private AzureResourceManagerService _resourceManagerService = new();

    private const string GoBack = "[bold darkorange]Go back[/]";

    private static readonly Style HeaderStyle = new Style(foreground: Color.Yellow);
    private static readonly Style ValueStyle = new Style(foreground: Color.Green);

    public async Task Run()
    {
        var explorerTools = new List<string> { "Go back", "Explore resource groups", "Search for resources" };
        while (true)
        {
            TreeContext().RenderHeader();

            switch (AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option")
                .AddChoices(explorerTools)))
            {
                case "Go back":
                    return;
                case "Explore resource groups":
                    await ExploreResourceGroups();
                    break;
                case "Search for resources":
                    await SearchForResources();
                    break;
            }

        }

    }

    private async Task SearchForResources()
    {
        while (true)
        {
            var searchString = AnsiConsole.Ask<string>("[bold yellow]Enter a search term. Write Quit to go back: [/]");

            if (searchString == "Quit")
            {
                return;
            }

            var resources = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
                .StartAsync<List<ResourcesQueryResult>>("Searching", (_) => _resourceManagerService.GetResourcesInSubscription(searchString));

            var choices = resources.Select(r => $"{r.Name} - {r.Type} - {r.ResourceGroup}").Prepend(GoBack);

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select a resource")
                .EnableSearch()
                .PageSize(10)
                .AddChoices(choices));

            if (choice == GoBack)
            {
                return;
            }

            var resource = resources.First(r => $"{r.Name} - {r.Type} - {r.ResourceGroup}" == choice);

            var resourceData = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
                .StartAsync<GenericResourceData>($"Retrieving data for {resource.Name}", (_) => _resourceManagerService.GetResourceDetails(resource.Id));

            var jsonText = new JsonText(JsonSerializer.Serialize(resourceData, new JsonSerializerOptions { WriteIndented = true }));

            var panel = new Panel(jsonText)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(foreground: Color.DarkOrange)
            };

            AnsiConsole.Write(panel);

            var nextStep = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("What would you like to do next?")
                .AddChoices(["Go back", "Make a new search", "Compare with another resource"]));

            if (nextStep == "Go back")
            {
                return;
            }

            if (nextStep == "Make a new search")
            {
                continue;
            }

            if (nextStep == "Compare with another resource")
            {
                await CompareResources(resourceData);
            }


        }


    }

    private async Task CompareResources(GenericResourceData compareTo)
    {
        AddResourceContext(TreeContext(), compareTo).RenderHeader();
        var searchString = AnsiConsole.Ask<string>("[bold yellow]Find a resource to compare to. Write Quit to go back: [/]");
        if (searchString == "Quit")
        {
            return;
        }
        var resources = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync($"Retrieving data for {searchString}", (_) => _resourceManagerService.GetResourcesInSubscription(searchString, compareTo.ResourceType));

        var choices = resources.Select(r => $"{r.Name} - {r.ResourceGroup}").Prepend(GoBack);

        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Select a resource")
            .EnableSearch()
            .PageSize(10)
            .AddChoices(choices));

        if (choice == GoBack)
        {
            return;
        }

        var resource = resources.First(r => $"{r.Name} - {r.ResourceGroup}" == choice);

        var resourceData = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<GenericResourceData>($"Retrieving data for {resource.Name}", (_) => _resourceManagerService.GetResourceDetails(resource.Id));

        var jsonText = new JsonText(JsonSerializer.Serialize(resourceData, new JsonSerializerOptions { WriteIndented = true }));


    }

    private Tree AddResourceContext(Tree tree, GenericResourceData data)
    {
        var resourceNode = tree.AddNode("[bold yellow]Compare resources[/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().NoWrap());

        IRenderable[] headers =
        [
            new Text("Name", HeaderStyle),
            new Text("Type", HeaderStyle)
        ];

        grid.AddRow(headers);

        IRenderable[] values =
        [
            new Text(data.Name, ValueStyle),
            new Text(data.ResourceType, ValueStyle),

        ];

        grid.AddRow(values);

        var toolPanel = new Panel(grid)
        {
            Border = BoxBorder.Heavy
        };

        resourceNode.AddNode(toolPanel);

        return tree;
    }

    private async Task ExploreResourceGroups()
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
        AnsiConsole.Clear();
    }

}
