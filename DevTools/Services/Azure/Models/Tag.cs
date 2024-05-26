namespace DevTools.Services.Azure.Models;

public record Tag(string Name, string Digest)
{
    public Version? Version => Version.TryParse(Name, out var version) ? version : null; 
}