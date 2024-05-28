using System.Text.Json.Nodes;
using DevTools.Services.Azure;
using DevTools.Services.Azure.Models;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace DevTools.Tools.BicepExplorer;

public class BicepExplorer
{
    private AzureResourceManagerService _resourceManagerService = new();
    private ContainerRegistryQueryResult? _selectedRegistry;
    private string? _selectedRepository;
    private Tag? _selectedTag;
    private ContainerRegistryService? _containerRegistryService;
    private const string GoBack = "[bold darkorange]Go back[/]";
    private static readonly Style HeaderStyle = new Style(foreground: Color.Yellow);
    private static readonly Style ValueStyle = new Style(foreground: Color.Green);
    private bool _exit;

    public async Task Run()
    {
        while (true)
        {
            if (_exit)
            {
                break;
            }

            RenderHeader();

            if (_selectedRegistry is null)
            {
                await SelectRegistry();
                continue;
            }

            if (_selectedRepository is null && _containerRegistryService is not null)
            {
                await SelectRepository();
                continue;
            }

            if (_selectedTag is null && _selectedRepository is not null && _containerRegistryService is not null)
            {
                await SelectTag();
                continue;
            }

            if (_selectedTag is not null && _selectedRepository is not null && _containerRegistryService is not null)
            {
                await DisplayBicep();
            }


        }
    }

    private async Task DisplayBicep()
    {
        var bicep = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<JsonObject>($"Retrieving bicep definition", (_) => _containerRegistryService!.GetBicep(_selectedRepository!, _selectedTag!));

        var json = new JsonText(bicep["parameters"]?.ToJsonString()!);

        while (true)
        {
            RenderHeader();

            AnsiConsole.Write(
                new Panel(json)
                    .Header("Parameters")
                    .Collapse()
                    .RoundedBorder()
                    .BorderColor(Color.Yellow));

            AnsiConsole.WriteLine("Press Enter to go back.");
            var key = await AnsiConsole.Console.Input.ReadKeyAsync(true, CancellationToken.None);

            if (key.GetValueOrDefault().Key == ConsoleKey.Enter)
            {
                _selectedTag = null;
                return;
            }
        }



    }

    private async Task SelectTag()
    {

        var tags = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<Tag>>($"Retrieving tags", (_) => _containerRegistryService!.GetTagsAsync(_selectedRepository!));

        var choices = tags.Select(t => t.Name).Prepend(GoBack).ToList();
        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a tag").PageSize(10).AddChoices(choices));

        if (choice == GoBack)
        {
            _selectedRepository = null;
        }
        else
        {
            _selectedTag = tags.First(t => t.Name == choice);
        }
    }

    private async Task SelectRepository()
    {
        var repositories = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<string>>($"Retrieving repositories", (_) => _containerRegistryService!.GetRepositoriesAsync());

        var choices = repositories.Prepend(GoBack).ToList();
        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a repository").PageSize(10).AddChoices(choices));

        if (choice == GoBack)
        {
            _selectedRegistry = null;
        }
        else
        {
            _selectedRepository = choice;
        }
    }

    private async Task SelectRegistry()
    {

        var containerRegistries = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<ContainerRegistryQueryResult>>($"Retrieving container registries", (_) => _resourceManagerService.GetContainerRegistries());

        var choices = containerRegistries.Select(c => $"{c.Name} - {c.LoginServer}").Prepend(GoBack);

        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Select a container registry")
            .PageSize(10)
            .AddChoices(choices));

        if (choice == GoBack)
        {
            _exit = true;
            return;
        }

        _selectedRegistry = containerRegistries.First(c => $"{c.Name} - {c.LoginServer}" == choice);

        _containerRegistryService = new ContainerRegistryService(_selectedRegistry!);

    }

    private void RenderHeader()
    {
        AddBicepExplorerToContext(TreeContext()).RenderHeader();
    }

    private Tree AddBicepExplorerToContext(Tree tree)
    {
        var bicepExplorerInfo = tree.AddNode("[bold yellow]Bicep Explorer[/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().NoWrap());

        IRenderable[] headers =
        [
            new Text("Registry", HeaderStyle),
            new Text("Repository", HeaderStyle),
            new Text("Tag", HeaderStyle)
        ];

        grid.AddRow(headers);

        IRenderable[] values =
        [
            new Text(_selectedRegistry?.Name ?? "Not selected", ValueStyle),
            new Text(_selectedRepository ?? "Not selected", ValueStyle),
            new Text(_selectedTag?.Name ?? "Not selected", ValueStyle)
        ];

        grid.AddRow(values);

        var toolPanel = new Panel(grid);
        toolPanel.Border = BoxBorder.Heavy;

        bicepExplorerInfo.AddNode(toolPanel);

        return tree;
    }

}