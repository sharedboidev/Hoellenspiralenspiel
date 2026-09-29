using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Spatial;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Objects;

public partial class Lootbag
        : Area3D,
          IUsable
{
    private const float BounceMeters    = 0.5f;
    private const float HoverBrightness = 1.5f;
    private const float HoverGlow       = 3f;

    private const float GridSpacingPx     = 100f;
    private const float GridReachPx       = 800f;
    private const float BagRadiusPx       = 30f;
    private const float FreeDistancePx    = 75f;
    private const float SightHeightMeters = 0.25f;

    private static readonly PackedScene Scene = ResourceLoader.Load<PackedScene>("res://Scenes/Objects/lootbag.tscn");

    private static readonly StringName TintParameter           = "tint";
    private static readonly StringName EmissionEnergyParameter = "emission_energy";

    private static readonly string[] LeatherParts = ["Sack", "Knot"];

    private static readonly List<Lootbag> LyingBags = new();

    private readonly List<(ShaderMaterial Material, float Glow)> leather = new();

    private CharacterItems collector;

    //In Pixeln wie alle Reichweiten. Wer weiter entfernt steht, muss erst hinlaufen
    [Export]
    public float PickupRadius { get; set; } = 150f;

    public static IReadOnlyList<Lootbag> Lying => LyingBags;

    public ItemInstance ContainedItem { get; private set; }

    public bool IsHighlighted { get; private set; }

    //Appeared meldet sich erst, wenn der Beutel an seinem Platz liegt
    public static event Action<Lootbag> Appeared;
    public static event Action<Lootbag> Vanished;

    //Jeder Beutel bekommt sein eigenes Material, sonst hellte die Maus alle Beutel zugleich auf
    public override void _Ready()
    {
        foreach (var partName in LeatherParts)
        {
            var part = GetNodeOrNull<MeshInstance3D>(partName);

            if (part?.GetActiveMaterial(0) is not ShaderMaterial material)
                continue;

            var ownMaterial = (ShaderMaterial)material.Duplicate();

            part.SetSurfaceOverrideMaterial(0, ownMaterial);

            leather.Add((ownMaterial, ownMaterial.GetShaderParameter(EmissionEnergyParameter).AsSingle()));
        }
    }

    //Das Licht allein reicht nicht: Die Seite, die vom Helden wegzeigt, liegt im Dunkeln und bliebe dunkel
    public void SetHighlight(bool active)
    {
        IsHighlighted = active;

        foreach (var (material, glow) in leather)
        {
            material.SetShaderParameter(TintParameter, Vector3.One * (active ? HoverBrightness : 1f));
            material.SetShaderParameter(EmissionEnergyParameter, glow * (active ? HoverGlow : 1f));
        }
    }

    public static Lootbag Drop(Node parent, Vector3 globalPosition, ItemInstance item, CharacterItems collector)
    {
        var lootbag = Scene.Instantiate<Lootbag>();

        lootbag.ContainedItem = item;
        lootbag.collector     = collector;

        parent.AddChild(lootbag);

        lootbag.GlobalPosition = WorldScale.OnGround(globalPosition);

        Appeared?.Invoke(lootbag);

        return lootbag;
    }

    //Wer etwas abwirft, steht selbst noch da. Der Beutel hält deshalb Abstand zu seinem Körper
    public static Lootbag DropAround(BaseUnit dropper, ItemInstance item, CharacterItems collector)
        => DropAround(dropper.GetParent<Node3D>(), dropper.GlobalPosition, dropper.BodyRadiusPx + BagRadiusPx, item, collector);

    //Der Beutel landet auf dem nächsten freien Punkt eines Gitters um die Mitte, nie auf einem anderen Beutel
    public static Lootbag DropAround(Node3D parent, Vector3 center, float minDistancePx, ItemInstance item, CharacterItems collector)
    {
        var onGround = WorldScale.OnGround(center);
        var space    = parent.GetWorld3D().DirectSpaceState;

        GridSearch.TryFind(new Spot(WorldScale.ToPx(onGround.X), WorldScale.ToPx(onGround.Z)),
                           GridSpacingPx,
                           minDistancePx,
                           GridReachPx,
                           spot => IsFree(ToWorld(spot), onGround, space),
                           out var found);

        return Drop(parent, ToWorld(found), item, collector);
    }

    private static Vector3 ToWorld(Spot spot)
        => new(WorldScale.ToMeters(spot.X), 0f, WorldScale.ToMeters(spot.Y));

    //Frei ist ein Punkt ohne Beutel in der Nähe, den man von der Mitte aus sieht. Hinter einer Mauer käme niemand mehr an das Item
    private static bool IsFree(Vector3 point, Vector3 center, PhysicsDirectSpaceState3D space)
    {
        foreach (var lootbag in LyingBags)
        {
            if (!lootbag.IsQueuedForDeletion() && WorldScale.GroundDistancePx(lootbag.GlobalPosition, point) < FreeDistancePx)
                return false;
        }

        var sight = PhysicsRayQueryParameters3D.Create(center + Vector3.Up * SightHeightMeters, point + Vector3.Up * SightHeightMeters, CollisionLayers.Walls);

        return space.IntersectRay(sight).Count == 0;
    }

    public override void _EnterTree()
        => LyingBags.Add(this);

    public override void _ExitTree()
    {
        LyingBags.Remove(this);

        Vanished?.Invoke(this);
    }

    public static Lootbag FindUnderMouse(Node3D asker)
        => FindAt(asker, asker.GetViewport().GetMousePosition());

    public static Lootbag FindAt(Node3D asker, Vector2 screenPoint)
        => Usables.FindAt(asker, screenPoint) as Lootbag;

    public bool IsInReachOf(BaseUnit unit)
        => unit.DistancePxTo(GlobalPosition) <= PickupRadius;

    public void Use()
        => Collect();

    public void Collect()
    {
        if (collector.PickUp(ContainedItem))
            QueueFree();
        else
            BounceAndFlip();
    }

    public void BounceAndFlip()
    {
        var startPosition = Position;
        var startRotation = Rotation;
        //Der Tween hängt am Beutel und endet mit ihm, sonst liefe er nach dem Aufheben ins Leere
        var tween = CreateTween();

        tween.SetParallel();
        tween.TweenProperty(this, "position", startPosition + Vector3.Up * BounceMeters, 0.1f);
        tween.TweenProperty(this, "position", startPosition, 0.2f).SetDelay(0.1f);
        tween.TweenProperty(this, "rotation", startRotation + Vector3.Up * Mathf.Tau, 0.2f);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => Rotation = startRotation));
    }
}
