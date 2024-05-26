namespace DevTools.Services.Azure.Models;

public record AzCliSubscription(
    string EnvironmentName,
    string HomeTenantId,
    string Id,
    bool IsDefault,
    string Name,
    string State,
    string TenantDisplayName,
    Guid TenantId
);