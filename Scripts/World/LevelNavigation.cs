using Godot;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class LevelNavigation : NavigationRegion2D
{
    private const string SourceGroup    = "level_navigation_source";
    private const float  GroundMarginPx = 64f;

    [Export]
    public TileMapLayer Ground { get; set; }

    //Alle Kollisionsformen unterhalb dieses Knotens werden zu Hindernissen, leer bedeutet der Elternknoten
    [Export]
    public Node2D ObstacleRoot { get; set; }

    [Export(PropertyHint.Layers2DPhysics)]
    public uint ObstacleLayers { get; set; } = CollisionLayers.Walls;

    //So weit bleibt die Mitte einer Einheit von Wänden weg. Größere Einheiten streifen an Ecken entlang
    [Export]
    public float AgentRadius { get; set; } = 40f;

    public bool IsBaked { get; private set; }

    public override void _Ready()
    {
        BakeFinished += () => IsBaked = true;

        Rebuild();
    }

    public void Rebuild()
    {
        IsBaked = false;

        (ObstacleRoot ?? GetParent<Node2D>()).AddToGroup(SourceGroup);

        var polygon = new NavigationPolygon
        {
            AgentRadius             = AgentRadius,
            ParsedGeometryType      = NavigationPolygon.ParsedGeometryTypeEnum.StaticColliders,
            ParsedCollisionMask     = ObstacleLayers,
            SourceGeometryMode      = NavigationPolygon.SourceGeometryModeEnum.GroupsWithChildren,
            SourceGeometryGroupName = SourceGroup
        };

        var bounds = GetGroundBounds();

        if (bounds.HasArea())
            polygon.AddOutline([bounds.Position, new Vector2(bounds.End.X, bounds.Position.Y), bounds.End, new Vector2(bounds.Position.X, bounds.End.Y)]);

        NavigationPolygon = polygon;

        BakeNavigationPolygon();
    }

    //Begehbar ist das Rechteck um den Boden. Der genaue Rand des Bodens wäre gezackt und zerlegte das Netz in hunderte Splitter
    private Rect2 GetGroundBounds()
    {
        if (Ground?.TileSet is null)
            return new Rect2();

        var toLocal  = GlobalTransform.AffineInverse() * Ground.GlobalTransform;
        var hasCells = false;
        var bounds   = new Rect2();

        foreach (var cell in Ground.GetUsedCells())
        {
            var center = toLocal * Ground.MapToLocal(cell);

            bounds   = hasCells ? bounds.Expand(center) : new Rect2(center, Vector2.Zero);
            hasCells = true;
        }

        return hasCells ? bounds.Grow(GroundMarginPx) : bounds;
    }
}
