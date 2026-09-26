using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGisBridge;
namespace APBridgeAddIn;
internal class Module1 : Module
{
    private static Module1? _instance;
    private ProBridgeService? _service;
    private readonly int _pid = Environment.ProcessId;
    public static Module1 Current => _instance ??= (Module1)FrameworkApplication.FindModule("HardenedExtended_Module");
    public bool IsRunning => _service?.IsRunning == true;
    public void StartBridge()
    {
        if (IsRunning) return;
        StopBridge();
        _service = new ProBridgeService(BridgeProtocol.PipeName(_pid));
        _service.Start(); Register();
    }
    private void Register() => BridgeRegistry.Register(_pid, BridgeProtocol.PipeName(_pid), Project.Current?.URI, Project.Current?.Name);
    public void StopBridge() { _service?.Dispose(); _service = null; BridgeRegistry.Unregister(_pid); }
    protected override bool Initialize()
    {
        ProjectOpenedAsyncEvent.Subscribe(OnOpened); ProjectClosedEvent.Subscribe(OnClosed);
        return base.Initialize();
    }
    private Task OnOpened(ProjectEventArgs args) { if (IsRunning) Register(); return Task.CompletedTask; }
    private void OnClosed(ProjectEventArgs args) { if (IsRunning) BridgeRegistry.UpdateProject(_pid, null, null); }
    protected override void Uninitialize()
    {
        StopBridge(); ProjectOpenedAsyncEvent.Unsubscribe(OnOpened); ProjectClosedEvent.Unsubscribe(OnClosed);
        base.Uninitialize();
    }
    protected override bool CanUnload() { StopBridge(); return true; }
}
