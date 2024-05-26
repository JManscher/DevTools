using BicepContainerRegistryTool.AzureService;
using DevTools.Services.Azure;
using Spectre.Console;

namespace DevTools.Tools.BicepExplorer;

public class BicepExplorer
{
    public static async Task Run()
    {
        var resourceManagerService = AzureResourceManagerService.Instance;
        
        var containerRegistries = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<ContainerRegistryQuery>>($"Retrieving container registries", (context) => resourceManagerService.GetContainerRegistries());
        
        var containerRegistry = AnsiConsole.Prompt(new SelectionPrompt<ContainerRegistryQuery>().Title("Select a container registry").PageSize(10).UseConverter(c => $"{c.Name} - {c.LoginServer}").AddChoices(containerRegistries));
        
        var containerRegistryService = new ContainerRegistryService(containerRegistry);
        
        var repositories = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<string>>($"Retrieving repositories", (context) => containerRegistryService.GetRepositoriesAsync());
        
        var repository = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a repository").PageSize(10).AddChoices(repositories));
        
        var tags = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<string>>($"Retrieving tags", (context) => containerRegistryService.GetTagsAsync(repository));
        
        AnsiConsole.MarkupLine($"Tags for repository [yellow]{repository}[/]:");
        foreach (var tag in tags)
        {
            AnsiConsole.MarkupLine($"- [green]{tag}[/]");
        }
        
        AnsiConsole.MarkupLine("Press any key to continue...");
        Console.ReadKey();
    }
}