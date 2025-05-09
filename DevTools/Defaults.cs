using System.Text.Json;

namespace DevTools;

public static class Defaults
{
    public static JsonSerializerOptions JsonSerializerOptions =>
        new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
}
