using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGisBridge;
namespace APBridgeAddIn;
internal class Button1 : Button
{
    protected override void OnClick()
    {
        try
        {
            var module = Module1.Current;
            if (module.IsRunning) { module.StopBridge(); MessageBox.Show("Extended bridge stopped. Already-running native work may continue.", "ArcGIS MCP"); }
            else
            {
                module.StartBridge();
                MessageBox.Show("Extended hardened bridge started. Enabled capabilities: " + string.Join(", ", BridgePolicy.Load().Capabilities) + ". Click again to stop.", "ArcGIS MCP");
            }
        }
        catch (Exception ex) { MessageBox.Show("Could not start extended bridge: " + ex.Message, "ArcGIS MCP"); }
    }
}
