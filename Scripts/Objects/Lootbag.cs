using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.World;

namespace Hoellenspiralenspiel.Scripts.Objects;

public partial class Lootbag : Area3D
{
    private const float PickRayMeters = 300f;
    private const float BounceMeters  = 0.5f;

    private static readonly PackedScene Scene = ResourceLoader.Load<PackedScene>("res://Scenes/Objects/lootbag.tscn");

    private CharacterItems collector;

    //In Pixeln wie alle Reichweiten. Wer weiter entfernt steht, muss erst hinlaufen
    [Export]
    public float PickupRadius { get; set; } = 150f;

    public ItemInstance ContainedItem { get; private set; }

    public static Lootbag Drop(Node parent, Vector3 globalPosition, ItemInstance item, CharacterItems collector)
    {
        var lootbag = Scene.Instantiate<Lootbag>();

        lootbag.ContainedItem = item;
        lootbag.collector     = collector;

        parent.AddChild(lootbag);

        lootbag.GlobalPosition = WorldScale.OnGround(globalPosition);

        return lootbag;
    }

    public static Lootbag FindUnderMouse(Node3D asker)
        => FindAt(asker, asker.GetViewport().GetMousePosition());

    public static Lootbag FindAt(Node3D asker, Vector2 screenPoint)
    {
        var camera = asker.GetViewport().GetCamera3D();

        if (camera is null)
            return null;

        var origin = camera.ProjectRayOrigin(screenPoint);
        var query  = PhysicsRayQueryParameters3D.Create(origin, origin + camera.ProjectRayNormal(screenPoint) * PickRayMeters, CollisionLayers.Interactive);

        query.CollideWithAreas  = true;
        query.CollideWithBodies = false;

        var hit = asker.GetWorld3D().DirectSpaceState.IntersectRay(query);

        return hit.Count == 0 ? null : hit["collider"].AsGodotObject() as Lootbag;
    }

    public bool IsInReachOf(Vector3 position)
        => WorldScale.GroundDistancePx(GlobalPosition, position) <= PickupRadius;

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
