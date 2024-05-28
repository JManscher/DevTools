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
    private const string ContainerRegistryQuery =
        """
        resources
        | where type == "microsoft.containerregistry/registries"
        | project id, name, ['type'], tenantId, resourceGroup, properties['loginServer']
        | project-rename loginServer=properties_loginServer
        """;

    private ArmClient _client = new(new AzureCliCredential(new AzureCliCredentialOptions
    {
        TenantId = SelectedTenant?.TenantId?.ToString() ?? null
    }));

    public async Task<List<TenantSimplified>> GetTenants()
    {
        var tenants = new List<TenantSimplified>();
        await foreach (var tenant in _client.GetTenants().GetAllAsync())
        {
            tenants.Add(new TenantSimplified(tenant.Data.DisplayName, tenant.Data.TenantId));
        }
        return tenants;
    }

    public async Task<List<SubscriptionSimplified>> GetSubscriptions()
    {
        var subscriptions = new List<SubscriptionSimplified>();
        await foreach (var subscription in _client.GetSubscriptions().GetAllAsync())
        {
            if (subscription.Data.TenantId == SelectedTenant?.TenantId)
            {
                subscriptions.Add(new SubscriptionSimplified(subscription.Data.DisplayName, subscription.Data.SubscriptionId));
            }
        }
        return subscriptions;
    }

    public async Task<List<ResourceGroupQueryResult>> GetResourceGroups()
    {
        var tenantResource = _client.GetTenants().First(t => t.Data.TenantId == SelectedTenant?.TenantId);
        var resourceGroups = await tenantResource.GetResourcesAsync(new ResourceQueryContent(
            $"""
            resources
            | where subscriptionId == '{SelectedSubscription?.SubscriptionId}'
            | extend Name = resourceGroup
            | distinct(Name)
            """
        ));

        return await resourceGroups.Value.Data.ToObjectAsync<List<ResourceGroupQueryResult>>(new JsonObjectSerializer(Defaults.JsonSerializerOptions),
            CancellationToken.None) ?? [];


        // var sub = await tenantResource.GetSubscriptionAsync(SelectedSubscription?.SubscriptionId);
        // var resourceGroups = sub.Value.GetResourceGroups();
        // return resourceGroups.Select(r => new ResourceGroupQueryResult(r.Id.ToString(), r.Data.Name)).ToList();
    }

    public async Task<List<ContainerRegistryQueryResult>> GetContainerRegistries()
    {
        var tenantResource = _client.GetTenants().First(t => t.Data.TenantId == SelectedTenant?.TenantId);
        var containerRegistries = await tenantResource.GetResourcesAsync(new ResourceQueryContent(ContainerRegistryQuery));
        return await containerRegistries.Value.Data.ToObjectAsync<List<ContainerRegistryQueryResult>>(new JsonObjectSerializer(Defaults.JsonSerializerOptions)) ?? [];
    }
}