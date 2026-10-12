using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Ein Lavariss im Boden. Er sucht sich seinen gezackten Weg zwischen den anderen Rissen hindurch (FissurePath), wächst vom Anfang aus
//auf seine Länge und liegt seine Dauer lang. Wen er beim Wachsen erreicht, den trifft er einmal je Schlag. Wer darauf steht, brennt (Lava). Ein Hauptriss gibt den ersten Gegnern, die er trifft,
//je einen Nebenriss in eine freie Richtung, bis das Proliferate des Wirkenden verbraucht ist. Danach kühlt er ab und verschwindet
public partial class LavaCrack : Node3D
{
    //In dieser Höhe prüft der Riss, ob eine Mauer im Weg steht
    public const float WallCheckHeight = 0.5f;

    private const float SearchMarginPx = 100f;

    private const string ShaderPath = "res://Shaders/Ps1/ps1_lava_crack.gdshader";

    //Rastet wie der Boden ein, Ps1Look stellt ihn mit ein
    public static Shader Shader { get; } = GD.Load<Shader>(ShaderPath);

    private static readonly StringName RevealParameter = "reveal";
    private static readonly StringName HeatParameter   = "heat";
    private static readonly StringName FadeParameter   = "fade";

    //Alle Risse, die gerade im Level liegen. Ein neuer weicht ihnen aus
    private static readonly List<LavaCrack> Active = new();

    //Die Hauptrisse, die noch brennen, der älteste zuerst. Über der Grenze löst sich der älteste desselben Wirkenden auf
    private static readonly List<LavaCrack> Burning = new();

    private readonly List<OmniLight3D> lights        = new();
    private readonly List<float>       lightDistance = new();
    private readonly List<BaseUnit>    unitsInRange  = new();

    private double          activeSec;
    private float           angle;
    private FissureBranches branches;
    private BaseUnit        caster;
    private Faction         faction;
    private float           heat = 1f;
    private bool            isCooling;
    private float           lightEnergy;
    private int             mainCrack;
    private SkillCast       hit;
    private bool            hasGrown;
    private HitRequest      perSecond;
    private PackedScene     scene;
    private FissureSettings settings;
    private FissureShape    shape;
    private Vector3         start;

    [Export]
    public MeshInstance3D Crack { get; set; }

    //Ein Licht, das sich entlang des Risses vervielfältigt, etwa alle LightSpacingMeters eins
    [Export]
    public OmniLight3D Glow { get; set; }

    [Export]
    public float LightSpacingMeters { get; set; } = 3.5f;

    //Die sichtbare Breite eines Hauptrisses an seiner breitesten Stelle, samt verkohltem Rand. Der Schaden zählt FissureWidth
    [Export]
    public float VisualWidthMeters { get; set; } = 0.6f;

    //Nach seiner Dauer erkaltet die Lava so lange, danach zerfällt der Riss
    [Export]
    public float CoolSec { get; set; } = 0.8f;

    [Export]
    public float FadeSec { get; set; } = 0.6f;

    //angle in Bogenmaß, 0 zeigt nach +X. mainCrack zählt die Hauptrisse eines Schlags ab 0, ein Nebenriss hat -1 und sucht sich seine Richtung selbst
    public static LavaCrack Open(PackedScene     crackScene,
                                 BaseUnit        crackCaster,
                                 HitRequest      crackPerSecond,
                                 SkillCast       crackHit,
                                 FissureSettings fissure,
                                 Vector3         from,
                                 float           crackAngle,
                                 FissureBranches crackBranches,
                                 int             mainCrackIndex,
                                 Node            parent = null)
    {
        var crack = crackScene.Instantiate<LavaCrack>();

        crack.scene     = crackScene;
        crack.caster    = crackCaster;
        crack.faction   = crackCaster.Faction;
        crack.perSecond = crackPerSecond;
        crack.hit       = crackHit;
        crack.settings  = fissure;
        crack.start     = WorldScale.OnGround(from);
        crack.angle     = crackAngle;
        crack.branches  = crackBranches;
        crack.mainCrack = mainCrackIndex;

        (parent ?? crackCaster.GetParent()).AddChild(crack);

        return crack;
    }

    public bool IsMainCrack => mainCrack >= 0;

    //Erkaltet oder zerfällt schon und brennt nicht mehr
    public bool IsCooling => isCooling;

    public FissureShape Shape => shape;

    public override void _Ready()
    {
        if (settings is null)
            return;

        GlobalPosition = start;

        var obstacles = new List<FissureSegment>();

        foreach (var other in Active)
            obstacles.AddRange(other.shape.Segments);

        var startX = WorldScale.ToPx(start.X);
        var startY = WorldScale.ToPx(start.Z);

        if (!IsMainCrack)
            angle = FissurePath.PickFreeAngle(startX, startY, obstacles, GameRandom.Shared, IsWallBetween);

        //Ein Hauptriss kreuzt andere Risse und knickt danach ab, ein Nebenriss weicht ihnen aus
        shape = FissurePath.Trace(startX, startY, angle, settings.Length, obstacles, GameRandom.Shared, IsWallBetween, null, IsMainCrack);

        Active.Add(this);

        if (IsMainCrack)
            LimitMainCracks();

        BuildMesh();
        PlaceLights();
        ShowGrowth(0f);
        ShowCooling(1f, 0f);
    }

    public override void _ExitTree()
    {
        Active.Remove(this);
        Burning.Remove(this);
    }

    private void LimitMainCracks()
    {
        Burning.Add(this);

        var own    = Burning.FindAll(crack => crack.caster == caster);
        var excess = settings.GetExcess(own.Count);

        for (var i = 0; i < excess; i++)
            own[i].Cool();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (shape is null || isCooling)
            return;

        activeSec += delta;

        if (settings.IsOver(activeSec))
        {
            Cool();

            return;
        }

        var grown = settings.GetGrownShare(activeSec);

        ShowGrowth(grown);
        BurnUnitsOn(grown, !hasGrown);

        hasGrown = grown >= 1f;
    }

    //isGrowing: Der Riss wächst noch oder hat in diesem Frame seine Länge erreicht. Nur dann läuft er frisch durch einen Gegner
    private void BurnUnitsOn(float grownShare, bool isGrowing)
    {
        var center = new Vector3(WorldScale.ToMeters(shape.CenterX), start.Y, WorldScale.ToMeters(shape.CenterY));
        var source = IsInstanceValid(caster) ? caster : null;

        UnitRegistry.FindNear(center, shape.ReachRadius + settings.HalfWidth + SearchMarginPx, unitsInRange);

        foreach (var unit in unitsInRange)
        {
            if (!IsInstanceValid(unit) || !unit.IsTargetable || unit.Faction == faction)
                continue;

            var position = unit.GlobalPosition;

            if (!shape.Touches(WorldScale.ToPx(position.X), WorldScale.ToPx(position.Z), unit.BodyRadiusPx, settings.HalfWidth, grownShare))
                continue;

            if (isGrowing && hit is not null && hit.CanHit(unit))
                hit.ApplyTo(unit);

            //Wer am Treffer stirbt, brennt nicht mehr
            if (unit.IsTargetable)
                unit.TryApplyStatusEffect(GroundDamage.Create(StatusEffectKind.Lava, perSecond, unit.Stats, source));

            //Der Nebenriss beginnt unter dem Gegner, auch wenn er gerade daran stirbt
            if (source is not null && IsMainCrack && branches.TryClaim(mainCrack, unit.GetInstanceId()))
                Open(scene, source, perSecond, hit, settings, position, 0f, branches, -1, GetParent());
        }
    }

    private bool IsWallBetween(float fromX, float fromY, float toX, float toY)
    {
        var from  = new Vector3(WorldScale.ToMeters(fromX), start.Y + WallCheckHeight, WorldScale.ToMeters(fromY));
        var to    = new Vector3(WorldScale.ToMeters(toX), start.Y + WallCheckHeight, WorldScale.ToMeters(toY));
        var query = PhysicsRayQueryParameters3D.Create(from, to, CollisionLayers.Walls);

        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count > 0;
    }

    //Erst erkaltet die Lava, dann zerfällt der Riss im Punktmuster der PS1. Brennen tut er dabei nicht mehr
    private void Cool()
    {
        if (isCooling)
            return;

        isCooling = true;

        Burning.Remove(this);

        ShowGrowth(1f);

        var tween = CreateTween();

        tween.TweenMethod(Callable.From<float>(value => ShowCooling(1f - value, 0f)), 0f, 1f, CoolSec);
        tween.TweenMethod(Callable.From<float>(value => ShowCooling(0f, value)), 0f, 1f, FadeSec);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void ShowGrowth(float grownShare)
    {
        var revealMeters = WorldScale.ToMeters(shape.Length * grownShare);

        Crack?.SetInstanceShaderParameter(RevealParameter, revealMeters);

        for (var i = 0; i < lights.Count; i++)
            lights[i].Visible = lightDistance[i] <= shape.Length * grownShare;
    }

    private void ShowCooling(float lavaHeat, float fade)
    {
        heat = lavaHeat;

        Crack?.SetInstanceShaderParameter(HeatParameter, heat);
        Crack?.SetInstanceShaderParameter(FadeParameter, fade);

        foreach (var light in lights)
            light.LightEnergy = lightEnergy * heat;
    }

    //Ein Band je Linie, flach auf dem Boden. UV.x ist der Weg ab dem Anfang des Risses in Metern, UV.y geht quer von 0 bis 1.
    //Rot der Farbe ist die Glut: 1 im Riss, weniger in den Haarrissen
    private void BuildMesh()
    {
        if (Crack is null)
            return;

        var tool = new SurfaceTool();

        tool.Begin(Mesh.PrimitiveType.Triangles);

        var vertexCount = AddBand(tool, shape.Points, 1f, 0);

        foreach (var twig in shape.Twigs)
            vertexCount = AddBand(tool, twig, 0.5f, vertexCount);

        Crack.Mesh = tool.Commit();
    }

    private int AddBand(SurfaceTool tool, IReadOnlyList<FissurePoint> points, float glow, int firstVertex)
    {
        if (points.Count < 2)
            return firstVertex;

        var originX = WorldScale.ToPx(start.X);
        var originY = WorldScale.ToPx(start.Z);

        for (var i = 0; i < points.Count; i++)
        {
            var before  = points[Mathf.Max(0, i - 1)];
            var after   = points[Mathf.Min(points.Count - 1, i + 1)];
            var tangent = new Vector2(after.X - before.X, after.Y - before.Y).Normalized();
            var side    = new Vector3(-tangent.Y, 0f, tangent.X) * (VisualWidthMeters / 2f * points[i].Width);
            var center  = new Vector3(WorldScale.ToMeters(points[i].X - originX), 0f, WorldScale.ToMeters(points[i].Y - originY));
            var along   = WorldScale.ToMeters(points[i].Distance);

            foreach (var (offset, across) in new[] { (side, 0f), (-side, 1f) })
            {
                tool.SetNormal(Vector3.Up);
                tool.SetColor(new Color(glow, 0f, 0f));
                tool.SetUV(new Vector2(along, across));
                tool.AddVertex(center + offset);
            }
        }

        for (var i = 0; i < points.Count - 1; i++)
        {
            var a = firstVertex + i * 2;

            tool.AddIndex(a);
            tool.AddIndex(a + 2);
            tool.AddIndex(a + 1);
            tool.AddIndex(a + 1);
            tool.AddIndex(a + 2);
            tool.AddIndex(a + 3);
        }

        return firstVertex + points.Count * 2;
    }

    //Die Lichter stehen gleichmäßig entlang des Risses und gehen an, sobald er sie erreicht
    private void PlaceLights()
    {
        if (Glow is null)
            return;

        lightEnergy = Glow.LightEnergy;

        var count = Mathf.Max(1, Mathf.RoundToInt(WorldScale.ToMeters(shape.Length) / Mathf.Max(0.5f, LightSpacingMeters)));

        for (var i = 0; i < count; i++)
        {
            var distance = shape.Length * (i + 0.5f) / count;
            var light    = i == 0 ? Glow : (OmniLight3D)Glow.Duplicate();

            if (i > 0)
                AddChild(light);

            light.Position = PointAt(distance) + Vector3.Up * Glow.Position.Y;

            lights.Add(light);
            lightDistance.Add(distance);
        }
    }

    private Vector3 PointAt(float distance)
    {
        var points = shape.Points;
        var origin = new Vector2(WorldScale.ToPx(start.X), WorldScale.ToPx(start.Z));

        for (var i = 1; i < points.Count; i++)
        {
            if (points[i].Distance < distance)
                continue;

            var t = (distance - points[i - 1].Distance) / Mathf.Max(0.0001f, points[i].Distance - points[i - 1].Distance);
            var x = Mathf.Lerp(points[i - 1].X, points[i].X, t) - origin.X;
            var y = Mathf.Lerp(points[i - 1].Y, points[i].Y, t) - origin.Y;

            return new Vector3(WorldScale.ToMeters(x), 0f, WorldScale.ToMeters(y));
        }

        return Vector3.Zero;
    }
}
