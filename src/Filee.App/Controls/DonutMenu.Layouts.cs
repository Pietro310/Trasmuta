// Trasmuta: the menu styles besides the donut (Settings → Theme → Menu style, DonutSettings.Layout).
//
//   Ring        → the donut as Filee draws it.
//   Fan         → the same slices sharing the upper half turn (FanSweep). Every polar computation goes through
//                 Sweep, SliceMid and SliceSpan, so groups, the keyboard, flips and the progress ring work unchanged.
//   List, Grid  → "cards": a header under the cursor (the hub: title, hints, cancel; the progress ring after a drop)
//                 with the presets below it as rows or tiles. A group opens its presets in a column at its right.
//                 Hit testing answers with the donut's DonutHit kinds, so the window, the controller and drag and drop
//                 work the same in every style.
//
// The settings editor (IsEditMode) always shows the donut: the order it edits is the same in every style. Digits 1-9
// pick a preset (or a preset of the open group) in every style; the card styles show the numbers.

using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Filee.Core.Settings;

namespace Filee.App.Controls;

public sealed partial class DonutMenu
{
    public static readonly StyledProperty<MenuLayout> LayoutProperty =
        AvaloniaProperty.Register<DonutMenu, MenuLayout>(nameof(Layout));

    /// <summary>How the presets are laid out (the editor ignores it and shows the donut).</summary>
    public MenuLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>
    /// Angle the fan's slices share: the upper half turn, opening wider (up to <see cref="MaxFanSweep"/>) when there are
    /// so many presets that a slice would get narrower than <see cref="MinFanSlice"/> and its name wouldn't fit.
    /// </summary>
    public static double FanSweep(int count) => Math.Clamp(count * MinFanSlice, Math.PI, MaxFanSweep);

    private const double MinFanSlice = 0.44;
    private const double MaxFanSweep = 1.45 * Math.PI;

    /// <summary>Angle the slices share: a whole turn, or the fan.</summary>
    internal double Sweep => !IsEditMode && Layout == MenuLayout.Fan ? FanSweep(Count) : DonutGeometry.FullTurn;

    private double SliceMid(int index, int count) => DonutGeometry.MidAngle(index, count, Sweep);
    private double SliceSpan(int count) => DonutGeometry.Span(count, Sweep);

    /// <summary>Rows or tiles instead of slices.</summary>
    internal bool UsesCards => !IsEditMode && Layout is MenuLayout.List or MenuLayout.Grid;

    // ───────────────────────── Card geometry ─────────────────────────

    /// <summary>Room around the cards for their sheet and its shadow.</summary>
    private const double CardPad = 16;

    /// <summary>Tall enough for the progress ring a drop turns the header into.</summary>
    private const double HeaderHeight = 2 * (RingRadius + RingStroke + 2) + 8;

    private const double RowWidth = 236;
    private const double RowHeight = 40;
    private const double CardGap = 4;
    private const double TileWidth = 104;
    private const double TileHeight = 68;
    private const int GridColumns = 3;

    /// <summary>The column a group opens: its gap to the group's card and its width.</summary>
    private const double ColumnGap = 10;
    private const double ColumnWidth = 196;

    /// <summary>How far the sheet behind the cards reaches past them.</summary>
    private const double SheetMargin = 6;

    private static double ContentWidth(MenuLayout layout) =>
        layout == MenuLayout.Grid ? GridColumns * TileWidth + (GridColumns - 1) * CardGap : RowWidth;

    private const double ItemsTop = CardPad + HeaderHeight + 2 * CardGap;

    private static Rect ItemRect(MenuLayout layout, int index) => layout == MenuLayout.Grid
        ? new Rect(CardPad + index % GridColumns * (TileWidth + CardGap), ItemsTop + index / GridColumns * (TileHeight + CardGap),
            TileWidth, TileHeight)
        : new Rect(CardPad, ItemsTop + index * (RowHeight + CardGap), RowWidth, RowHeight);

    /// <summary>Bottom of the last card, or of the header without any.</summary>
    private static double ItemsBottom(MenuLayout layout, int count) =>
        count <= 0 ? CardPad + HeaderHeight : ItemRect(layout, count - 1).Bottom;

    private static double ColumnHeight(int count) => count <= 0 ? 0 : count * RowHeight + (count - 1) * CardGap;

    /// <summary>The middle of the header: where the cursor is, and where the progress ring turns.</summary>
    private static Point CardAnchor(MenuLayout layout) => new(CardPad + ContentWidth(layout) / 2, CardPad + HeaderHeight / 2);

    /// <summary>
    /// Size of the menu for <paramref name="items"/> presets, and the point that goes under the cursor. With
    /// <paramref name="groups"/> it keeps room for a group's ring (or column) of up to <paramref name="maxChildren"/>.
    /// The donut and the fan keep the square of <see cref="SideFor"/>, centred on the cursor.
    /// </summary>
    public static (Size Size, Point Anchor) Footprint(MenuLayout layout, double outerRadius, bool groups, int items, int maxChildren)
    {
        if (layout is not (MenuLayout.List or MenuLayout.Grid))
        {
            var side = SideFor(outerRadius, groups);
            return (new Size(side, side), new Point(side / 2, side / 2));
        }
        var width = CardPad + ContentWidth(layout) + (groups ? ColumnGap + ColumnWidth : 0) + CardPad;
        var height = Math.Max(ItemsBottom(layout, items), groups ? CardPad + ColumnHeight(maxChildren) : 0) + CardPad;
        return (new Size(width, height), CardAnchor(layout));
    }

    private Size CardsMeasure() => Footprint(Layout, OuterRadius, ReserveSubRing || Items?.Any(i => i.IsGroup) == true,
        Count, Items?.Max(i => i.Children?.Count ?? 0) ?? 0).Size;

    private Rect HeaderRect => new(CardPad, CardPad, ContentWidth(Layout), HeaderHeight);

    private Rect CardRect(int index) => ItemRect(Layout, index);

    /// <summary>
    /// Slice <paramref name="sub"/> of the column group <paramref name="group"/> opens at its right: level with the
    /// group, moved up as far as it must to fit. In the grid it lies over the tiles to the right of the group.
    /// </summary>
    private Rect ChildRect(int group, int sub)
    {
        var count = Items![group].Children!.Count;
        var parent = CardRect(group);
        var top = Math.Max(CardPad, Math.Min(parent.Top, Bounds.Height - CardPad - ColumnHeight(count)));
        return new Rect(parent.Right + ColumnGap, top + sub * (RowHeight + CardGap), ColumnWidth, RowHeight);
    }

    private Rect ColumnRect(int group) =>
        ChildRect(group, 0).Union(ChildRect(group, Items![group].Children!.Count - 1));

    /// <summary>The gap between a group's card and its column, which the pointer crosses on its way there.</summary>
    private Rect Corridor(int group)
    {
        var parent = CardRect(group);
        var column = ColumnRect(group);
        var top = Math.Min(parent.Top, column.Top);
        return new Rect(parent.Right, top, column.Left - parent.Right, Math.Max(parent.Bottom, column.Bottom) - top);
    }

    private DonutHit HitTestCards(Point p)
    {
        if (ExpandedChildren is { } children)
        {
            for (var k = 0; k < children.Count; k++)
                if (ChildRect(_expanded, k).Inflate(CardGap / 2).Contains(p))
                    return new DonutHit(DonutHitKind.SubSegment, _expanded, k);
        }
        if (HeaderRect.Contains(p))
            return new DonutHit(DonutHitKind.Center);
        for (var i = 0; i < Count; i++)
            if (CardRect(i).Inflate(CardGap / 2).Contains(p))
                return new DonutHit(DonutHitKind.Segment, i);
        if (ExpandedChildren is not null && Corridor(_expanded).Contains(p))
            return new DonutHit(DonutHitKind.Segment, _expanded);
        return new DonutHit(DonutHitKind.Outside);
    }

    /// <summary>
    /// The card styles' <see cref="TrackGroups"/>: resting on a group (<see cref="ExpandDelay"/>) or moving right on it
    /// opens its column; the column stays open over the group, the gap and the column itself, and folds anywhere else.
    /// </summary>
    private void TrackGroupsCards(DonutHit hit, Point? at)
    {
        var before = (_expanded, _pendingGroup);
        var sub = -1;
        switch (hit.Kind)
        {
            case DonutHitKind.Segment when hit.Index == _expanded:
                _ringFromKeys = false;
                ClearPending();
                break;
            case DonutHitKind.Segment when hit.Index >= 0 && hit.Index < Count && Items![hit.Index].CanExpand:
                _expanded = -1;
                var x = at?.X ?? 0;
                if (_pendingGroup != hit.Index)
                {
                    ClearPending();
                    _pendingGroup = hit.Index;
                    _pendingDistance = x;
                    _expandTimer = DispatcherTimer.RunOnce(ExpandPendingGroup, ExpandDelay);
                }
                else if (at is not null && x - _pendingDistance >= OutwardPixels)
                {
                    ExpandPendingGroup(); // heading right, for the column
                }
                else if (at is not null)
                {
                    _pendingDistance = Math.Min(_pendingDistance, x);
                }
                break;
            case DonutHitKind.SubSegment:
                _ringFromKeys = false;
                sub = hit.Sub;
                break;
            case DonutHitKind.Center when _ringFromKeys:
                sub = _subHover;
                break;
            default:
                _expanded = -1;
                _ringFromKeys = false;
                ClearPending();
                break;
        }
        SetSub(sub);
        if ((_expanded, _pendingGroup) != before)
            StartAnimation();
    }

    // ───────────────────────── Keyboard ─────────────────────────

    /// <summary>0 for the 1 key (top row or keypad) up to 8 for the 9 key; null for any other key.</summary>
    private static int? DigitOf(Key key) => key switch
    {
        >= Key.D1 and <= Key.D9 => key - Key.D1,
        >= Key.NumPad1 and <= Key.NumPad9 => key - Key.NumPad1,
        _ => null,
    };

    /// <summary>
    /// Digit <paramref name="digit"/> (0-based): converts with that preset, or with that preset of the open group;
    /// on a group it opens the group. Returns true when the key was used.
    /// </summary>
    private bool PickByDigit(int digit)
    {
        if (ExpandedChildren is { } children)
        {
            if (digit < children.Count)
            {
                SetSub(digit);
                if (children[digit].IsEnabled)
                    SubSegmentInvoked?.Invoke(this, (_expanded, digit));
            }
            return true;
        }
        if (digit >= Count)
            return true;
        SetHover(digit);
        if (Items![digit].CanExpand)
        {
            Expand(digit);
            SetSub(0);
            _ringFromKeys = true;
        }
        else if (Items[digit].IsEnabled)
        {
            SegmentInvoked?.Invoke(this, digit);
        }
        return true;
    }

    /// <summary>The next card from <paramref name="current"/> for an arrow key, or null when the style leaves it to the donut's rule.</summary>
    private int? CardStep(Key key, int current)
    {
        if (!UsesCards || Count == 0 || Layout != MenuLayout.Grid || key is not (Key.Up or Key.Down))
            return null;
        if (current < 0)
            return key == Key.Down ? 0 : Count - 1;
        var next = current + (key == Key.Down ? GridColumns : -GridColumns);
        return next >= 0 && next < Count ? next : current;
    }

    // ───────────────────────── Rendering ─────────────────────────

    private void RenderCards(DrawingContext context, Typeface bold, Typeface regular)
    {
        var n = Count;
        // Nearly transparent everywhere: the pointer and drags never slip through a seam between cards.
        context.DrawRectangle(HitBackdrop, null, new Rect(Bounds.Size));
        var opening = _open.Length == 0 ? 1 : Math.Clamp(_open.Average(), 0, 1);
        var flip = FlipTransform(Center);

        using (context.PushTransform(flip))
        {
            var sheet = new Rect(CardPad, CardPad, ContentWidth(Layout), ItemsBottom(Layout, n) - CardPad).Inflate(SheetMargin);
            using (context.PushOpacity(opening))
                DrawSheet(context, sheet);
            DrawCardHeader(context, bold, regular);
            if (n > 0)
            {
                var y = HeaderRect.Bottom + CardGap;
                context.DrawLine(new Pen(WithOpacity(SecondaryForeground, 0.18), 1), new Point(HeaderRect.Left + 8, y),
                    new Point(HeaderRect.Right - 8, y));
            }

            for (var i = 0; i < n; i++)
            {
                var open = i < _open.Length ? _open[i] : 1;
                if (open <= 0.001)
                    continue;
                var hover = i < _hover.Length ? _hover[i].Value : 0;
                var rect = CardRect(i);
                var c = rect.Center;
                var dx = Layout == MenuLayout.List ? hover * 3 : 0;
                var dy = (1 - open) * -12;
                var scale = Layout == MenuLayout.Grid ? 1 + 0.035 * hover : 1;
                var transform = Matrix.CreateTranslation(-c.X, -c.Y) * Matrix.CreateScale(scale, scale) *
                                Matrix.CreateTranslation(c.X + dx, c.Y + dy);
                using (context.PushTransform(transform))
                using (context.PushOpacity(Math.Clamp(open, 0, 1)))
                    DrawCard(context, Items![i], rect, hover, i == ActiveIndex, i + 1, Layout == MenuLayout.List, bold, regular);
            }
        }

        if (_shownExpanded >= 0 && _shownExpanded < n && _expand.Value > 0.01 && Items![_shownExpanded].Children is { Count: > 0 })
        {
            using (context.PushTransform(flip))
                DrawCardColumn(context, opening, bold, regular);
        }
    }

    /// <summary>The panel behind the cards (or a group's column), with a soft shadow.</summary>
    private void DrawSheet(DrawingContext context, Rect sheet)
    {
        var radius = SliceCornerRadius + SheetMargin;
        // Stacked translucent outlines fake a blurred shadow without blur effects (cheap on any GPU), like the donut's.
        for (var k = 1; k <= 4; k++)
        {
            var spread = k * 2.0;
            var shadow = new Rect(sheet.X - spread / 2, sheet.Y + 2 + spread / 2, sheet.Width + spread, sheet.Height + spread);
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(22 - k * 4), 0, 0, 0)), null, shadow,
                radius + spread / 2, radius + spread / 2);
        }
        context.DrawRectangle(WithOpacity(CenterBrush, Math.Max(SliceOpacity, 0.85)), BorderPen(), sheet, radius, radius);
    }

    private void DrawCardHeader(DrawingContext context, Typeface bold, Typeface regular)
    {
        var header = HeaderRect;
        var radius = SliceCornerRadius;
        if (_externalCenterHot)
            context.DrawRectangle(WithOpacity(DangerBrush, 0.18), null, header, radius, radius);
        var pulse = 1 + _centerPulse.Value * 0.06;
        var center = header.Center;
        var transform = Matrix.CreateTranslation(-center.X, -center.Y) * Matrix.CreateScale(pulse, pulse) *
                        Matrix.CreateTranslation(center.X, center.Y);
        using (context.PushTransform(transform))
            DrawHubText(context, center, header.Width - 24, bold, regular);
    }

    /// <summary>One preset as a row (<paramref name="row"/>) or a tile, numbered <paramref name="number"/> when 1-9.</summary>
    private void DrawCard(DrawingContext context, DonutItem item, Rect rect, double hover, bool active, int number, bool row,
        Typeface bold, Typeface regular)
    {
        var radius = SliceCornerRadius;
        context.DrawRectangle(WithOpacity(SliceBrush, SliceOpacity * (item.IsEnabled ? 1 : 0.55)), BorderPen(), rect, radius, radius);
        if (hover > 0.01 && item.IsEnabled)
            context.DrawRectangle(WithOpacity(AccentBrush, 0.82 * Math.Clamp(hover, 0, 1)), null, rect, radius, radius);

        var hot = active && item.IsEnabled;
        var foreground = hot ? OnAccentBrush : item.IsEnabled ? Foreground : SecondaryForeground;
        var muted = hot ? OnAccentBrush : SecondaryForeground;
        var label = KeepKoreanWords(item.Label);
        var caption = !string.IsNullOrEmpty(item.Caption) &&
                      !string.Equals(item.Caption, item.Label, StringComparison.OrdinalIgnoreCase) ? item.Caption : null;
        var digit = number is >= 1 and <= 9 ? MakeAligned(number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            regular, 11, muted, 14, TextAlignment.Center) : null;

        if (row)
        {
            const double numberWidth = 26, chevronWidth = 16, padding = 10;
            if (digit is not null)
                using (context.PushOpacity(0.75))
                    context.DrawText(digit, new Point(rect.Left + padding - 2, rect.Center.Y - digit.Height / 2));
            var right = rect.Right - padding;
            if (item.IsGroup)
            {
                var chevron = MakeAligned("›", bold, 16, muted, chevronWidth, TextAlignment.Right);
                context.DrawText(chevron, new Point(right - chevronWidth, rect.Center.Y - chevron.Height / 2));
                right -= chevronWidth + 2;
            }
            if (caption is not null)
            {
                var text = MakeAligned(caption, regular, 11, muted, 92, TextAlignment.Right);
                context.DrawText(text, new Point(right - 92, rect.Center.Y - text.Height / 2));
                right -= Math.Min(92, text.WidthIncludingTrailingWhitespace) + 8;
            }
            var left = rect.Left + numberWidth;
            var name = MakeAligned(label, bold, 13, foreground, Math.Max(20, right - left), TextAlignment.Left);
            context.DrawText(name, new Point(left, rect.Center.Y - name.Height / 2));
            return;
        }

        if (digit is not null)
            using (context.PushOpacity(0.75))
                context.DrawText(digit, new Point(rect.Left + 5, rect.Top + 4));
        if (item.IsGroup)
        {
            var chevron = MakeAligned("›", bold, 15, muted, 14, TextAlignment.Right);
            context.DrawText(chevron, new Point(rect.Right - 19, rect.Top + 2));
        }
        var width = rect.Width - 12;
        var title = MakeText(label, bold, LabelSize(label, bold, width), foreground, width, maxLines: 2);
        var sub = caption is null ? null : MakeText(caption, regular, 10.5, muted, width);
        var total = title.Height + (sub?.Height ?? 0);
        var top = rect.Center.Y - total / 2 + 3;
        DrawCentered(context, title, rect.Center.X, top);
        if (sub is not null)
            DrawCentered(context, sub, rect.Center.X, top + title.Height);
    }

    /// <summary>The column an open group shows at its right, growing out of the group's card.</summary>
    private void DrawCardColumn(DrawingContext context, double opening, Typeface bold, Typeface regular)
    {
        var group = _shownExpanded;
        var children = Items![group].Children!;
        var t = Math.Max(0, _expand.Value);
        var parent = CardRect(group);
        var anchor = new Point(parent.Right, parent.Center.Y);
        var scale = 0.85 + 0.15 * t;
        var transform = Matrix.CreateTranslation(-anchor.X, -anchor.Y) * Matrix.CreateScale(scale, scale) *
                        Matrix.CreateTranslation(anchor.X, anchor.Y);
        using (context.PushTransform(transform))
        using (context.PushOpacity(Math.Clamp(t, 0, 1) * opening))
        {
            DrawSheet(context, ColumnRect(group).Inflate(SheetMargin));
            for (var k = 0; k < children.Count; k++)
            {
                var hot = k == _subHover && group == _expanded;
                DrawCard(context, children[k], ChildRect(group, k), hot ? 1 : 0, hot, k + 1, row: true, bold, regular);
            }
        }
    }

    /// <summary>
    /// After a drop: the cards fly into the header while it turns into the progress ring's plate (the card styles'
    /// part of <see cref="RenderProgress"/>).
    /// </summary>
    private void RenderCardsCollapse(DrawingContext context, Point center, double collapseAll, Typeface bold, Typeface regular)
    {
        var n = Count;
        using (context.PushOpacity(1 - collapseAll))
            DrawSheet(context, new Rect(CardPad, CardPad, ContentWidth(Layout), ItemsBottom(Layout, n) - CardPad).Inflate(SheetMargin));
        for (var i = 0; i < n; i++)
        {
            var t = Math.Clamp((_collapseClock - i * CollapseStagger) / CollapseSeconds, 0, 1);
            var c = EaseInCubic(t);
            if (c >= 0.999)
                continue;
            var rect = CardRect(i);
            var from = rect.Center;
            var to = new Point(Lerp(from.X, center.X, c), Lerp(from.Y, center.Y, c));
            var scale = 1 - 0.8 * c;
            var transform = Matrix.CreateTranslation(-from.X, -from.Y) * Matrix.CreateScale(scale, scale) *
                            Matrix.CreateTranslation(to.X, to.Y);
            using (context.PushTransform(transform))
            using (context.PushOpacity(1 - c))
                DrawCard(context, Items![i], rect, i == ActiveIndex ? 1 : 0, i == ActiveIndex, i + 1,
                    Layout == MenuLayout.List, bold, regular);
        }
        var plate = RingRadius + RingStroke + 2;
        using (context.PushOpacity(EaseInCubic(collapseAll)))
            context.DrawEllipse(CenterBrush, BorderPen(), center, plate, plate);
    }

    private static FormattedText MakeAligned(string text, Typeface typeface, double size, IBrush? brush, double maxWidth,
        TextAlignment alignment) =>
        new(text, System.Globalization.CultureInfo.CurrentUICulture, TextDirection, typeface, size, brush)
        {
            MaxTextWidth = Math.Max(10, maxWidth),
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = alignment,
        };
}
