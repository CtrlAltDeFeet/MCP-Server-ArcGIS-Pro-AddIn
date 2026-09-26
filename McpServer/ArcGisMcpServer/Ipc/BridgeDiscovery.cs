using ArcGisBridge;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace ArcGisMcpServer.Ipc;
public static class BridgeDiscovery
{
    public static bool HttpMode => false;
    public static string? PinnedProject => Environment.GetEnvironmentVariable("ARCGIS_PROJECT") is { Length: > 0 } p ? p : null;
    private static volatile string? _runtimeOverride;
    public static string? RuntimeOverride { get => _runtimeOverride; set => _runtimeOverride = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }
    public static string? EffectivePin => PinnedProject ?? RuntimeOverride;
    public static BridgeEntry Resolve() => SelectCurrent(ReadAllLive()) ?? throw new IOException("No matching hardened bridge. Open a saved ArcGIS Pro project and click Start / stop extended bridge. Check ARCGIS_PROJECT if pinned.");
    public static string Discover() => Resolve().PipeName;
    public static BridgeEntry? SelectCurrent(IReadOnlyList<BridgeEntry> entries) => ProjectRouting.Select(entries, e => e.ProjectPath, e => e.ProjectName, EffectivePin);
    public static List<BridgeEntry> ReadAllLive()
    {
        var entries = new List<BridgeEntry>();
        if (!Directory.Exists(BridgeProtocol.RegistryDirectory)) return entries;
        foreach (var file in Directory.EnumerateFiles(BridgeProtocol.RegistryDirectory, "*.json").Take(128))
        {
            try
            {
                if (new FileInfo(file).Length > 16384) continue;
                var entry = JsonSerializer.Deserialize<BridgeEntry>(File.ReadAllText(file));
                if (entry == null || entry.PipeName != BridgeProtocol.PipeName(entry.Pid)) continue;
                using var process = Process.GetProcessById(entry.Pid);
                if (process.HasExited || process.ProcessName != "ArcGISPro") continue;
                entries.Add(entry);
            }
            catch (Exception) { /* Skip stale/corrupt entries; actual pipe identity is verified on connection. */ }
        }
        return entries;
    }
    public class BridgeEntry
    {
        [JsonPropertyName("pid")] public int Pid { get; set; }
        [JsonPropertyName("pipeName")] public string PipeName { get; set; } = "";
        [JsonPropertyName("projectPath")] public string? ProjectPath { get; set; }
        [JsonPropertyName("projectName")] public string? ProjectName { get; set; }
        [JsonPropertyName("startedUtc")] public string StartedUtc { get; set; } = "";
    }
}
