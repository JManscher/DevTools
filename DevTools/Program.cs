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
};


while (true)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("DevTools").Justify(Justify.Center).Color(Color.Green));

    if (DevToolsContext.SelectedTenant is null)
    {
        await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync(
                $"Initializing context",
                (context) => AzureCliService.InitializeContext());
    }
    
    if(DevToolsContext.SelectedSubscription is null)
    {
        await SubscriptionSelector.Run();
    }

    AnsiConsole.Write(
        DevToolsContext.RenderContext()
            .AddTenantToContext()
            .AddSubscriptionToContext());
    
    var tool = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a tool").PageSize(10).AddChoices(tools));

    switch (tool)
    {
        case "Tenant Selector":
            await TenantSelector.Run();
            break;
        case "Subscription Selector":
            await SubscriptionSelector.Run();
            break;
        case "Bicep Explorer":
            await BicepExplorer.Run();
            break;
    }
}
