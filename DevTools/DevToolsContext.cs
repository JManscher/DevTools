using DevTools.Services.Azure.Models;
using DevTools.Tools.AzureResourceTools;
using Spectre.Console;

namespace DevTools;

public static class DevToolsContext
{
    public static TenantSimplified? SelectedTenant { get; set; }
    public static SubscriptionSimplified? SelectedSubscription { get; set; }
    
    public static Tree TreeContext()
    {
        var root = new Tree("[bold]Current Context[/]");
        return root.AddTenantToContext().AddSubscriptionToContext();
    }
    
    public static void RenderHeader(this Tree tree)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("DevTools").Justify(Justify.Center).Color(Color.Green));

        var contextPanel = new Panel(tree)
        {
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(contextPanel);
    }
}