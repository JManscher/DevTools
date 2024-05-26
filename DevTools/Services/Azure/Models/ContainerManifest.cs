namespace DevTools.Services.Azure.Models;

public record ContainerManifest(
    int SchemaVersion,
    string MediaType,
    string ArtifactType,
    Config Config,
    Layers[] Layers
);

public record Config(
    string MediaType,
    string Digest,
    int Size
);


public record Layers(
    string MediaType,
    string Digest,
    int Size
);

