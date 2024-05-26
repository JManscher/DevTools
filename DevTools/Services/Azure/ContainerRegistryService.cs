using Azure.Containers.ContainerRegistry;
using Azure.Identity;
using DevTools.Services.Azure;

namespace BicepContainerRegistryTool.AzureService;

public class ContainerRegistryService
{
    private readonly ContainerRegistryClient _client;
    
    private readonly IDictionary<string, object> _repositoryCache = new Dictionary<string, object>();
    public ContainerRegistryService(ContainerRegistryQuery containerRegistryUri)
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



    public async Task<List<string>> GetTagsAsync(string repositoryName)
    {
        var tags = new List<Version>();
        var nonVersionTags = new List<string>();
        await foreach (var manifest in _client.GetRepository(repositoryName).GetAllManifestPropertiesAsync())
        {
            foreach (var tag in manifest.Tags)
            {
                if (Version.TryParse(tag, out var version))
                {
                    tags.Add(version);
                }
                else
                {
                    nonVersionTags.Add(tag);
                }
            }
        }

        // Sort the versions in descending order and convert them back to strings.
        return tags.OrderByDescending(v => v).Select(v => v.ToString()).Concat(nonVersionTags).ToList();
    }
}