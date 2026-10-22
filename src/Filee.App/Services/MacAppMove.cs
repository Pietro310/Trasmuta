// Moving Trasmuta.app into Applications. An app opened from Downloads keeps its quarantine flag, and macOS runs it
// from a read-only copy (App Translocation): it can't update itself there. On start Trasmuta offers the move; the
// update notice does it before updating. The app is copied (ditto keeps its signature) without the quarantine flag,
// put in place of an older copy, the one in Downloads goes to the Trash, and the new one opens once this has quit.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Filee.App.Services;

internal static class MacAppMove
{
    /// <summary>
    /// A published Trasmuta.app outside the Applications folders (Downloads, a disk image, a translocated copy): the
    /// start offers to move it.
    /// </summary>
    public static bool ShouldOffer => UpdateService.IsReleaseBuild && MacAppUpdate.RunningBundle is { } bundle &&
                                      !MacAppUpdate.IsInApplications(bundle, Home);

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>/Applications when this account may change it (administrators), else Applications in the home folder.</summary>
    internal static string TargetFolder(string home, Func<string, bool> writable) =>
        writable("/Applications") ? "/Applications" : Path.Combine(home, "Applications");

    /// <summary>
    /// Copies the running app into Applications and starts the script that opens it once this process has quit (with
    /// <paramref name="update"/>, the new copy installs the latest version at once). The caller quits next. Throws
    /// <see cref="IOException"/>, <see cref="UnauthorizedAccessException"/> or <see cref="InvalidDataException"/>
    /// when the copy fails; the running app is left as it was.
    /// </summary>
    public static async Task<string> MoveAsync(ILogger log, bool update)
    {
        var bundle = MacAppUpdate.RunningBundle ?? throw new InvalidOperationException("Not running from an .app");
        var original = OperatingSystem.IsMacOS() ? Filee.Platform.MacOS.MacOSBundle.OriginalPath(bundle) : bundle;
        var folder = TargetFolder(Home, path => Directory.Exists(path) && MacAppUpdate.IsWritable(path));
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, Filee.Core.AppIdentity.Name + ".app");
        await InstallCopyAsync(bundle, target, CopyAsync);
        log.LogInformation("Moved {Bundle} (from {Original}) to {Target}", bundle, original ?? "an unknown folder", target);

        // The copy left in Downloads would open the old version again; one on a disk image can't be removed.
        if (OperatingSystem.IsMacOS() && original is not null && original != target && Directory.Exists(original)
            && !original.StartsWith("/Volumes/", StringComparison.Ordinal)
            && !Filee.Platform.MacOS.MacOSBundle.MoveToTrash(original))
            log.LogInformation("Could not move {Original} to the Trash", original);
        StartRelaunch(target, update);
        return target;
    }

    /// <summary>
    /// Copies <paramref name="bundle"/> to <paramref name="target"/> through <paramref name="copy"/>, replacing what is
    /// there only once the copy is complete; an earlier app stays when anything fails.
    /// </summary>
    internal static async Task InstallCopyAsync(string bundle, string target, Func<string, string, Task> copy)
    {
        var incoming = target + ".incoming";
        var previous = target + ".previous";
        DeleteFolder(incoming);
        DeleteFolder(previous);
        try
        {
            await copy(bundle, incoming);
        }
        catch
        {
            TryDeleteFolder(incoming);
            throw;
        }
        if (Directory.Exists(target))
            Directory.Move(target, previous);
        try
        {
            Directory.Move(incoming, target);
        }
        catch
        {
            if (Directory.Exists(previous))
                Directory.Move(previous, target);
            TryDeleteFolder(incoming);
            throw;
        }
        TryDeleteFolder(previous);
    }

    /// <summary>ditto keeps the signatures and executable bits; only the quarantine flag is taken off the copy.</summary>
    private static async Task CopyAsync(string bundle, string target)
    {
        await MacAppUpdate.RunAsync("/usr/bin/ditto", bundle, target);
        try
        {
            await MacAppUpdate.RunAsync("/usr/bin/xattr", "-dr", "com.apple.quarantine", target);
        }
        catch (InvalidDataException)
        {
            // Nothing to take off (the app wasn't quarantined): xattr says so with an error.
        }
    }

    /// <summary>Waits for the process <c>$1</c> to exit, then opens the app <c>$2</c>, passing it the rest.</summary>
    internal const string RelaunchScript = """
        #!/bin/sh
        # Trasmuta moved to Applications: opens the new copy once the old one has quit.
        pid="$1"; app="$2"; shift 2
        opener="${TRASMUTA_OPEN:-open}"  # tests replace it, so they open nothing
        tries=0
        while kill -0 "$pid" 2>/dev/null; do
            tries=$((tries + 1))
            [ "$tries" -gt 300 ] && exit 1
            sleep 0.2
        done
        if [ "$#" -gt 0 ]; then $opener "$app" --args "$@"; else $opener "$app"; fi
        """;

    private static void StartRelaunch(string target, bool update)
    {
        var script = Path.Combine(Path.GetTempPath(), Filee.Core.AppIdentity.Name + "-relaunch.sh");
        File.WriteAllText(script, RelaunchScript);
        var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
        foreach (var argument in new[] { script, Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture), target })
            start.ArgumentList.Add(argument);
        if (update)
            start.ArgumentList.Add("--update");
        using var _ = Process.Start(start) ?? throw new IOException("The relaunch script didn't start");
    }

    private static void DeleteFolder(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private static void TryDeleteFolder(string path)
    {
        try
        {
            DeleteFolder(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A leftover next to the app, removed by the next move.
        }
    }
}
