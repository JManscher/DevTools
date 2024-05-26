using DevTools.Services.Azure;
using Spectre.Console;

namespace DevTools.Tools.AzureResourceTools;

public static class SubscriptionSelector
{
    public static async Task Run()
    {
        if(DevToolsContext.SelectedTenant is null)
        {
            await TenantSelector.Run();
        }
        
        var resourceManagerService = new AzureResourceManagerService();
        
        var subscriptions = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<SubscriptionSimplified>>(
                $"Retrieving subscriptions in tenant {DevToolsContext.SelectedTenant?.DisplayName}",
                (context) => resourceManagerService.GetSubscriptions());
        
        var subscription = AnsiConsole.Prompt(new SelectionPrompt<SubscriptionSimplified>()
            .Title("Select a subscription")
            .PageSize(10)
            .UseConverter(t => $"{t.DisplayName} - {t.SubscriptionId}")
            .AddChoices(subscriptions));

        
        await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync(
                $"Setting subscription ${subscription?.DisplayName}",
                (context) => AzureCliService.SetSubscription(subscription!.SubscriptionId));
        
        DevToolsContext.SelectedSubscription = subscription;
    }
    
    public static Tree AddSubscriptionToContext(this Tree tree)
    {
        if(DevToolsContext.SelectedSubscription is null)
        {
            return tree;
        }
        
        var subscriptionInfo = tree.AddNode("[bold yellow]Subscription[/]");
        var panel = new Panel($"[italic green]{DevToolsContext.SelectedSubscription?.DisplayName} - {DevToolsContext.SelectedSubscription?.SubscriptionId}[/]");
        
        subscriptionInfo.AddNode(panel);
        
        return tree;
    }
}