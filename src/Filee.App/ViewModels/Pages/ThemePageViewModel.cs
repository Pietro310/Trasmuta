// Theme: light/dark, accent colour, softness (corner radius), the donut's opacity, reduce animations. Changes apply live.

using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Filee.App.Controls;
using Filee.App.Services;
using Filee.Core.Localization;
using Filee.Core.Platform;
using Filee.Core.Settings;

namespace Filee.App.ViewModels.Pages;

public sealed partial class ThemePageViewModel : ObservableObject
{
    private readonly UserDataStore _store;
    private readonly bool _loading;

    public ThemePageViewModel(UserDataStore store, ILocalizer loc, IPlatformServices platform)
    {
        _store = store;
        _loading = true;
        var theme = store.Settings.Theme;
        Modes = Enum.GetValues<ThemeMode>().Select(m => new Choice<ThemeMode>(m, loc[$"theme.mode.{m}"])).ToList();
        Mode = Modes.First(m => m.Value == theme.Mode);
        Accent = Color.TryParse(theme.Accent, out var c) ? c : Colors.MediumPurple;
        AccentHex = $"#{Accent.R:X2}{Accent.G:X2}{Accent.B:X2}";
        CornerRadius = theme.CornerRadius;
        DonutOpacity = Math.Round(Math.Clamp(store.Settings.Donut.Opacity, DonutSettings.MinOpacity, 1) * 100);
        ReduceMotion = theme.ReduceMotion ?? platform.PrefersReducedMotion;
        Layouts =
        [
            new(MenuLayout.Ring, loc["theme.layout.Ring"], StreamGeometry.Parse("F0 M12,2 A10,10 0 1 1 11.99,2 Z M12,7 A5,5 0 1 1 11.99,7 Z")),
            new(MenuLayout.Fan, loc["theme.layout.Fan"], StreamGeometry.Parse("F0 M2,18 A10,10 0 0 1 22,18 L17,18 A5,5 0 0 0 7,18 Z")),
            new(MenuLayout.List, loc["theme.layout.List"], StreamGeometry.Parse("M4,3 H20 V7.5 H4 Z M4,9.75 H20 V14.25 H4 Z M4,16.5 H20 V21 H4 Z")),
            new(MenuLayout.Grid, loc["theme.layout.Grid"], StreamGeometry.Parse("M3,3 H10.5 V10.5 H3 Z M13.5,3 H21 V10.5 H13.5 Z M3,13.5 H10.5 V21 H3 Z M13.5,13.5 H21 V21 H13.5 Z")),
        ];
        Layout = Layouts.FirstOrDefault(l => l.Value == store.Settings.Donut.Layout) ?? Layouts[0];
        PreviewItems =
        [
            new DonutItem { Id = "a", Label = "PNG" },
            new DonutItem { Id = "b", Label = "PDF" },
            new DonutItem { Id = "c", Label = "WEBP" },
            new DonutItem { Id = "d", Label = "HWPX" },
            new DonutItem { Id = "e", Label = "JPG 70%", Caption = "JPG" },
            new DonutItem { Id = "f", Label = "TIFF" },
        ];
        _loading = false;
    }

    public IReadOnlyList<Choice<ThemeMode>> Modes { get; }
    public IReadOnlyList<Color> Swatches { get; } = ThemeService.Swatches.Select(Color.Parse).ToList();
    public IReadOnlyList<DonutItem> PreviewItems { get; }

    /// <summary>The menu styles, as tiles with a small picture of each.</summary>
    public IReadOnlyList<LayoutChoice> Layouts { get; }

    /// <summary>The menu style: saved at once, followed by the preview and by the menu the next time it opens.</summary>
    [ObservableProperty] private LayoutChoice _layout = null!;

    partial void OnLayoutChanged(LayoutChoice value)
    {
        if (_loading || value is null)
            return;
        _store.Settings.Donut.Layout = value.Value;
        _store.SaveSettings();
    }

    [ObservableProperty] private Choice<ThemeMode> _mode = null!;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(AccentBrush), nameof(PreviewBackdrop))] private Color _accent;
    [ObservableProperty] private string _accentHex = "";

    public IBrush AccentBrush => new SolidColorBrush(Accent);

    /// <summary>
    /// What the preview donut floats on, standing in for the desktop: a gradient of the accent, so the slices'
    /// opacity shows (on the plain card, slices of its own colour look the same at any opacity).
    /// </summary>
    public IBrush PreviewBackdrop => new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(ThemeService.Shade(Accent, 0.55), 0),
            new GradientStop(Accent, 0.5),
            new GradientStop(ThemeService.Shade(Accent, -0.45), 1),
        },
    };
    [ObservableProperty] private double _cornerRadius;
    [ObservableProperty] private bool _reduceMotion;

    /// <summary>
    /// The donut toolbar's opacity in percent: the one setting Donut toolbar → Appearance → Opacity edits too (each
    /// page reads it when it opens). The preview donut follows it at once, the toolbar the next time it opens.
    /// </summary>
    [ObservableProperty] private double _donutOpacity;

    partial void OnModeChanged(Choice<ThemeMode>? oldValue, Choice<ThemeMode> newValue)
    {
        if (!Choices.Restore(oldValue, newValue, v => Mode = v))
            Save(t => t.Mode = newValue.Value);
    }
    partial void OnAccentChanged(Color value)
    {
        var hex = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
        if (!string.Equals(AccentHex, hex, StringComparison.OrdinalIgnoreCase))
            AccentHex = hex;
        Save(t => t.Accent = hex);
    }

    // Custom colour typed as #RRGGBB; ignored until it parses.
    partial void OnAccentHexChanged(string value)
    {
        if (Color.TryParse(value.Trim(), out var color) && color != Accent)
            Accent = Color.FromRgb(color.R, color.G, color.B);
    }
    partial void OnCornerRadiusChanged(double value) => Save(t => t.CornerRadius = Math.Round(value));
    partial void OnReduceMotionChanged(bool value) => Save(t => t.ReduceMotion = value);

    partial void OnDonutOpacityChanged(double value)
    {
        if (_loading)
            return;
        _store.Settings.Donut.Opacity = Math.Round(value) / 100;
        _store.SaveSettings();
    }

    private void Save(Action<ThemeSettings> change)
    {
        if (_loading)
            return;
        change(_store.Settings.Theme);
        _store.SaveSettings(); // App re-applies the theme on SettingsChanged
    }
}

/// <summary>A menu style on the Theme page: its name and a small picture of it.</summary>
public sealed record LayoutChoice(MenuLayout Value, string Name, Geometry Icon);
