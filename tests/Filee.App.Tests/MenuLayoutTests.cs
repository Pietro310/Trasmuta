// Trasmuta's menu styles (DonutMenu.Layouts.cs): every style renders, answers hit tests with the donut's kinds and
// puts its header (the hub) where the controller places the cursor. Screenshots land in bin/.../screenshots.

using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Filee.App.Controls;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.App.Views;
using Filee.Core.Settings;

namespace Filee.App.Tests;

public class MenuLayoutTests
{
    private static readonly string[] Images = [@"C:\photos\a.jpg", @"C:\photos\b.jpg", @"C:\photos\c.jpg"];

    private static RadialWindow Open(MenuLayout layout, out RadialViewModel vm)
    {
        TestServices.EnsureInitialized("en");
        AppHost.Get<UserDataStore>().Settings.Donut.Layout = layout;
        vm = AppHost.Get<RadialViewModel>();
        vm.Load(Images, clickMode: true);
        vm.FitToThisDonut();
        var window = new RadialWindow { DataContext = vm };
        window.Show();
        RenderTests.Pump();
        return window;
    }

    [AvaloniaTheory]
    [InlineData(MenuLayout.Ring)]
    [InlineData(MenuLayout.Fan)]
    [InlineData(MenuLayout.List)]
    [InlineData(MenuLayout.Grid)]
    public void Every_style_renders_with_a_highlight(MenuLayout layout)
    {
        var window = Open(layout, out var vm);
        try
        {
            Assert.Equal(layout, window.DonutControl.Layout);
            window.DonutControl.SetExternalHighlight(1);
            RenderTests.Pump();
            RenderTests.Save(window, $"menu-{layout.ToString().ToLowerInvariant()}.png");
            Assert.True(vm.Items.Count > 3);
        }
        finally
        {
            window.Close();
            AppHost.Get<UserDataStore>().Settings.Donut.Layout = MenuLayout.Ring;
        }
    }

    [AvaloniaTheory]
    [InlineData(MenuLayout.List)]
    [InlineData(MenuLayout.Grid)]
    public void Card_styles_hit_the_header_and_each_card(MenuLayout layout)
    {
        var window = Open(layout, out var vm);
        try
        {
            var donut = window.DonutControl;
            var (_, anchor) = DonutMenu.Footprint(layout, donut.OuterRadius, vm.ReserveSubRing, vm.Items.Count, 6);
            Assert.Equal(DonutHitKind.Center, donut.HitTest(anchor).Kind);
            for (var i = 0; i < vm.Items.Count; i++)
            {
                var hit = donut.HitTest(CardCenter(layout, i));
                Assert.Equal(DonutHitKind.Segment, hit.Kind);
                Assert.Equal(i, hit.Index);
            }
            Assert.Equal(DonutHitKind.Outside, donut.HitTest(new Point(donut.Bounds.Width - 2, donut.Bounds.Height - 2)).Kind);
        }
        finally
        {
            window.Close();
            AppHost.Get<UserDataStore>().Settings.Donut.Layout = MenuLayout.Ring;
        }
    }

    [AvaloniaFact]
    public void Fan_slices_stay_in_the_upper_half()
    {
        var window = Open(MenuLayout.Fan, out _);
        try
        {
            var donut = window.DonutControl;
            var center = new Point(donut.Bounds.Width / 2, donut.Bounds.Height / 2);
            var middle = (donut.OuterRadius + donut.OuterRadius * donut.HoleRatio) / 2;
            Assert.Equal(DonutHitKind.Segment, donut.HitTest(center + new Point(0, -middle)).Kind);
            Assert.Equal(DonutHitKind.Outside, donut.HitTest(center + new Point(0, middle)).Kind);
            // The first slice starts at the fan's left edge.
            var start = -donut.Sweep / 2 + 0.05;
            Assert.Equal(0, donut.HitTest(center + new Point(middle * Math.Sin(start), -middle * Math.Cos(start))).Index);
        }
        finally
        {
            window.Close();
            AppHost.Get<UserDataStore>().Settings.Donut.Layout = MenuLayout.Ring;
        }
    }

    [AvaloniaFact]
    public void Digits_pick_a_preset()
    {
        var window = Open(MenuLayout.List, out var vm);
        try
        {
            var invoked = -1;
            window.DonutControl.SegmentInvoked += (_, index) => invoked = index;
            var first = vm.Items.Select((item, i) => (item, i)).First(x => x.item is { IsEnabled: true, IsGroup: false }).i;
            Assert.True(window.DonutControl.HandleKey(Key.D1 + first));
            Assert.Equal(first, invoked);
        }
        finally
        {
            window.Close();
            AppHost.Get<UserDataStore>().Settings.Donut.Layout = MenuLayout.Ring;
        }
    }

    private static Point CardCenter(MenuLayout layout, int index)
    {
        // Mirrors DonutMenu.ItemRect: 16 padding, header 96, two 4-pixel gaps, rows 40 + 4 or tiles 104 × 68 + 4.
        const double top = 16 + 96 + 8;
        return layout == MenuLayout.Grid
            ? new Point(16 + index % 3 * 108 + 52, top + index / 3 * 72 + 34)
            : new Point(16 + 118, top + index * 44 + 20);
    }
}
