using System.Text.Json;
using Azure;
using Azure.Core.Serialization;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using DevTools.Services.Azure.Models;

namespace DevTools.Services.Azure;

public class AzureResourceManagerService
{
    public static AzureResourceManagerService Instance { get; } = new();
    private const string ContainerRegistryQuery =
        """
        resources
        | where type == "microsoft.containerregistry/registries"
        | project id, name, ['type'], tenantId, resourceGroup, properties['loginServer']
        | project-rename loginServer=properties_loginServer
        """; 

    private static ArmClient Client => new(new AzureCliCredential( new AzureCliCredentialOptions
    {
        TenantId = DevToolsContext.SelectedTenant?.TenantId?.ToString() ?? null
    }));
    
    public async Task<List<TenantSimplified>> GetTenants()
    {
        var tenants = new List<TenantSimplified>();
        await foreach (var tenant in Client.GetTenants().GetAllAsync())
        {
            tenants.Add(new TenantSimplified(tenant.Data.DisplayName, tenant.Data.TenantId));
        }
        return tenants;
    }
    
    public async Task<List<SubscriptionSimplified>> GetSubscriptions()
    {
        var subscriptions = new List<SubscriptionSimplified>();
        await foreach (var subscription in Client.GetSubscriptions().GetAllAsync())
        {
            if (subscription.Data.TenantId == DevToolsContext.SelectedTenant?.TenantId)
            {
                subscriptions.Add(new SubscriptionSimplified(subscription.Data.DisplayName, subscription.Data.SubscriptionId));
            }
        }
        return subscriptions;
    }

    public async Task<List<ContainerRegistryQueryResult>> GetContainerRegistries()
    {
        var tenantResource = Client.GetTenants().First(t => t.Data.TenantId == DevToolsContext.SelectedTenant?.TenantId);
        var containerRegistries = await tenantResource.GetResourcesAsync( new ResourceQueryContent(ContainerRegistryQuery));
        return await containerRegistries.Value.Data.ToObjectAsync<List<ContainerRegistryQueryResult>>(new JsonObjectSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
                
            }),
            CancellationToken.None) ?? [];
    }
}