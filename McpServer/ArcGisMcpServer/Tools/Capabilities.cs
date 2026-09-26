using ArcGisBridge;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
namespace ArcGisMcpServer.Tools;
public partial class ProTools
{
    [McpServerTool(ReadOnly=true, Destructive=false), Description("Show enabled local capabilities, output directory, disabled operations, and transport limits. Policy changes require the operator to edit policy.json and restart the Pro bridge.")]
    public static string GetCapabilities()
    {
        var policy = BridgePolicy.Load();
        return JsonSerializer.Serialize(new
        {
            version="3.0.1", transport="local stdio", enabled=policy.Capabilities,
            outputRoot=policy.OutputRoot, policyPath=BridgePolicy.PolicyPath,
            operations=OperationCatalog.Capabilities,
            maxRequestBytes=BridgeProtocol.MaxRequestBytes, maxResponseBytes=BridgeProtocol.MaxResponseBytes,
            note="The Pro bridge snapshots policy at start. Restart it after policy edits. Automation and Python are full-trust capabilities, not sandboxed. Tool output can enter the AI provider's context."
        });
    }
}
