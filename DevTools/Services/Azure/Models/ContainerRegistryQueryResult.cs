namespace DevTools.Services.Azure.Models;

public record ContainerRegistryQueryResult(string Name, string Type, string TenantId, string ResourceGroup, string LoginServer);