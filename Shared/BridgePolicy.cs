using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace ArcGisBridge;

public sealed class BridgePolicy
{
    public string[] Capabilities { get; set; } = ["Inspect", "View", "Cartography", "Export"];
    public string OutputRoot { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ArcGISMcpOutputs");
    public static string PolicyPath => Path.Combine(BridgeProtocol.StateDirectory, "policy.json");
    public static readonly string[] KnownCapabilities = ["Inspect", "View", "Cartography", "Export", "Editing", "Automation", "Python"];
    private static readonly JsonSerializerOptions Options = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 8 };

    public static BridgePolicy Load()
    {
        try
        {
            using var stream = new FileStream(PolicyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > 16384) throw new InvalidDataException("Policy file exceeds 16 KiB.");
            using var reader = new StreamReader(stream);
            return Parse(reader.ReadToEnd());
        }
        catch (FileNotFoundException) { return new(); }
        catch (DirectoryNotFoundException) { return new(); }
        // Access and I/O errors must not silently substitute a more permissive
        // default profile for an unreadable operator policy.
    }
    public static BridgePolicy Parse(string json)
    {
        var policy = JsonSerializer.Deserialize<BridgePolicy>(json, Options) ?? throw new InvalidDataException("Empty policy.");
        if (policy.Capabilities == null || policy.Capabilities.Any(c => !KnownCapabilities.Contains(c, StringComparer.Ordinal)))
            throw new InvalidDataException("Policy contains an unknown capability.");
        if (!Path.IsPathFullyQualified(policy.OutputRoot) || policy.OutputRoot.StartsWith("\\\\"))
            throw new InvalidDataException("OutputRoot must be an absolute local drive path.");
        return policy;
    }
    public void Authorize(string op, Dictionary<string, string>? args)
    {
        if (string.IsNullOrWhiteSpace(op) || !OperationCatalog.Capabilities.TryGetValue(op, out var capability))
            throw new InvalidOperationException("Unsupported bridge operation.");
        if (!Capabilities.Contains(capability, StringComparer.Ordinal))
            throw new InvalidOperationException($"Capability '{capability}' is disabled. The operator can change policy.json and restart the bridge; MCP tools cannot enable capabilities.");
        if (args != null && (args.Count > 64 || args.Any(kv => kv.Key.Length > 64 || kv.Value == null || kv.Value.Length > 524288)))
            throw new InvalidDataException("Invalid or oversized operation arguments.");
        if (capability == "Export")
        {
            if (args == null) throw new InvalidDataException("Export arguments required.");
            if (op == "pro.captureMapView" && (!args.TryGetValue("output", out var capture) || string.IsNullOrWhiteSpace(capture)))
                args["output"] = Path.Combine(OutputRoot, $"map-{Guid.NewGuid():N}.png");
            if (!args.TryGetValue("output", out var output) || string.IsNullOrWhiteSpace(output))
                throw new InvalidDataException("Explicit output path required.");
            args["output"] = ValidateOutput(output);
        }
    }
    public string ValidateOutput(string value, bool allowExisting = false)
    {
        if (!Path.IsPathFullyQualified(value) || value.StartsWith("\\\\") || value.IndexOf(':', 2) >= 0)
            throw new InvalidDataException("Output must be an absolute local path without device or alternate-stream syntax.");
        var path = Path.GetFullPath(value);
        foreach (var segment in path[Path.GetPathRoot(path)!.Length..].Split(Path.DirectorySeparatorChar))
        {
            var stem = segment.Split('.')[0].ToUpperInvariant();
            if (segment.EndsWith(' ') || segment.EndsWith('.') ||
                stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
                System.Text.RegularExpressions.Regex.IsMatch(stem, "^(COM|LPT)[1-9¹²³]$"))
                throw new InvalidDataException("Ambiguous Windows path components and device names are not allowed.");
        }
        var root = Path.GetFullPath(OutputRoot).TrimEnd('\\', '/');
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Output must be inside the configured OutputRoot '{root}'. Rejected path: '{path}'.");
        if (!allowExisting && (File.Exists(path) || Directory.Exists(path))) throw new IOException("Refusing to overwrite an existing output.");
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Reparse points are not allowed in output paths.");
        }
        return path;
    }
}
