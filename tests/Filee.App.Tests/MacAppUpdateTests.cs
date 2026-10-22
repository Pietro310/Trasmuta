// Trasmuta's macOS in-app update (MacAppUpdate): finding the running bundle, picking this Mac's zip from a release,
// and the script that swaps the apps once the old one has quit.

using System.Diagnostics;
using System.Text.Json;
using Filee.App.Services;

namespace Filee.App.Tests;

public class MacAppUpdateTests
{
    [Fact]
    public void The_bundle_is_found_from_its_executable_folder()
    {
        Assert.Equal("/Users/a/Applications/Trasmuta.app",
            MacAppUpdate.BundleOf("/Users/a/Applications/Trasmuta.app/Contents/MacOS/"));
        Assert.Null(MacAppUpdate.BundleOf("/Users/a/src/Filee.App/bin/Release/net10.0/"));
        Assert.Null(MacAppUpdate.BundleOf("/Users/a/Thing/Contents/MacOS/"));
    }

    [Fact]
    public void A_release_offers_this_Macs_zip()
    {
        using var release = JsonDocument.Parse("""
            {"assets": [
              {"name": "Trasmuta-1.1.0-osx-arm64.zip", "size": 10, "browser_download_url": "https://example.test/arm.zip"},
              {"name": "Trasmuta-1.1.0-osx-x64.zip", "size": 11, "browser_download_url": "https://example.test/intel.zip"},
              {"name": "Trasmuta-1.1.0-SHA256SUMS.txt", "size": 1, "browser_download_url": "https://example.test/sums.txt"}
            ]}
            """);
        var (arm, sums) = UpdateService.FindFiles(release.RootElement, "-osx-arm64.zip");
        Assert.Equal("Trasmuta-1.1.0-osx-arm64.zip", arm!.Name);
        Assert.Equal("Trasmuta-1.1.0-SHA256SUMS.txt", sums!.Name);
        Assert.Equal("Trasmuta-1.1.0-osx-x64.zip", UpdateService.FindFiles(release.RootElement, "-osx-x64.zip").Installer!.Name);
        // A Windows build of the same code still looks for its Setup, which this release doesn't have.
        Assert.Null(UpdateService.FindFiles(release.RootElement).Installer);
    }

    [Fact]
    public async Task The_script_swaps_the_apps_once_the_old_one_has_quit()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            return;
        var folder = Directory.CreateTempSubdirectory("trasmuta-swap-").FullName;
        try
        {
            var app = Fake(folder, "Applications/Trasmuta.app", "old");
            var incoming = Fake(folder, "update/stage/Trasmuta.app", "new");
            var opened = Path.Combine(folder, "opened.txt");
            using var running = Process.Start("/bin/sleep", "1")!; // stands in for the app that is quitting

            var exit = await RunScriptAsync(folder, running.Id, incoming, app, opened);

            Assert.Equal(0, exit);
            Assert.True(running.HasExited);
            Assert.Equal("new", File.ReadAllText(Path.Combine(app, "version.txt")));
            Assert.False(Directory.Exists(app + ".previous"));
            Assert.False(Directory.Exists(app + ".incoming"));
            Assert.Equal(app, File.ReadAllText(opened).Trim());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Without_a_new_app_the_old_one_stays_and_opens_again()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            return;
        var folder = Directory.CreateTempSubdirectory("trasmuta-swap-").FullName;
        try
        {
            var app = Fake(folder, "Applications/Trasmuta.app", "old");
            var opened = Path.Combine(folder, "opened.txt");

            var exit = await RunScriptAsync(folder, pid: 999999, Path.Combine(folder, "missing/Trasmuta.app"), app, opened);

            Assert.NotEqual(0, exit);
            Assert.Equal("old", File.ReadAllText(Path.Combine(app, "version.txt")));
            Assert.Equal(app, File.ReadAllText(opened).Trim());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static string Fake(string root, string relative, string version)
    {
        var app = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.Combine(app, "Contents", "MacOS"));
        File.WriteAllText(Path.Combine(app, "version.txt"), version);
        return app;
    }

    /// <summary>Runs the swap script with "open" replaced by a command that writes down what it would open.</summary>
    private static async Task<int> RunScriptAsync(string folder, int pid, string newApp, string app, string opened)
    {
        var script = Path.Combine(folder, "swap.sh");
        File.WriteAllText(script, MacAppUpdate.SwapScript);
        var recorder = Path.Combine(folder, "record-open.sh");
        File.WriteAllText(recorder, $"#!/bin/sh\necho \"$1\" > '{opened}'\n");
        File.SetUnixFileMode(recorder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
        foreach (var argument in new[] { script, pid.ToString(System.Globalization.CultureInfo.InvariantCulture), newApp, app })
            start.ArgumentList.Add(argument);
        start.Environment["TRASMUTA_OPEN"] = recorder;
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
        return process.ExitCode;
    }
}
