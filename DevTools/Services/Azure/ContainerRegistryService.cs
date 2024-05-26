using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Azure.Containers.ContainerRegistry;
using Azure.Core.Serialization;
using Azure.Identity;
using DevTools.Services.Azure.Models;

namespace DevTools.Services.Azure;

public class ContainerRegistryService
{
    private readonly ContainerRegistryClient _client;
    private const string BicepLayer = "application/vnd.ms.bicep.module.layer.v1+json";
    private const string OciImageManifestMediaType = "application/vnd.oci.image.manifest.v1+json";

    public ContainerRegistryService(ContainerRegistryQueryResult containerRegistryUri)
    {
        var endpoint = new Uri($"https://{containerRegistryUri.LoginServer}");      
        _client = new ContainerRegistryClient(
            endpoint,
            new DefaultAzureCredential(),
            new ContainerRegistryClientOptions()
            {
                Audience = ContainerRegistryAudience.AzureResourceManagerPublicCloud
            });
    }
    
    public async Task<List<string>> GetRepositoriesAsync()
    {
        var repositories = new List<string>();
        await foreach (var repository in _client.GetRepositoryNamesAsync())
        {
            repositories.Add(repository);
        }
        return repositories;
    }
    
    public async Task<List<Tag>> GetTagsAsync(string repositoryName)
    {
        var tags = new List<Tag>();
        await foreach (var manifest in _client.GetRepository(repositoryName).GetAllManifestPropertiesAsync())
        {
            tags.AddRange(manifest.Tags.Select(tag => new Tag(tag, manifest.Digest)));
        }

        // Sort the versions in descending order and convert them back to strings.
        return tags.OrderByDescending(v => v.Version).ToList();
    }
    
    public async Task<JsonObject> GetBicep(string repositoryName, Tag tag)
    {
        var contentClient = new ContainerRegistryContentClient(_client.Endpoint, repositoryName, new AzureCliCredential());
        var manifestResponse = await contentClient.GetManifestAsync(tag.Name);
        
        if(manifestResponse.HasValue is false)
        {
            throw new InvalidOperationException("Manifest not found");
        }

        var manifest = await manifestResponse.GetRawResponse().Content
            .ToObjectAsync<ContainerManifest>(new JsonObjectSerializer(Defaults.JsonSerializerOptions));
        
        if(manifest!.MediaType != OciImageManifestMediaType)
        {
            throw new InvalidOperationException("Manifest is not a valid OCI image manifest");
        }

        var contentLayer = manifest.Layers.First(l => l.MediaType == BicepLayer);

        var contentResponse = await contentClient.DownloadBlobStreamingAsync(contentLayer.Digest);
        
        if(contentResponse.HasValue is false)
        {
            throw new InvalidOperationException("Content not found");
        }

        var ms = new MemoryStream();
        await contentResponse.Value.Content.CopyToAsync(ms);
        ms.Position = 0; // Reset the position of the stream to the beginning

        var bicepParam = await JsonSerializer.DeserializeAsync<JsonObject>(ms, Defaults.JsonSerializerOptions);
        
        return bicepParam!;
    }
}