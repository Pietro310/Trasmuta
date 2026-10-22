// Moving Trasmuta.app to Applications (MacAppMove), why a Mac copy can't update itself (MacAppUpdate.State), and how
// often the update check runs (UpdateService).

using System.Diagnostics;
using Filee.App.Services;

namespace Filee.App.Tests;

public class MacAppMoveTests
{
    private const string Home = "/Users/a";

    [Theory]
    [InlineData("/Applications/Trasmuta.app", true)]
    [InlineData("/Applications/Tools/Trasmuta.app", true)]
    [InlineData("/Users/a/Applications/Trasmuta.app", true)]
    [InlineData("/Users/a/Downloads/Trasmuta.app", false)]
    [InlineData("/Volumes/Trasmuta/Trasmuta.app", false)]
    [InlineData("/private/var/folders/x/T/AppTranslocation/1234/d/Trasmuta.app", false)]
    [InlineData("/Users/a/ApplicationsOld/Trasmuta.app", false)]
    public void Only_the_Applications_folders_count_as_installed(string bundle, bool installed) =>
        Assert.Equal(installed, MacAppUpdate.IsInApplications(bundle, Home));

    [Fact]
    public void A_copy_that_cant_update_says_why()
    {
        Assert.Equal(MacSelfUpdate.NotAnApp, MacAppUpdate.StateOf(null, canReplace: false, Home));
        Assert.Equal(MacSelfUpdate.Ready, MacAppUpdate.StateOf("/Users/a/Downloads/Trasmuta.app", canReplace: true, Home));
        // Opened from Downloads, macOS runs it from a read-only copy: moving it to Applications fixes that.
        Assert.Equal(MacSelfUpdate.NeedsMove,
            MacAppUpdate.StateOf("/private/var/folders/x/T/AppTranslocation/1234/d/Trasmuta.app", canReplace: false, Home));
        Assert.Equal(MacSelfUpdate.NeedsMove, MacAppUpdate.StateOf("/Volumes/Trasmuta/Trasmuta.app", canReplace: false, Home));
        // In /Applications on an account that may not change it: only an administrator can.
        Assert.Equal(MacSelfUpdate.NoPermission, MacAppUpdate.StateOf("/Applications/Trasmuta.app", canReplace: false, Home));
    }

    [Fact]
    public void Administrators_get_the_shared_Applications_folder_others_their_own()
    {
        Assert.Equal("/Applications", MacAppMove.TargetFolder(Home, _ => true));
        Assert.Equal("/Users/a/Applications", MacAppMove.TargetFolder(Home, _ => false));
    }

    [Fact]
    public async Task The_copy_replaces_an_older_app_in_Applications()
    {
        var folder = Directory.CreateTempSubdirectory("trasmuta-move-").FullName;
        try
        {
            var running = Fake(folder, "Downloads/Trasmuta.app", "new");
            var target = Fake(folder, "Applications/Trasmuta.app", "old");

            await MacAppMove.InstallCopyAsync(running, target, CopyAsync);

            Assert.Equal("new", File.ReadAllText(Path.Combine(target, "version.txt")));
            Assert.True(Directory.Exists(running)); // the caller sends it to the Trash
            Assert.False(Directory.Exists(target + ".incoming"));
            Assert.False(Directory.Exists(target + ".previous"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task A_failed_copy_leaves_the_app_in_Applications_as_it_was()
    {
        var folder = Directory.CreateTempSubdirectory("trasmuta-move-").FullName;
        try
        {
            var running = Fake(folder, "Downloads/Trasmuta.app", "new");
            var target = Fake(folder, "Applications/Trasmuta.app", "old");

            await Assert.ThrowsAsync<IOException>(() => MacAppMove.InstallCopyAsync(running, target, async (from, to) =>
            {
                await CopyAsync(from, to);
                throw new IOException("disk full");
            }));

            Assert.Equal("old", File.ReadAllText(Path.Combine(target, "version.txt")));
            Assert.False(Directory.Exists(target + ".incoming"));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task The_moved_app_opens_once_the_old_one_has_quit_and_is_told_to_update()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            return;
        var folder = Directory.CreateTempSubdirectory("trasmuta-move-").FullName;
        try
        {
            var script = Path.Combine(folder, "relaunch.sh");
            File.WriteAllText(script, MacAppMove.RelaunchScript);
            var opened = Path.Combine(folder, "opened.txt");
            var recorder = Path.Combine(folder, "record-open.sh");
            File.WriteAllText(recorder, $"#!/bin/sh\necho \"$@\" > '{opened}'\n");
            File.SetUnixFileMode(recorder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            using var quitting = Process.Start("/bin/sleep", "1")!;
            var start = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
            foreach (var argument in new[] { script, quitting.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                         "/Applications/Trasmuta.app", "--update" })
                start.ArgumentList.Add(argument);
            start.Environment["TRASMUTA_OPEN"] = recorder;

            using var process = Process.Start(start)!;
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30),
                TestContext.Current.CancellationToken);

            Assert.Equal(0, process.ExitCode);
            Assert.True(quitting.HasExited);
            Assert.Equal("/Applications/Trasmuta.app --args --update", File.ReadAllText(opened).Trim());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void The_moved_copy_is_started_with_update()
    {
        Assert.True(CommandLine.Parse(["--update"]).Update);
        Assert.False(CommandLine.Parse([]).Update);
    }

    [Fact]
    public void A_long_gap_between_two_looks_at_the_schedule_means_the_computer_slept()
    {
        var now = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(UpdateService.IsWake(now, default)); // the first look
        Assert.False(UpdateService.IsWake(now, now - TimeSpan.FromMinutes(1)));
        Assert.True(UpdateService.IsWake(now, now - TimeSpan.FromHours(8)));
        Assert.Equal(TimeSpan.FromHours(1), UpdateService.CheckInterval);
    }

    private static string Fake(string root, string relative, string version)
    {
        var app = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.Combine(app, "Contents", "MacOS"));
        File.WriteAllText(Path.Combine(app, "version.txt"), version);
        return app;
    }

    /// <summary>Stands in for ditto.</summary>
    private static Task CopyAsync(string from, string to)
    {
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var copy = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy);
        }
        Directory.CreateDirectory(Path.Combine(to, "Contents", "MacOS"));
        return Task.CompletedTask;
    }
}
