using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.StatusLooks;

//Eine Kruste aus Eis über jedem Netz der Einheit, dazu ein paar Eiszacken. Bei einem Modell mit Skelett hängt jede Zacke an einem Knochen
//und folgt der Animation, sonst steht sie am Körper
public partial class BrittleCrust : StatusLook
{
    private static readonly StringName Snap           = "snap";
    private static readonly StringName SnapResolution = "snap_resolution";

    private readonly List<(GeometryInstance3D Mesh, Material Before)> crusted = new();
    private readonly List<Node3D>                                     spikes  = new();
    private          bool                                             isDetached;

    //Liegt als Overlay über jedem Netz. Durchscheinend, so bleibt der Umriss der Einheit unberührt
    [Export]
    public ShaderMaterial CrustMaterial { get; set; }

    //Eine Zacke mit dem Fuß im Ursprung, die entlang +Y wächst
    [Export]
    public PackedScene SpikeScene { get; set; }

    [Export]
    public int SpikeCount { get; set; } = 7;

    //Größe einer Zacke in Vielfachen ihrer Szene, zufällig dazwischen
    [Export]
    public Vector2 SpikeScale { get; set; } = new(0.7f, 1.3f);

    [Export]
    public float GrowSec { get; set; } = 0.15f;

    [Export]
    public float MeltSec { get; set; } = 0.2f;

    public override void Attach(Node3D visual, float height, float radius)
    {
        foreach (var mesh in visual.FindChildren("*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>())
        {
            AdoptSnap(mesh);

            crusted.Add((mesh, mesh.MaterialOverlay));

            mesh.MaterialOverlay = CrustMaterial;
        }

        if (SpikeScene is null)
            return;

        var skeleton = visual.FindChildren("*", nameof(Skeleton3D), true, false).OfType<Skeleton3D>().FirstOrDefault();

        for (var i = 0; i < SpikeCount; i++)
        {
            var spike = SpikeScene.Instantiate<Node3D>();

            if (skeleton is not null && skeleton.GetBoneCount() > 0)
                AttachToBone(skeleton, spike);
            else
                AttachToBody(visual, spike, height, radius);

            Grow(spike);
        }
    }

    public override void Detach()
    {
        if (isDetached)
            return;

        isDetached = true;

        foreach (var (mesh, before) in crusted)
        {
            if (IsInstanceValid(mesh) && mesh.MaterialOverlay == CrustMaterial)
                mesh.MaterialOverlay = before;
        }

        crusted.Clear();

        var melt = CreateTween().SetParallel();

        foreach (var spike in spikes.Where(IsInstanceValid))
            melt.TweenProperty(spike, "scale", Vector3.One * 0.01f, MeltSec);

        melt.Chain().TweenCallback(Callable.From(FreeAll));
    }

    //Die Kruste rastet auf demselben Raster ein wie der Körper darunter, sonst zappelt sie gegen ihn
    private void AdoptSnap(MeshInstance3D mesh)
    {
        if (CrustMaterial is null || mesh.GetActiveMaterial(0) is not ShaderMaterial body)
            return;

        if (body.GetShaderParameter(Snap).VariantType != Variant.Type.Nil)
            CrustMaterial.SetShaderParameter(Snap, body.GetShaderParameter(Snap));

        if (body.GetShaderParameter(SnapResolution).VariantType != Variant.Type.Nil)
            CrustMaterial.SetShaderParameter(SnapResolution, body.GetShaderParameter(SnapResolution));
    }

    //Die Zacke wächst aus dem Gelenk in eine zufällige Richtung und sticht durch den Körper nach außen
    private void AttachToBone(Skeleton3D skeleton, Node3D spike)
    {
        var anchor = new BoneAttachment3D { BoneName = skeleton.GetBoneName(GD.RandRange(0, skeleton.GetBoneCount() - 1)) };

        skeleton.AddChild(anchor);
        anchor.AddChild(spike);

        spike.Basis = Point(RandomDirection(-1f));

        spikes.Add(anchor);
    }

    private void AttachToBody(Node3D visual, Node3D spike, float height, float radius)
    {
        var direction = RandomDirection(0.1f);

        visual.AddChild(spike);

        spike.Position = new Vector3(direction.X * radius * 0.6f, height * (float)GD.RandRange(0.25, 0.85), direction.Z * radius * 0.6f);
        spike.Basis    = Point(direction);

        spikes.Add(spike);
    }

    private void Grow(Node3D spike)
    {
        var size = (float)GD.RandRange(SpikeScale.X, SpikeScale.Y);

        spike.Scale = Vector3.One * 0.01f;

        CreateTween().TweenProperty(spike, "scale", Vector3.One * size, GrowSec);
    }

    //minUp hebt die Richtung über die Waagerechte, -1 lässt jede zu
    private static Vector3 RandomDirection(float minUp)
    {
        var angle = (float)GD.RandRange(0, Mathf.Tau);
        var up    = (float)GD.RandRange(minUp, 0.8);
        var flat  = Mathf.Sqrt(Mathf.Max(0f, 1f - up * up));

        return new Vector3(Mathf.Cos(angle) * flat, up, Mathf.Sin(angle) * flat).Normalized();
    }

    private static Basis Point(Vector3 direction)
        => new(new Quaternion(Vector3.Up, direction));

    private void FreeAll()
    {
        foreach (var spike in spikes.Where(IsInstanceValid))
            spike.QueueFree();

        spikes.Clear();

        QueueFree();
    }
}
