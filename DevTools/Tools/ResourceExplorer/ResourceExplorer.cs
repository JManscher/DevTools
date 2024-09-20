using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Identity;
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
        var explorerTools = new List<string> { "Go back", "Explore resource groups", "Search for resources", "Compare Resources" };
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
                    case "Compare Resources":
                        await CompareResources();
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
            var resourceData = await FindResource();

            if (resourceData is null)
            {
                return;
            }

            var jsonText = new JsonText(JsonSerializer.Serialize(resourceData, Defaults.JsonSerializerOptions));

            var panel = new Panel(jsonText)
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(foreground: Color.DarkOrange)
            };

            AnsiConsole.Write(panel);

            var nextStep = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("What would you like to do next?")
                .AddChoices(["Go back", "Make a new search"]));

            if (nextStep == "Go back")
            {
                return;
            }

            if (nextStep == "Make a new search")
            {
                continue;
            }

        }


    }

    private async Task CompareResources()
    {
        var resource1 = await FindResource();

        if (resource1 is null)
        {
            return;
        }

        AddResourceContext(TreeContext(), resource1).RenderHeader();

        var resource2 = await FindResource(resource1?["type"]?.ToString());

        if (resource2 is null)
        {
            return;
        }

        var flattenedCompareTo = FlattenJson(resource1!);
        var flattenedResourceData = FlattenJson(resource2);

        var showOnlyDiffs = true;

        while (true)
        {
            AddResourceContext(TreeContext(), resource1!).RenderHeader();

            var diffTable = new Table();

            diffTable.AddColumn("JsonPath");
            diffTable.AddColumn("First");
            diffTable.AddColumn("Second");

            var diffStyle = new Style(decoration: Decoration.Bold, foreground: Color.Red);

            foreach (var key in flattenedCompareTo.Keys)
            {
                var firstValue = flattenedCompareTo[key];
                var secondValue = flattenedResourceData.ContainsKey(key) ? flattenedResourceData[key] : "N/A";

                if (firstValue != secondValue)
                {
                    diffTable.AddRow(new Markup(key.EscapeMarkup(), diffStyle), new Markup(firstValue.EscapeMarkup(), diffStyle), new Markup(secondValue.EscapeMarkup(), diffStyle));
                }
                else if (showOnlyDiffs == false)
                {
                    diffTable.AddRow(key.EscapeMarkup(), firstValue.EscapeMarkup(), secondValue.EscapeMarkup());
                }
            }


            diffTable.Collapse();


            AnsiConsole.Write(diffTable);

            AnsiConsole.MarkupLine("[bold darkorange]Press w to show all rows/hide equal rows[/]");
            AnsiConsole.MarkupLine("[bold darkorange]Press q to go back[/]");
            var keyPressed = await AnsiConsole.Console.Input.ReadKeyAsync(true, CancellationToken.None);

            if (keyPressed.Value.Key == ConsoleKey.W)
            {
                showOnlyDiffs = !showOnlyDiffs;
                continue;
            }

            if (keyPressed.Value.Key == ConsoleKey.Q)
            {
                return;
            }

        }



    }

    private async Task<JsonObject?> FindResource(string? resourceType = null)
    {

        var searchString = AnsiConsole.Ask<string>("[bold yellow]Search, q to go back [/]");

        if (searchString == "q")
        {
            return null;
        }

        var resources = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync($"Retrieving data for {searchString}", (_) => resourceType is null ?
                _resourceManagerService.GetSourcesInTenant(searchString) :
                _resourceManagerService.GetSourcesInTenant(searchString, resourceType));

        var choices = resources.Select(r => $"{r.Name} - {r.Type} - {r.ResourceGroup}").Prepend(GoBack);

        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Select a resource")
            .EnableSearch()
            .PageSize(15)
            .AddChoices(choices));

        if (choice == GoBack)
        {
            return null;
        }

        var resource = resources.First(r => $"{r.Name} - {r.Type} - {r.ResourceGroup}" == choice);
        var resourceData = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<JsonObject>($"Retrieving data for {resource.Name}", (_) => _resourceManagerService.GetResourceDetails(resource.Id));

        return resourceData;

    }

    private Tree AddResourceContext(Tree tree, JsonObject data)
    {
        var resourceNode = tree.AddNode("[bold yellow]Compare resources[/]");

        var grid = new Grid();
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
            new Text(data["name"]!.ToString(), ValueStyle),
            new Text(data["type"]!.ToString(), ValueStyle),

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
                .StartAsync($"Retrieving resource groups from {SelectedSubscription?.DisplayName}", (_) => _resourceManagerService.GetResourceGroups());

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
            .StartAsync($"Retrieving resources from {resourceGroup.Name}", (_) => _resourceManagerService.GetResourcesInResourceGroup(resourceGroup.Name));
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Type");
        table.AddColumn("Kind");
        table.AddColumn("Sku");

        foreach (var resource in resources)
        {
            table.AddRow(Markup.Escape(resource.Name), resource.Type, string.IsNullOrEmpty(resource.Kind) ? "N/A" : resource.Kind, resource.Sku?.ToString() ?? "N/A");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[bold darkorange]Press any key to continue[/]");
        await AnsiConsole.Console.Input.ReadKeyAsync(true, CancellationToken.None);
        AnsiConsole.Clear();
    }


    private Dictionary<string, string> FlattenJson(JsonObject json)
    {
        var flattenedJson = new Dictionary<string, string>();
        FlattenJsonRecursive(json, "", flattenedJson);
        return flattenedJson;
    }

    private static void FlattenJsonRecursive(JsonObject json, string prefix, Dictionary<string, string> flattenedJson)
    {
        foreach (var property in json)
        {
            var propertyName = property.Key;
            var propertyValue = property.Value;

            if (propertyValue is JsonObject nestedJson)
            {
                FlattenJsonRecursive(nestedJson, $"{prefix}{propertyName}.", flattenedJson);
            }
            else
            {
                flattenedJson.Add($"{prefix}{propertyName}", propertyValue?.ToString() ?? "null");
            }
        }
    }

}
