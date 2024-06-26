﻿using DevTools.Services.Azure;
using DevTools.Services.Azure.Models;
using Spectre.Console;

namespace DevTools.Tools.AzureResourceTools;

public static class SubscriptionSelector
{
    public static async Task Run()
    {
        if (SelectedTenant is null)
        {
            await TenantSelector.Run();
        }

        var resourceManagerService = new AzureResourceManagerService();

        var subscriptions = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<SubscriptionSimplified>>(
                $"Retrieving subscriptions in tenant {SelectedTenant?.DisplayName}",
                (_) => resourceManagerService.GetSubscriptions());

        var subscription = AnsiConsole.Prompt(new SelectionPrompt<SubscriptionSimplified>()
            .Title("Select a subscription")
            .PageSize(10)
            .UseConverter(t => $"{t.DisplayName} - {t.SubscriptionId}")
            .AddChoices(subscriptions));

        await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync(
                $"Setting subscription {subscription.DisplayName}",
                (_) => AzureCliService.SetSubscription(subscription.SubscriptionId));

        SelectedSubscription = subscription;
    }

    public static Tree AddSubscriptionToContext(this Tree tree)
    {
        if (SelectedSubscription is null)
        {
            return tree;
        }

        var subscriptionInfo = tree.AddNode("[bold yellow]Subscription[/]");
        var panel = new Panel($"[italic green]{SelectedSubscription.DisplayName} - {SelectedSubscription.SubscriptionId}[/]");

        subscriptionInfo.AddNode(panel);

        return tree;
    }
}