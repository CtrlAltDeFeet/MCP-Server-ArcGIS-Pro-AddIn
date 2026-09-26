using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGisBridge;
namespace APBridgeAddIn;
internal partial class ProBridgeService
{
    private static readonly AsyncLocal<string?> ActiveRequestProject = new();
    // Capture before queuing: do not rely on Pro's custom queue forwarding
    // ExecutionContext/AsyncLocal state to its main CIM thread.
    private static Task<T> CheckedRun<T>(Func<T> action)
    {
        var project = ActiveRequestProject.Value;
        return QueuedTask.Run(() => { ProjectRouting.RequireSameProject(project, Project.Current?.URI); return action(); });
    }
    private static Task<T> CheckedRun<T>(Func<Task<T>> action)
    {
        var project = ActiveRequestProject.Value;
        return QueuedTask.Run(async () => { ProjectRouting.RequireSameProject(project, Project.Current?.URI); return await action(); });
    }
    private static Task CheckedRun(Action action)
    {
        var project = ActiveRequestProject.Value;
        return QueuedTask.Run(() => { ProjectRouting.RequireSameProject(project, Project.Current?.URI); action(); });
    }
    private static Task CheckedRun(Func<Task> action)
    {
        var project = ActiveRequestProject.Value;
        return QueuedTask.Run(async () => { ProjectRouting.RequireSameProject(project, Project.Current?.URI); await action(); });
    }
}
