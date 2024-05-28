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

        }

    }

}
