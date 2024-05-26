using DevTools.Services.Azure;
using Spectre.Console;

namespace DevTools;

public static class DevToolsContext
{
    public static TenantSimplified? SelectedTenant { get; set; }
    public static SubscriptionSimplified? SelectedSubscription { get; set; }


    public static Tree RenderContext()
    {
        var root = new Tree("Context");
        return root;
    }
}