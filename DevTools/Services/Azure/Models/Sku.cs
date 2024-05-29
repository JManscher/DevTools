namespace DevTools;

public record class Sku(string? Name, string? Tier, string? Size, string? Family, string? Model, int? Capacity)
{
    public override string ToString() => $"Name: {Name}, Capacity: {Capacity?.ToString() ?? "N/A"}, Tier: {Tier ?? "N/A"}, Size: {Size ?? "N/A"}";
}
