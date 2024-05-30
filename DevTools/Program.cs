global using static DevTools.DevToolsContext;
using Azure.Identity;
using DevTools;
using DevTools.Services.Azure;
using DevTools.Tools.AzureResourceTools;
using DevTools.Tools.BicepExplorer;
using Spectre.Console;

var tools = new List<string>
{
    "Tenant Selector",
    "Subscription Selector",
    "Bicep Explorer",
    "Resource Explorer"
};

while (true)
{
    TreeContext().RenderHeader();

    if (SelectedTenant is null)
    {
        await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync(
                $"Initializing context",
                (_) => AzureCliService.InitializeContext());
    }

    if (SelectedSubscription is null)
    {
        await SubscriptionSelector.Run();
    }

    TreeContext().RenderHeader();

    var tool = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a tool").PageSize(10).AddChoices(tools));
    try
    {
        switch (tool)
        {
            case "Tenant Selector":
                await TenantSelector.Run();
                break;
            case "Subscription Selector":
                await SubscriptionSelector.Run();
                break;
            case "Bicep Explorer":
                await new BicepExplorer().Run();
                break;
            case "Resource Explorer":
                await new ResourceExplorer().Run();
                break;
        }
    }
    catch (CredentialUnavailableException ce)
    {
        AnsiConsole.Markup($"[bold red]Failed to retrieve token for {SelectedTenant?.TenantId}[/]");
        AnsiConsole.Markup($"""[bold red]Please run "az login --tenant {SelectedTenant?.TenantId} [/]""");
        AnsiConsole.WriteException(ce);
        break;
    }
    catch (Exception e)
    {
        AnsiConsole.WriteException(e);
        break;
    }
}
