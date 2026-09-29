using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.UI;

//Die gezeichnete Karte einer erzeugten Ebene. Sie liest den Grundriss und zeigt, was der Held schon gesehen hat
public partial class LevelMap : Control
{
    private const float HeroMarkMeters = 1.2f;
    private const float ExitMarkMeters = 1.6f;

    private readonly Vector2[] corners = new Vector2[4];

    private Vector3 right;
    private Vector3 up;

    [Export]
    public Descent Descent { get; set; }

    [Export]
    public Hero Hero { get; set; }

    //Die Karte der Oberfläche bleibt unter der Erde aus
    [Export]
    public OverlayMapViewport SurfaceMap { get; set; }

    //So viele Meter der Welt zeigt die Karte von oben nach unten
    [Export]
    public float ViewSizeMeters { get; set; } = 90f;

    [Export]
    public Color FloorColor { get; set; } = new(0.75f, 0.7f, 0.6f, 0.16f);

    [Export]
    public Color WallColor { get; set; } = new(0.95f, 0.9f, 0.8f, 0.75f);

    [Export]
    public Color HeroColor { get; set; } = new(0.4f, 0.9f, 1f, 0.95f);

    [Export]
    public Color ExitColor { get; set; } = new(1f, 0.82f, 0.25f, 0.95f);

    [Export]
    public float WallWidthPx { get; set; } = 2f;

    //Zum Testen: zeigt die ganze Ebene, der Stand der Erkundung bleibt dabei, wie er ist
    public bool ShowsEverything { get; private set; }

    public override void _Ready()
    {
        Visible     = false;
        MouseFilter = MouseFilterEnum.Ignore;

        if (Descent is not null)
            Descent.LevelEntered += OnLevelEntered;
    }

    public override void _Process(double delta)
    {
        if (Descent?.Level is null)
            return;

        if (Input.IsActionJustPressed(InputActions.ToggleOverlayMap))
            Visible = !Visible;

        if (Visible)
            QueueRedraw();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F5 })
            ToggleShowsEverything();
    }

    public void ToggleShowsEverything()
    {
        ShowsEverything = !ShowsEverything;

        QueueRedraw();
    }

    public bool IsShown(Cell cell)
        => Descent?.Level is { } level && level.Layout.IsFloor(cell) && (ShowsEverything || Descent.Exploration.IsRevealed(cell));

    public override void _Draw()
    {
        if (Descent?.Level is not { } level || !IsInstanceValid(Hero))
            return;

        var camera = GetViewport().GetCamera3D();

        if (camera is null)
            return;

        right = camera.GlobalBasis.X;
        up    = camera.GlobalBasis.Y;

        for (var y = 0; y < level.Layout.Height; y++)
        {
            for (var x = 0; x < level.Layout.Width; x++)
            {
                if (IsShown(new Cell(x, y)))
                    DrawCell(level, new Cell(x, y));
            }
        }

        foreach (var exit in level.Exits)
        {
            if (IsInstanceValid(exit) && IsShown(level.Grid.GetCell(exit.GlobalPosition)))
                DrawMark(exit.GlobalPosition, ExitMarkMeters, ExitColor);
        }

        DrawMark(Hero.GlobalPosition, HeroMarkMeters, HeroColor);
    }

    private void DrawCell(BuiltLevel level, Cell cell)
    {
        corners[0] = ToMap(level.Grid.GetCorner(cell.X, cell.Y));
        corners[1] = ToMap(level.Grid.GetCorner(cell.X + 1, cell.Y));
        corners[2] = ToMap(level.Grid.GetCorner(cell.X + 1, cell.Y + 1));
        corners[3] = ToMap(level.Grid.GetCorner(cell.X, cell.Y + 1));

        DrawColoredPolygon(corners, FloorColor);

        for (var side = 0; side < SideExtensions.All.Length; side++)
        {
            if (level.Layout.HasWall(cell, SideExtensions.All[side]))
                DrawLine(corners[side], corners[(side + 1) % corners.Length], WallColor, WallWidthPx);
        }
    }

    private void DrawMark(Vector3 place, float sizeMeters, Color color)
    {
        var half = sizeMeters / 2f;

        corners[0] = ToMap(place + Vector3.Forward * half);
        corners[1] = ToMap(place + Vector3.Right * half);
        corners[2] = ToMap(place + Vector3.Back * half);
        corners[3] = ToMap(place + Vector3.Left * half);

        DrawColoredPolygon(corners, color);
    }

    //Die Karte blickt aus dem Winkel der Spielkamera, der Held steht in der Mitte
    private Vector2 ToMap(Vector3 place)
    {
        var offset = place - Hero.GlobalPosition;
        var scale  = Size.Y / ViewSizeMeters;

        return Size / 2f + new Vector2(offset.Dot(right), -offset.Dot(up)) * scale;
    }

    private void OnLevelEntered()
    {
        if (SurfaceMap is { IsSuspended: false })
        {
            Visible                = SurfaceMap.GetParent<SubViewportContainer>().Visible;
            SurfaceMap.IsSuspended = true;
        }

        QueueRedraw();
    }
}
