using Godot;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class LevelNavigation : NavigationRegion3D
{
    private const string SourceGroup = "level_navigation_source_3d";

    //Alle Kollisionsformen unterhalb dieses Knotens werden zu Boden und Hindernissen, leer bedeutet der Elternknoten
    [Export]
    public Node3D SourceRoot { get; set; }

    [Export(PropertyHint.Layers3DPhysics)]
    public uint SourceLayers { get; set; } = CollisionLayers.NavigationSources;

    //Wege halten diesen Abstand zu Mauern. Er richtet sich nach den großen Körpern, sonst schleifen sie um jede Ecke
    [Export]
    public float AgentRadius { get; set; } = 0.75f;

    [Export]
    public float AgentHeight { get; set; } = 2f;

    private ulong bakeStartMsec;

    public bool IsBaked { get; private set; }

    //Wie lange das letzte Backen im Hintergrund gedauert hat
    public ulong LastBakeMsec { get; private set; }

    public override void _Ready()
    {
        BakeFinished += () =>
        {
            IsBaked      = true;
            LastBakeMsec = Time.GetTicksMsec() - bakeStartMsec;
        };

        Rebuild();
    }

    public void Rebuild()
    {
        IsBaked       = false;
        bakeStartMsec = Time.GetTicksMsec();

        (SourceRoot ?? GetParent<Node3D>()).AddToGroup(SourceGroup);

        NavigationMesh = new NavigationMesh
        {
            AgentRadius                = AgentRadius,
            AgentHeight                = AgentHeight,
            GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders,
            GeometryCollisionMask      = SourceLayers,
            GeometrySourceGeometryMode = NavigationMesh.SourceGeometryMode.GroupsWithChildren,
            GeometrySourceGroupName    = SourceGroup
        };

        BakeNavigationMesh();
    }
}
