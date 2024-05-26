using System.Text.Json;

namespace DevTools;

public static class Defaults
{
    public static JsonSerializerOptions JsonSerializerOptions => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

}