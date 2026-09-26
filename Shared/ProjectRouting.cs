using System.IO;
namespace ArcGisBridge;
public static class ProjectRouting
{
    public static bool Matches(string? projectPath, string? projectName, string pin)
    {
        if (Path.IsPathFullyQualified(pin))
            return !string.IsNullOrWhiteSpace(projectPath) && string.Equals(Path.GetFullPath(projectPath), Path.GetFullPath(pin), StringComparison.OrdinalIgnoreCase);
        if (pin.Contains('\\') || pin.Contains('/')) throw new InvalidOperationException("Project pins must be absolute paths or a unique project name.");
        var name = Path.GetFileNameWithoutExtension(pin.Trim());
        return string.Equals(Path.GetFileNameWithoutExtension(projectName), name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetFileNameWithoutExtension(projectPath), name, StringComparison.OrdinalIgnoreCase);
    }
    public static T? Select<T>(IEnumerable<T> items, Func<T, string?> path, Func<T, string?> name, string? pin) where T : class
    {
        var matches = (pin == null ? items : items.Where(i => Matches(path(i), name(i), pin))).ToArray();
        if (matches.Length > 1) throw new InvalidOperationException("Ambiguous Pro project. Pin an exact absolute .aprx path and use only one bridge for that project.");
        return matches.SingleOrDefault();
    }
    public static void RequireSameProject(string? requested, string? current)
    {
        if (string.IsNullOrWhiteSpace(requested) || string.IsNullOrWhiteSpace(current)
            || !string.Equals(Path.GetFullPath(requested), Path.GetFullPath(current), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Project changed or no saved project is open. Discover the bridge again before retrying.");
    }
    public static void RequireUniqueName(IEnumerable<string> names, string requested, string kind)
    {
        if (names.Count(n => n.Equals(requested, StringComparison.OrdinalIgnoreCase)) > 1)
            throw new InvalidOperationException($"Ambiguous {kind} '{requested}'. Rename duplicates in Pro before proceeding.");
    }
}
