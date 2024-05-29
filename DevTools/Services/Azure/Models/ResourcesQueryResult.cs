using Azure.ResourceManager.Models;

namespace DevTools.Models
{
    public record ResourcesQueryResult
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required string ResourceGroup { get; set; }
        public string? Kind { get; set; }
        public Sku? Sku { get; set; }
    }
}