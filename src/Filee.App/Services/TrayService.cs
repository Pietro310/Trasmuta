// System tray icon: open settings, pause gestures, install a new version, quit. While a new version is out the icon
// carries a red dot (and on macOS so does the Dock icon), so it is seen without opening anything.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Filee.Core.Localization;
using Filee.Core.Platform;
using Filee.Core.Settings;

namespace Filee.App.Services;

public sealed class TrayService(UserDataStore store, ILocalizer loc, IPlatformServices platform, WindowService windows,
    UpdateService updates)
{
    private TrayIcon? _tray;
    private NativeMenuItem? _open, _pause, _quit, _update;
    private byte[] _iconFile = [];
    private WindowIcon? _icon, _dotted;
    private bool? _dotShown;

    public void Create(Application app)
    {
        using (var file = AssetLoader.Open(new Uri("avares://Filee/Assets/Icons/filee.ico")))
        using (var copy = new MemoryStream())
        {
            file.CopyTo(copy);
            _iconFile = copy.ToArray();
        }
        _icon = new WindowIcon(new MemoryStream(_iconFile));
        _open = new NativeMenuItem();
        _open.Click += (_, _) => windows.ShowMain();
        _pause = new NativeMenuItem();
        _pause.Click += (_, _) =>
        {
            store.Settings.Paused = !store.Settings.Paused;
            store.SaveSettings();
        };
        _quit = new NativeMenuItem();
        _quit.Click += (_, _) => (app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        _update = new NativeMenuItem();
        _update.Click += async (_, _) => await updates.UpdateAsync();

        _tray = new TrayIcon
        {
            Icon = _icon,
            // Set before the icon is attached: on macOS Avalonia passes the tooltip to native code, which can't take null.
            ToolTipText = Filee.Core.AppIdentity.Name,
            Menu = new NativeMenu { Items = { _open, _pause, new NativeMenuItemSeparator(), _quit } },
            IsVisible = platform.SupportsDockVisibility ? store.Settings.ShowMenuBarIcon : true,
        };
        _tray.Clicked += (_, _) => windows.ShowMain();
        TrayIcon.SetIcons(app, [_tray]);

        Refresh();
        loc.LanguageChanged += (_, _) => Refresh();
        store.SettingsChanged += (_, _) => Refresh();
        updates.PropertyChanged += (_, e) =>
        {
            if (UpdateTexts.Affects(e.PropertyName))
                Refresh();
        };
    }

    private void Refresh()
    {
        if (_tray?.Menu is not { } menu)
            return;
        _tray.IsVisible = platform.SupportsDockVisibility ? store.Settings.ShowMenuBarIcon : true;
        ShowDot(updates.IsUpdateAvailable);
        // "Update to x.y.z" sits at the top of the menu while a newer release is out ("Downloading… 42%" meanwhile).
        if (updates.LatestVersion is { } latest)
        {
            _update!.Header = updates.Step is UpdateStep.Downloading or UpdateStep.Installing
                ? UpdateTexts.Short(updates, loc)
                : loc.Format("tray.update", latest);
            if (!menu.Items.Contains(_update))
                menu.Items.Insert(0, _update);
        }
        else
        {
            menu.Items.Remove(_update!);
        }
        _open!.Header = loc["tray.open"];
        _pause!.Header = store.Settings.Paused ? loc["tray.resume"] : loc["tray.pause"];
        _quit!.Header = loc["tray.quit"];
        var drag = store.Settings.Triggers.FirstOrDefault(t => t.Enabled && t.Kind == TriggerKind.Drag);
        _tray.ToolTipText = store.Settings.Paused
            ? loc["triggers.paused"]
            : loc.Format("tray.tooltip", drag is null ? "—" : GestureText.Modifiers(drag.Modifiers));
    }

    /// <summary>The red dot on the tray icon, and on macOS the Dock badge, while a newer version is out.</summary>
    private void ShowDot(bool show)
    {
        if (_dotShown == show || _tray is null)
            return;
        _dotShown = show;
        _tray.Icon = show ? _dotted ??= WithDot(_iconFile) ?? _icon : _icon;
        if (OperatingSystem.IsMacOS() && MacAppUpdate.RunningBundle is not null)
            Filee.Platform.MacOS.MacOSBundle.SetDockBadge(show ? "1" : null);
    }

    /// <summary>The icon with a red dot in its top-right corner; null when it can't be drawn (the plain icon stays).</summary>
    private static WindowIcon? WithDot(byte[] iconFile)
    {
        const int size = 64;
        try
        {
            using var source = new Bitmap(new MemoryStream(iconFile));
            var target = new RenderTargetBitmap(new PixelSize(size, size));
            using (var context = target.CreateDrawingContext())
            {
                context.DrawImage(source, new Rect(0, 0, size, size));
                const double radius = size * 0.2;
                context.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D)), new Pen(Brushes.White, size * 0.05),
                    new Point(size - radius - 2, radius + 2), radius, radius);
            }
            return new WindowIcon(target);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null; // e.g. no renderer: the update still shows in the tray menu
        }
    }

    public void Dispose() => _tray?.Dispose();
}
