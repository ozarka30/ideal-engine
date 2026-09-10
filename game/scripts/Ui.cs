using System;
using System.Collections.Generic;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// The one hit list every screen registers its interactive elements in (D-47): rect, action, hint. Resolution
/// prefers the smallest rect under the point, and on a miss accepts the nearest rect within a touch slop, so
/// 16 px buttons remain tappable on a phone without drawing them larger.
/// </summary>
public sealed class Hits
{
    public const int TouchSlop = 6;

    public readonly record struct Hit(Rect2I Rect, Action Click, string Hint);

    private readonly List<Hit> _hits = new();

    public void Clear() => _hits.Clear();

    public void Add(Rect2I rect, Action click, string hint) => _hits.Add(new Hit(rect, click, hint));

    public IReadOnlyList<Hit> All => _hits;

    public Hit? At(Vector2I p)
    {
        Hit? best = null;
        foreach (Hit h in _hits)
        {
            if (!h.Rect.HasPoint(p)) continue;
            if (best == null || Area(h.Rect) < Area(best.Value.Rect)) best = h;
        }
        if (best != null) return best;
        foreach (Hit h in _hits)
        {
            Rect2I grown = h.Rect.Grow(TouchSlop);
            if (!grown.HasPoint(p)) continue;
            if (best == null || Area(h.Rect) < Area(best.Value.Rect)) best = h;
        }
        return best;
    }

    public string HintAt(Vector2I p) => At(p)?.Hint ?? string.Empty;

    private static long Area(Rect2I r) => (long)r.Size.X * r.Size.Y;
}

/// <summary>Shared widgets in the greybox style (GAME_DESIGN.md §13): buttons invert on hover, and the pressed state offsets the label.</summary>
public static class Ui
{
    public static void Button(CanvasItem c, Rect2I rect, string label, string tone, bool enabled = true, string? slot = null)
    {
        ScreenRouter r = ScreenRouter.Instance;
        Vector2 m = c.GetViewport().GetMousePosition();
        bool hover = enabled && rect.HasPoint(new Vector2I((int)m.X, (int)m.Y));
        bool pressed = hover && Input.IsMouseButtonPressed(MouseButton.Left);
        Color fill = enabled ? Tones.Fill(tone) : Tones.Fill("structure");
        Color text = enabled ? Tones.Text(tone) : Tones.Hatch("interface");
        if (hover) (fill, text) = (text, fill);

        // A button with art draws it (D-74); the greybox rect is what a slot without art falls back to.
        if (slot != null && r.Manifest.Present.Contains(slot))
        {
            c.DrawTextureRect(r.Textures.For(r.Layout.Entry(slot)), new Rect2(rect.Position, rect.Size), false,
                enabled ? Colors.White : new Color(1, 1, 1, 0.5f));
        }
        else
        {
            c.DrawRect(new Rect2(rect.Position, rect.Size), fill);
            c.DrawRect(new Rect2(rect.Position, rect.Size), Tones.Border(tone), false);
        }
        int size = rect.Size.Y >= 20 ? r.Font.Large : r.Font.Small;
        int textH = size;
        r.Font.Draw(c, rect.Position.X, rect.Position.Y + (rect.Size.Y - textH) / 2 + (pressed ? 1 : 0), label, size, text, HorizontalAlignment.Center, rect.Size.X);
    }

    /// <summary>Wraps at word boundaries to a pixel width, measured with the face that will draw it (the faces are proportional).</summary>
    public static IEnumerable<string> Wrap(PixelFont font, int size, string text, int width, int maxLines)
    {
        var lines = new List<string>();
        string current = string.Empty;
        foreach (string word in text.Split(' '))
        {
            if (current.Length > 0 && font.Width(current + " " + word, size) > width)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = current.Length == 0 ? word : current + " " + word;
            }
        }
        if (current.Length > 0) lines.Add(current);
        return lines.GetRange(0, Math.Min(maxLines, lines.Count));
    }

    /// <summary>Abbreviates by content, never mid-word past the cut: "Reception" → "Recep.".</summary>
    public static string Abbrev(string name, int max)
    {
        if (name.Length <= max) return name;
        return name[..Math.Max(1, max - 1)] + ".";
    }

    /// <summary>Side tones: A and B never borrow a category tone (Morale is people, Anomaly is anomalous).</summary>
    public static string SideTone(string side) => side == "A" ? "operations" : "support";

    /// <summary>Tier as a colour, the auto-battler convention (D-68): T1 neutral, T2 ochre, T3 teal, beyond that violet.</summary>
    public static string TierTone(long tier) => tier switch { 1 => "interface", 2 => "support", 3 => "operations", _ => "anomalous" };

    /// <summary>A department's tone for the tower facade markers.</summary>
    public static string DeptTone(string dept) => dept switch
    {
        "engineering" => "operations",
        "sales" => "support",
        "hr" => "people",
        "legal" => "structure",
        "management" => "interface",
        "extraplanar" => "anomalous",
        _ => "interface",
    };
}
