using DevTools.Services.Azure;
using DevTools.Services.Azure.Models;
using Spectre.Console;

namespace DevTools.Tools.AzureResourceTools;

public static class TenantSelector
{
    public static async Task Run()
    {
        var resourceManagerService = new AzureResourceManagerService();
        var tenants = await AnsiConsole.Status().Spinner(Spinner.Known.Default)
            .StartAsync<List<TenantSimplified>>($"Retrieving tenants", (_) => resourceManagerService.GetTenants());
        
        var tenant = AnsiConsole.Prompt(new SelectionPrompt<TenantSimplified>().Title("Select a tenant").PageSize(10).UseConverter(t => $"{t.DisplayName} - {t.TenantId}").AddChoices(tenants));
        
        DevToolsContext.SelectedTenant = tenant;
        DevToolsContext.SelectedSubscription = null;
    }
    
    public static Tree AddTenantToContext(this Tree tree)
    {
        if(DevToolsContext.SelectedTenant is null)
        {
            return tree;
        }
        
        var tenantInfo = tree.AddNode("[bold yellow]Tenant[/]");
        var panel = new Panel($"[italic green]{DevToolsContext.SelectedTenant.DisplayName} - {DevToolsContext.SelectedTenant.TenantId}[/]");
        
        tenantInfo.AddNode(panel);
        
        return tree;
    }
}