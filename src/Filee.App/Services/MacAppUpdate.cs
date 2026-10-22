// macOS in-app update: the verified release zip (UpdateService.DownloadInstallerAsync) is unpacked, the app in it is
// checked (signature, bundle id, version), and a small script swaps it in for the running Trasmuta.app once this
// process has quit, then opens it. Files downloaded by the app itself carry no quarantine flag, so the new version
// opens without Gatekeeper's "Open Anyway"; its signature requires only the bundle id, so the Accessibility
// permission stays granted.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Filee.App.Services;

/// <summary>Whether a Mac copy of Trasmuta can update itself.</summary>
public enum MacSelfUpdate
{
    /// <summary>Not a published Trasmuta.app (Windows, Linux, run from source): updates are downloaded by hand.</summary>
    NotAnApp,
    Ready,
    /// <summary>
    /// Its folder is read-only and outside Applications: macOS runs it from a copy (opened from Downloads) or a disk
    /// image. Moving it to Applications (<see cref="MacAppMove"/>) lets it update.
    /// </summary>
    NeedsMove,
    /// <summary>In Applications, but this account may not change that folder (not an administrator).</summary>
    NoPermission,
}

internal static class MacAppUpdate
{
    /// <summary>The .app the running process belongs to (…/Trasmuta.app/Contents/MacOS/ → …/Trasmuta.app), or null.</summary>
    public static string? RunningBundle { get; } = OperatingSystem.IsMacOS() ? BundleOf(AppContext.BaseDirectory) : null;

    /// <summary>The end of the release zip for this Mac: "-osx-arm64.zip" on Apple silicon, "-osx-x64.zip" on Intel.</summary>
    public static string ZipSuffix => RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "-osx-arm64.zip" : "-osx-x64.zip";

    /// <summary>A published build in an .app whose folder the user may write: it can replace itself.</summary>
    public static bool CanReplace { get; } = RunningBundle is { } bundle && UpdateService.IsReleaseBuild &&
                                             IsWritable(Path.GetDirectoryName(bundle)!);

    /// <summary>Whether this copy can update itself and, when it can't, why (the update notice says so).</summary>
    public static MacSelfUpdate State { get; } = StateOf(UpdateService.IsReleaseBuild ? RunningBundle : null, CanReplace, Home);

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    internal static MacSelfUpdate StateOf(string? publishedBundle, bool canReplace, string home) =>
        publishedBundle is null ? MacSelfUpdate.NotAnApp
        : canReplace ? MacSelfUpdate.Ready
        : IsInApplications(publishedBundle, home) ? MacSelfUpdate.NoPermission
        : MacSelfUpdate.NeedsMove;

    /// <summary>
    /// True for an app in /Applications or ~/Applications (or a folder in them). Not for one in Downloads, on a disk
    /// image, or one macOS runs from a read-only copy because it was opened from Downloads (App Translocation).
    /// </summary>
    internal static bool IsInApplications(string bundle, string home) =>
        bundle.StartsWith("/Applications/", StringComparison.Ordinal) ||
        bundle.StartsWith(home.TrimEnd('/') + "/Applications/", StringComparison.Ordinal);

    internal static string? BundleOf(string baseDirectory)
    {
        var macOS = Path.TrimEndingDirectorySeparator(baseDirectory);
        var contents = Path.GetDirectoryName(macOS);
        var bundle = contents is null ? null : Path.GetDirectoryName(contents);
        return Path.GetFileName(macOS) == "MacOS" && Path.GetFileName(contents) == "Contents" &&
               bundle is not null && bundle.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
            ? bundle
            : null;
    }

    internal static bool IsWritable(string folder)
    {
        try
        {
            var probe = Path.Combine(folder, $".{Filee.Core.AppIdentity.Name}-write-{Guid.NewGuid():N}");
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Unpacks <paramref name="zip"/> next to it and checks the app inside: a valid signature, this app's bundle id and
    /// <paramref name="version"/>. Returns its path; throws <see cref="InvalidDataException"/> when it isn't that app.
    /// </summary>
    public static async Task<string> PrepareAsync(string zip, string version)
    {
        var stage = Path.Combine(Path.GetDirectoryName(zip)!, "stage");
        if (Directory.Exists(stage))
            Directory.Delete(stage, recursive: true);
        Directory.CreateDirectory(stage);
        // ditto keeps the signatures (extended attributes of the assemblies) and the executable bits.
        await RunAsync("/usr/bin/ditto", "-x", "-k", zip, stage);
        var app = Path.Combine(stage, Filee.Core.AppIdentity.Name + ".app");
        if (!Directory.Exists(app))
            throw new InvalidDataException($"{Path.GetFileName(zip)} holds no {Filee.Core.AppIdentity.Name}.app");
        await RunAsync("/usr/bin/codesign", "--verify", "--deep", "--strict", app);
        var plist = Path.Combine(app, "Contents", "Info.plist");
        var id = await RunAsync("/usr/libexec/PlistBuddy", "-c", "Print :CFBundleIdentifier", plist);
        var shipped = await RunAsync("/usr/libexec/PlistBuddy", "-c", "Print :CFBundleShortVersionString", plist);
        if (id != Filee.Core.AppIdentity.BundleId || shipped != version)
            throw new InvalidDataException($"The downloaded app is {id} {shipped}, not {Filee.Core.AppIdentity.BundleId} {version}");
        return app;
    }

    /// <summary>
    /// The script that swaps the apps: it waits for the process <c>$1</c> to exit, moves <c>$2</c> (the new app) in
    /// place of <c>$3</c> (the running one), keeping the old one until the new one is in place, and opens the app.
    /// </summary>
    internal const string SwapScript = """
        #!/bin/sh
        # Trasmuta update: replaces the app once the running copy has quit, then opens it.
        pid="$1"; new="$2"; app="$3"
        opener="${TRASMUTA_OPEN:-open}"  # tests replace it, so they open nothing
        tries=0
        while kill -0 "$pid" 2>/dev/null; do
            tries=$((tries + 1))
            [ "$tries" -gt 300 ] && exit 1  # still running after a minute: leave everything as it is
            sleep 0.2
        done
        incoming="$app.incoming"; previous="$app.previous"
        rm -rf "$incoming" "$previous"
        mv "$new" "$incoming" 2>/dev/null || /usr/bin/ditto "$new" "$incoming" || { $opener "$app"; exit 1; }
        if mv "$app" "$previous"; then
            if mv "$incoming" "$app"; then
                rm -rf "$previous"
            else
                mv "$previous" "$app"
            fi
        fi
        rm -rf "$incoming"
        $opener "$app"
        """;

    /// <summary>Starts the swap script for <paramref name="newApp"/>; it does its work once this process has quit.</summary>
    public static void StartSwap(string newApp, string bundle)
    {
        var script = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(newApp)!)!, "swap.sh");
        File.WriteAllText(script, SwapScript);
        var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
        foreach (var argument in new[] { script, Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture), newApp, bundle })
            start.ArgumentList.Add(argument);
        using var _ = Process.Start(start) ?? throw new InvalidOperationException("The update script didn't start");
    }

    /// <summary>Runs a tool and returns its trimmed output; throws <see cref="InvalidDataException"/> when it fails.</summary>
    internal static async Task<string> RunAsync(string tool, params string[] arguments)
    {
        var start = new ProcessStartInfo(tool) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new Win32Exception($"{tool} didn't start");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidDataException($"{Path.GetFileName(tool)} failed ({process.ExitCode}): {(await error).Trim()}");
        return (await output).Trim();
    }
}
