// The name this build goes by: Trasmuta, a fork of Filee. Everything that must not collide with an installed Filee
// (data and engine folders, temp folders, the single-instance mutex, the macOS bundle id) is derived from here, so the
// two apps run side by side. Namespaces and assembly names stay "Filee.*" to keep merging upstream changes easy.

namespace Filee.Core;

public static partial class AppIdentity
{
    /// <summary>Display name, and the folder name under Application Support and the temp folder.</summary>
    public const string Name = "Trasmuta";

    /// <summary>macOS bundle identifier (also the LaunchAgent label and the Services status key).</summary>
    public const string BundleId = "com.trasmuta.app";

    /// <summary>Lower-case name for files, folders and commands: Linux desktop files, the CLI link ("trasmuta").</summary>
    public const string Slug = "trasmuta";

    /// <summary>Publisher shown by Windows (Apps &amp; Features, the Explorer menu package).</summary>
    public const string Publisher = "Pietro310";

    /// <summary>
    /// The translations (shared with upstream Filee) name the app "Filee": shows <see cref="Name"/> instead, leaving
    /// URLs (github.com/KnifeLemon/Filee), file names (Filee.app) and identifiers (Filee.Core) alone.
    /// </summary>
    public static string WithName(string text) =>
        text.Contains("Filee", StringComparison.Ordinal) ? UpstreamName().Replace(text, Name) : text;

    [System.Text.RegularExpressions.GeneratedRegex(@"(?<![/\w])Filee(?!\.\w)\b")]
    private static partial System.Text.RegularExpressions.Regex UpstreamName();
}
