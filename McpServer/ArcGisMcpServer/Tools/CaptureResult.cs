using ArcGisBridge;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
namespace ArcGisMcpServer.Tools;
public static class CaptureResult
{
    public static async Task<CallToolResult> CreateAsync(string path, string description)
    {
        path = BridgePolicy.Load().ValidateOutput(path, allowExisting: true);
        if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new McpException("Capture must be PNG.");
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > 4 * 1024 * 1024)
            return new() { Content = [new TextContentBlock { Text = description + "\nPNG exceeds the inline image limit; recapture at smaller dimensions." }] };
        var bytes = new byte[checked((int)file.Length)]; await file.ReadExactlyAsync(bytes);
        if (bytes.Length < 8 || !bytes.AsSpan(0,8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}))
            throw new McpException("Capture is not a valid PNG stream.");
        return new() { Content = [new TextContentBlock { Text = description }, ImageContentBlock.FromBytes(bytes,"image/png")] };
    }
}
