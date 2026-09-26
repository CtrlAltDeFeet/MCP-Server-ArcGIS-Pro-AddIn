using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace ArcGisBridge;
public static class BridgeProtocol
{
    public const int MaxRequestBytes = 1024 * 1024;
    public const int MaxResponseBytes = 4 * 1024 * 1024;
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan OperationTimeout = TimeSpan.FromMinutes(10);
    // AppData may be virtualized for packaged desktop hosts. Both endpoints
    // must use the same physical per-user directory across client applications.
    public static string StateDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".arcgis-mcp-extended");
    public static string RegistryDirectory => Path.Combine(StateDirectory, "bridges");
    public static string PipeName(int pid) => $"ArcGisProMcp-Hardened-v3-{WindowsIdentity.GetCurrent().User!.Value}-{pid}";
    private static readonly JsonSerializerOptions Json = new() { MaxDepth = 64, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
    public static async Task<T> ReadAsync<T>(Stream stream, int limit, CancellationToken ct)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, ct);
        int length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length <= 0 || length > limit) throw new InvalidDataException("Bridge frame exceeds the permitted size.");
        var bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, ct);
        return JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new InvalidDataException("Empty bridge message.");
    }
    public static async Task WriteAsync<T>(Stream stream, T value, int limit, CancellationToken ct)
    {
        using var buffer = new BoundedBuffer(limit);
        JsonSerializer.Serialize(buffer, value, Json);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, checked((int)buffer.Length));
        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(buffer.GetBuffer().AsMemory(0, checked((int)buffer.Length)), ct);
        await stream.FlushAsync(ct);
    }
    // Deliberately no retry: a missing response does not prove a write failed.
    public static async Task<TResponse> ExchangeOnceAsync<TRequest,TResponse>(Stream stream, TRequest request, CancellationToken ct)
    {
        await WriteAsync(stream, request, MaxRequestBytes, ct);
        return await ReadAsync<TResponse>(stream, MaxResponseBytes, ct);
    }
    private sealed class BoundedBuffer(int limit) : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) { Check(count); base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Check(buffer.Length); base.Write(buffer); }
        private void Check(int count) { if (Length + count > limit) throw new InvalidDataException("Bridge response is too large; reduce rows or fields."); }
    }
}
// One reserved instance bounds connections. Actual native work is awaited even
// after its cancellation deadline so it cannot pile up in Pro's queue.
public sealed class BridgeServer<TRequest, TResponse> : IDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly Func<TRequest, CancellationToken, Task<TResponse>> _handle;
    private readonly Func<string, TResponse> _failure;
    private readonly CancellationTokenSource _stop = new();
    private readonly TimeSpan _idle;
    private Task? _loop;
    public bool IsRunning => _loop is { IsCompleted: false };
    public BridgeServer(string name, Func<TRequest, CancellationToken, Task<TResponse>> handle, Func<string, TResponse> failure, TimeSpan? idleTimeout = null)
    {
        _handle = handle; _failure = failure; _idle = idleTimeout ?? BridgeProtocol.IdleTimeout;
        _pipe = new(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance);
    }
    public void Start() => _loop ??= RunAsync();
    private async Task RunAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await _pipe.WaitForConnectionAsync(_stop.Token);
                try
                {
                    using var read = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    read.CancelAfter(_idle);
                    var request = await BridgeProtocol.ReadAsync<TRequest>(_pipe, BridgeProtocol.MaxRequestBytes, read.Token);
                    TResponse response;
                    using var operation = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    operation.CancelAfter(BridgeProtocol.OperationTimeout);
                    try { response = await _handle(request, operation.Token); }
                    catch (Exception ex) { response = _failure(ex.Message); }
                    using var write = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    write.CancelAfter(_idle);
                    try { await BridgeProtocol.WriteAsync(_pipe, response, BridgeProtocol.MaxResponseBytes, write.Token); }
                    catch (InvalidDataException ex) { await BridgeProtocol.WriteAsync(_pipe, _failure(ex.Message), BridgeProtocol.MaxResponseBytes, write.Token); }
                }
                catch (Exception) { /* A bad connection cannot fault the listener. */ }
                finally
                {
                    // A peer can disconnect before IsConnected is sampled. The
                    // server instance still needs Disconnect to return to idle.
                    try { if (!_stop.IsCancellationRequested) _pipe.Disconnect(); }
                    catch (IOException) { }
                }
            }
        }
        catch (Exception) when (_stop.IsCancellationRequested) { }
    }
    public void Dispose() { _stop.Cancel(); _pipe.Dispose(); }
}
