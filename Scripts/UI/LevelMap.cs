using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.UI;

//Die gezeichnete Karte einer erzeugten Ebene. Sie liest den Grundriss und zeigt, was der Held schon gesehen hat
public partial class LevelMap : Control, IClosableWindow
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

    //Freier Boden einer Fläche ist heller als der Boden der Räume, Hindernisse sind dunkel
    [Export]
    public Color GroundColor { get; set; } = new(0.85f, 0.8f, 0.7f, 0.24f);

    [Export]
    public Color ObstacleColor { get; set; } = new(0.08f, 0.07f, 0.06f, 0.6f);

    [Export]
    public Color HeroColor { get; set; } = new(0.4f, 0.9f, 1f, 0.95f);

    [Export]
    public Color ExitColor { get; set; } = new(1f, 0.82f, 0.25f, 0.95f);

    [Export]
    public Color EntranceColor { get; set; } = new(0.75f, 0.75f, 0.75f, 0.95f);

    [Export]
    public Color TownPortalColor { get; set; } = new(0.35f, 0.55f, 1f, 0.95f);

    [Export]
    public float WallWidthPx { get; set; } = 2f;

    //Zum Testen: zeigt die ganze Ebene, der Stand der Erkundung bleibt dabei, wie er ist
    public bool ShowsEverything { get; private set; }

    public override void _Ready()
    {
        Visible     = false;
        MouseFilter = MouseFilterEnum.Ignore;

        if (Descent is null)
            return;

        Descent.LevelEntered += OnLevelEntered;
        Descent.PlaceEntered += OnPlaceEntered;
    }

    public override void _Process(double delta)
    {
        if (Descent?.Level is null)
            return;

        if (Input.IsActionJustPressed(InputActions.ToggleOverlayMap) && !InputActions.IsTyping(GetViewport()))
            Visible = !Visible;

        if (Visible)
            QueueRedraw();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (OS.IsDebugBuild() && @event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F5 })
            ToggleShowsEverything();
    }

    //Über der Erde steht die Karte der Oberfläche für diese hier
    public bool IsOpen => Visible || (SurfaceMap?.GetParent<SubViewportContainer>().Visible ?? false);

    public void Close()
    {
        Visible = false;

        if (SurfaceMap is not null)
            SurfaceMap.GetParent<SubViewportContainer>().Visible = false;
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
                var cell = new Cell(x, y);

                if (IsShown(cell))
                    DrawCell(level, cell);
                else if (level.Layout.GetKind(cell) == CellKind.Obstacle && IsBesideShown(cell))
                    DrawObstacle(level, cell);
            }
        }

        foreach (var exit in level.Exits)
            DrawMarkOf(level, exit, ExitColor);

        foreach (var entrance in level.Entrances)
            DrawMarkOf(level, entrance, EntranceColor);

        DrawMarkOf(level, Descent.OpenPortal, TownPortalColor);

        DrawMark(Hero.GlobalPosition, HeroMarkMeters, HeroColor);
    }

    //Zwischen Boden und Hindernis zieht die Karte keine Mauer, das dunkle Hindernis zeigt die Grenze
    private void DrawCell(BuiltLevel level, Cell cell)
    {
        SetCorners(level, cell);

        DrawColoredPolygon(corners, level.Layout.GetKind(cell) == CellKind.Ground ? GroundColor : FloorColor);

        for (var side = 0; side < SideExtensions.All.Length; side++)
        {
            var across = SideExtensions.All[side];

            if (level.Layout.HasWall(cell, across) && level.Layout.GetKind(cell.Step(across)) != CellKind.Obstacle)
                DrawLine(corners[side], corners[(side + 1) % corners.Length], WallColor, WallWidthPx);
        }
    }

    private void DrawObstacle(BuiltLevel level, Cell cell)
    {
        SetCorners(level, cell);

        DrawColoredPolygon(corners, ObstacleColor);
    }

    private void SetCorners(BuiltLevel level, Cell cell)
    {
        corners[0] = ToMap(level.Grid.GetCorner(cell.X, cell.Y));
        corners[1] = ToMap(level.Grid.GetCorner(cell.X + 1, cell.Y));
        corners[2] = ToMap(level.Grid.GetCorner(cell.X + 1, cell.Y + 1));
        corners[3] = ToMap(level.Grid.GetCorner(cell.X, cell.Y + 1));
    }

    //Ein Hindernis zeigt die Karte, sobald Boden daneben aufgedeckt ist
    private bool IsBesideShown(Cell cell)
    {
        foreach (var side in SideExtensions.All)
        {
            if (IsShown(cell.Step(side)))
                return true;
        }

        return false;
    }

    private void DrawMarkOf(BuiltLevel level, Node3D passage, Color color)
    {
        if (IsInstanceValid(passage) && IsShown(level.Grid.GetCell(passage.GlobalPosition)))
            DrawMark(passage.GlobalPosition, ExitMarkMeters, color);
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

    //Über der Erde zeigt wieder die Karte der Oberfläche, was zu sehen ist
    private void OnPlaceEntered()
    {
        if (SurfaceMap is { IsSuspended: true })
        {
            SurfaceMap.IsSuspended                                = false;
            SurfaceMap.GetParent<SubViewportContainer>().Visible = Visible;
        }

        Visible = false;
    }
}
