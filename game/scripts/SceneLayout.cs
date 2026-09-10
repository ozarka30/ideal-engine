using System;
using Godot;

namespace CompanyWars.Game;

/// <summary>
/// A screen's layout, read from its own scene (D-77). The `Layout` node's children are the authority for
/// where things go: drag one in the 2D editor, save, and the screen follows — there is no conversion step
/// and nothing to keep in sync. The manifest still owns sizes, anchors, footprints and draw order; a box
/// here is sized to match, but resizing it changes nothing until the manifest entry changes.
///
/// A missing or misnamed node throws rather than defaulting to the origin, because a slot silently at
/// (0, 0) is the one layout bug that looks like a rendering bug.
/// </summary>
public sealed class SceneLayout
{
    private readonly Node _root;
    private readonly string _screen;

    public SceneLayout(Node screen)
    {
        _screen = screen.Name;
        _root = screen.GetNodeOrNull("Layout")
                ?? throw new InvalidOperationException($"{_screen}: the scene has no Layout node (D-77)");
        // The boxes are guides: the 2D editor draws them, the game never does. Hiding at load
        // rather than in the scene keeps them visible where they are useful and nowhere else.
        if (_root is CanvasItem guides) guides.Visible = false;
    }

    private Node Slot(string name) =>
        _root.GetNodeOrNull(name)
        ?? throw new InvalidOperationException($"{_screen}: Layout has no slot named '{name}' (D-77)");

    /// <summary>
    /// A slot is a ColorRect where there is no art to show and a Sprite2D where there is, so the 2D editor
    /// draws the real thing. Both answer for position and extent; a Sprite2D's extent is its texture scaled,
    /// which for chrome at 2x (D-76) comes back as the slot's own size.
    /// </summary>
    private static (Vector2 Position, Vector2 Size) Box(Node n) => n switch
    {
        Control c => (c.Position, c.Size),
        Sprite2D s => (s.Position, (s.Texture?.GetSize() ?? Vector2.Zero) * s.Scale),
        Node2D d => (d.Position, Vector2.Zero),
        _ => (Vector2.Zero, Vector2.Zero),
    };

    /// <summary>Where the slot's top-left sits, in canvas pixels.</summary>
    public Vector2I At(string name)
    {
        Vector2 p = Box(Slot(name)).Position;
        return new Vector2I((int)Math.Round(p.X), (int)Math.Round(p.Y));
    }

    /// <summary>The slot's box. Width and height are the scene's, for hit-testing and centred text.</summary>
    public Rect2I Rect(string name)
    {
        (Vector2 p, Vector2 size) = Box(Slot(name));
        return new Rect2I(
            (int)Math.Round(p.X), (int)Math.Round(p.Y),
            (int)Math.Round(size.X), (int)Math.Round(size.Y));
    }

    public int X(string name) => At(name).X;

    public int Y(string name) => At(name).Y;

    /// <summary>A bare Node2D marker, for a repeating grid whose origin is draggable but whose pitch is code.</summary>
    public Vector2I Origin(string name)
    {
        Node2D n = _root.GetNodeOrNull<Node2D>(name)
                   ?? throw new InvalidOperationException($"{_screen}: Layout has no marker named '{name}' (D-77)");
        return new Vector2I((int)Math.Round(n.Position.X), (int)Math.Round(n.Position.Y));
    }
}
