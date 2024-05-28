using System.Diagnostics;
using System.Text.Json;
using DevTools.Services.Azure.Models;

namespace DevTools.Services.Azure;

public static class AzureCliService
{
    private static string? _azureCliPath;

    private static async Task Login()
    {
        await RunAzureCliCommand("login");
    }

    public static async Task InitializeContext()
    {
        try
        {
            var result = await RunAzureCliCommand("account show");
            var subscription = JsonSerializer.Deserialize<AzCliSubscription>(result, Defaults.JsonSerializerOptions) ?? throw new InvalidOperationException("No subscription found");

            SelectedSubscription = new SubscriptionSimplified(subscription.Name, subscription.Id);
            SelectedTenant = new TenantSimplified(subscription.TenantDisplayName, subscription.TenantId);

        }
        catch (InvalidOperationException e) when (e.Message == "Azure CLI command failed with exit code 1")
        {
            await Login();
            await InitializeContext();
        }


    }

    public static async Task SetSubscription(string subscriptionId)
    {
        await RunAzureCliCommand($"account set --subscription {subscriptionId}");
    }

    private static async Task<string> RunAzureCliCommand(string command)
    {

        if (_azureCliPath is null)
        {
            _azureCliPath = GetAzureCliPath();
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _azureCliPath,
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.Start();
        var result = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Azure CLI command failed with exit code {process.ExitCode}");
        }

        return result;
    }

    private static string GetAzureCliPath()
    {
        string az = "az.cmd";
        var result = (Environment.GetEnvironmentVariable("PATH")
                ?.Split(';') ?? [])
            .FirstOrDefault(s => File.Exists(Path.Combine(s, az)));

        if (result is null)
        {
            throw new InvalidOperationException("Azure CLI not found in PATH");
        }

        return $"{result}\\{az}";
    }

}