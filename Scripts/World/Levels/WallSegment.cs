using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Ein gerades Stück Mauer längs seiner X-Achse, der Ursprung liegt am Boden in der Mitte.
//Unten der Sockel, darüber das Mauerwerk, das die Sicht auf den Helden freigibt. Als Tool zeigt es sich auch im Editor
[Tool]
public partial class WallSegment : StaticBody3D
{
    //Auf dieser Ebene liegen nur die Schattenwerfer der Mauern. Im PS1-Look werfen Lichter nur ihre Schatten
    public const uint ShadowLayer = 1u << 19;

    private const float  MetersPerTile = 2f;
    private const float  ProbeMeters   = 0.5f;
    private const float  ShadowInset   = 0.05f;
    private const string PreviewSkin   = "res://Textures/World/wall_brick.png";

    private static readonly StringName SeeThrough = "see_through";

    private static readonly Dictionary<Vector3, BoxMesh> MeshOf = new();

    private readonly List<Node> parts = new();

    private readonly List<(MeshInstance3D Mark, bool IsBehind)> marks = new();

    private float          height           = 2.5f;
    private bool           isBuilt;
    private float          length           = 4f;
    private MeshInstance3D masonry;
    private float          plinthBrightness = 0.55f;
    private float          plinthHeight     = 0.6f;
    private float          plinthLedge      = 0.06f;
    private Texture2D      texture;
    private float          thickness        = 0.5f;

    [Export]
    public float Length
    {
        get => length;
        set => Change(ref length, value);
    }

    [Export]
    public float Height
    {
        get => height;
        set => Change(ref height, value);
    }

    [Export]
    public float PlinthHeight
    {
        get => plinthHeight;
        set => Change(ref plinthHeight, value);
    }

    [Export]
    public float Thickness
    {
        get => thickness;
        set => Change(ref thickness, value);
    }

    //Der Sockel steht auf jeder Seite so weit vor
    [Export]
    public float PlinthLedge
    {
        get => plinthLedge;
        set => Change(ref plinthLedge, value);
    }

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float PlinthBrightness
    {
        get => plinthBrightness;
        set => Change(ref plinthBrightness, value);
    }

    [Export]
    public Texture2D Texture
    {
        get => texture;
        set => Change(ref texture, value);
    }

    public WallOpening Opening { get; private set; } = WallOpening.Open;

    public WallPlane Plane
    {
        get
        {
            var normal = WorldScale.OnGround(GlobalBasis.Z).Normalized();

            return new WallPlane(GlobalPosition.X, GlobalPosition.Z, normal.X, normal.Z);
        }
    }

    public override void _EnterTree()
    {
        if (!Engine.IsEditorHint())
            WallFade.Register(this);
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
            WallFade.Unregister(this);
    }

    public override void _Ready()
        => Build();

    public bool IsSeeThroughAt(Vector3 point)
        => point.Y > GlobalPosition.Y + PlinthHeight && WallFadeRule.IsSeeThrough(new WorldPoint(point.X, point.Y, point.Z), Plane, Opening, WallFade.View);

    //Welche Räume an den beiden Seiten liegen, zeigt ein Punkt kurz vor und kurz hinter der Mitte des Stücks
    public void Refresh(int roomOfHero)
    {
        var reach  = WorldScale.OnGround(GlobalBasis.Z).Normalized() * (Thickness / 2f + ProbeMeters);
        var center = GlobalPosition;

        Opening = WallOpeningRule.DecideForBothSides(RoomZone.GetIdAt(center + reach), RoomZone.GetIdAt(center - reach), roomOfHero);

        ShowOpening();
    }

    //Eine Spur auf dem Mauerwerk, sie öffnet sich mit ihm. Auf der Rückseite sind die Seiten der Öffnung vertauscht
    public void AddMark(MeshInstance3D mark, bool isBehind)
    {
        marks.Add((mark, isBehind));

        AddChild(mark);

        ShowOpening();
    }

    private void ShowOpening()
    {
        var opening = new Vector2(Opening.HeroAtNormal, Opening.HeroAtBack);

        masonry?.SetInstanceShaderParameter(SeeThrough, opening);

        foreach (var (mark, isBehind) in marks)
        {
            if (IsInstanceValid(mark))
                mark.SetInstanceShaderParameter(SeeThrough, isBehind ? new Vector2(opening.Y, opening.X) : opening);
        }
    }

    private void Change<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;

        if (!isBuilt)
            return;

        foreach (var part in parts)
        {
            RemoveChild(part);

            part.QueueFree();
        }

        parts.Clear();

        masonry = null;
        isBuilt = false;

        Build();
    }

    private void Build()
    {
        //Mauerstücke in Raumvorlagen bekommen ihre Textur erst vom Thema. Im Editor wären sie sonst unsichtbar
        var skin = Texture ?? (Engine.IsEditorHint() ? GD.Load<Texture2D>(PreviewSkin) : null);

        if (isBuilt || skin is null)
            return;

        isBuilt          = true;
        CollisionLayer   = CollisionLayers.Walls;
        CollisionMask    = 0;
        InputRayPickable = false;

        var plinthTop = Math.Clamp(PlinthHeight, 0f, Height);

        Add(new CollisionShape3D
        {
            Name      = nameof(CollisionShape3D),
            Shape     = new BoxShape3D { Size = new Vector3(Length, Height, Thickness) },
            Position  = Vector3.Up * (Height / 2f),
            DebugFill = false
        });

        if (plinthTop > 0f)
            AddPart("Plinth", new Vector3(Length + PlinthLedge * 2f, plinthTop, Thickness + PlinthLedge * 2f), plinthTop / 2f, WallFade.GetPlinth(skin, PlinthBrightness));

        if (Height > plinthTop)
            masonry = AddPart("Masonry", new Vector3(Length, Height - plinthTop, Thickness), (Height + plinthTop) / 2f, WallFade.GetMasonry(skin));

        AddShade();
        ShowOpening();
    }

    private MeshInstance3D AddPart(string name, Vector3 size, float centerHeight, Material material)
    {
        var part = new MeshInstance3D
        {
            Name       = name,
            Mesh       = GetMesh(size),
            Position   = Vector3.Up * centerHeight,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        part.SetSurfaceOverrideMaterial(0, material);

        Add(part);

        return part;
    }

    //Die Mauer steht auch dort, wo ihr Mauerwerk die Sicht freigibt. Ihr Schatten kommt deshalb von einem eigenen Körper, den man nie sieht.
    //Er ist etwas schmaler als die Mauer, sonst flimmerte ihr Schatten auf ihr selbst
    private void AddShade()
        => Add(new MeshInstance3D
        {
            Name       = "Shade",
            Mesh       = new BoxMesh { Size = new Vector3(Length, Height, Math.Max(0.05f, Thickness - ShadowInset * 2f)) },
            Position   = Vector3.Up * (Height / 2f),
            Layers     = ShadowLayer,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly
        });

    //Die Teile gehören niemandem und landen deshalb nicht in der gespeicherten Szene
    private void Add(Node part)
    {
        parts.Add(part);

        AddChild(part);
    }

    //Große Flächen verziehen sich im PS1-Look, deshalb zerfällt jede Seite in Kacheln
    private static BoxMesh GetMesh(Vector3 size)
    {
        if (MeshOf.TryGetValue(size, out var known))
            return known;

        return MeshOf[size] = new BoxMesh
        {
            Size            = size,
            SubdivideWidth  = GetCuts(size.X),
            SubdivideHeight = GetCuts(size.Y),
            SubdivideDepth  = GetCuts(size.Z)
        };
    }

    private static int GetCuts(float meters)
        => Math.Max(0, Mathf.CeilToInt(meters / MetersPerTile) - 1);
}
