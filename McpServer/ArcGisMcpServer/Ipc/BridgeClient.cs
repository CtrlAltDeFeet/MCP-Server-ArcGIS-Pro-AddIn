using ArcGisBridge;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace ArcGisMcpServer.Ipc;
public sealed class BridgeClient
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);
    public async Task<IpcResponse> SendAsync(IpcRequest request, CancellationToken ct = default)
    {
        bool transmissionStarted = false;
        try
        {
            BridgePolicy.Load().Authorize(request.Op, request.Args);
            var entry = BridgeDiscovery.Resolve();
            request = request with { ProjectPath = entry.ProjectPath };
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(BridgeProtocol.OperationTimeout + TimeSpan.FromSeconds(10));
            using var client = new NamedPipeClientStream(".", entry.PipeName, PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, TokenImpersonationLevel.Anonymous);
            await client.ConnectAsync(3000, deadline.Token);
            VerifyServer(client, entry.Pid);
            transmissionStarted = true;
            return await BridgeProtocol.ExchangeOnceAsync<IpcRequest,IpcResponse>(client, request, deadline.Token);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new(false, transmissionStarted
                ? $"Outcome uncertain: {ex.Message}. The operation may have completed or may still be running. Inspect the project before retrying; this server never automatically replays requests."
                : ex.Message, null);
        }
    }
    public static void VerifyServer(NamedPipeClientStream client, int expectedPid)
    {
        if (!GetNamedPipeServerProcessId(client.SafePipeHandle, out uint pid) || pid != expectedPid)
            throw new IOException("The bridge server process does not match discovery.");
        using var process = Process.GetProcessById(checked((int)pid));
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ArcGIS", "Pro", "bin", "ArcGISPro.exe");
        if (!string.Equals(process.MainModule?.FileName, expected, StringComparison.OrdinalIgnoreCase))
            throw new IOException("The pipe is not owned by the installed ArcGIS Pro executable.");
    }
    public Task<IpcResponse> OpAsync(string op, Dictionary<string, string>? args = null, CancellationToken ct = default) => SendAsync(new(op, args), ct);
}
