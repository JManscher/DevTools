using System.Text.Json.Nodes;
using Azure;
using Azure.Core;
using Azure.Core.Serialization;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using Azure.ResourceManager.Resources;
using DevTools.Models;
using DevTools.Services.Azure.Models;
using Spectre.Console;
using Spectre.Console.Json;

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

    public async Task<List<ResourcesQueryResult>> GetResourcesInSubscription(string searchTerm)
    {
        var tenantResource = _client.GetTenants().First(t => t.Data.TenantId == SelectedTenant?.TenantId);
        var resources = await tenantResource.GetResourcesAsync(new ResourceQueryContent(
            $"""
            resources
            | where subscriptionId == '{SelectedSubscription?.SubscriptionId}'
            | where name contains '{searchTerm}' or resourceGroup contains '{searchTerm}' or type contains '{searchTerm}'
            """
        ));
        return await resources.Value.Data.ToObjectAsync<List<ResourcesQueryResult>>(new JsonObjectSerializer(Defaults.JsonSerializerOptions),
            CancellationToken.None) ?? [];
    }

    public async Task<List<ResourcesQueryResult>> GetResourcesInSubscription(string searchTerm, string resourceType)
    {
        var tenantResource = _client.GetTenants().First(t => t.Data.TenantId == SelectedTenant?.TenantId);
        var resources = await tenantResource.GetResourcesAsync(new ResourceQueryContent(
            $"""
            resources
            | where subscriptionId == '{SelectedSubscription?.SubscriptionId}'
            | where type == '{resourceType}'
            | where name contains '{searchTerm}' or resourceGroup contains '{searchTerm}'
            """
        ));
        return await resources.Value.Data.ToObjectAsync<List<ResourcesQueryResult>>(new JsonObjectSerializer(Defaults.JsonSerializerOptions),
            CancellationToken.None) ?? [];
    }

    public async Task<GenericResourceData> GetResourceDetails(string resourceId)
    {
        var resource = _client.GetGenericResource(new ResourceIdentifier(resourceId));

        var resourceContent = await resource.GetAsync();
        return resourceContent.Value.Data;
    }


    public async Task<List<ResourcesQueryResult>> GetResourcesInResourceGroup(string resourceGroupName)
    {
        var tenantResource = _client.GetTenants().First(t => t.Data.TenantId == SelectedTenant?.TenantId);
        var resources = await tenantResource.GetResourcesAsync(new ResourceQueryContent(
            $"""
            resources
            | where resourceGroup == '{resourceGroupName}'
            """
        ));
        return await resources.Value.Data.ToObjectAsync<List<ResourcesQueryResult>>(new JsonObjectSerializer(Defaults.JsonSerializerOptions),
            CancellationToken.None) ?? [];
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
    }

    public async Task<List<ContainerRegistryQueryResult>> GetContainerRegistries()
    {
        var tenantResource = _client.GetTenants().First(t => t.Data.TenantId == SelectedTenant?.TenantId);
        var containerRegistries = await tenantResource.GetResourcesAsync(new ResourceQueryContent(ContainerRegistryQuery));
        return await containerRegistries.Value.Data.ToObjectAsync<List<ContainerRegistryQueryResult>>(new JsonObjectSerializer(Defaults.JsonSerializerOptions)) ?? [];
    }
}