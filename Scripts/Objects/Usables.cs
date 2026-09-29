using Godot;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.Objects;

public static class Usables
{
    private const float PickRayMeters = 300f;
    private const int   MaxSkipped    = 8;

    public static IUsable FindUnderMouse(Node3D asker, bool skipsLootbags = false)
        => FindAt(asker, asker.GetViewport().GetMousePosition(), skipsLootbags);

    //Was hinter Mauerwerk liegt, durch das man nicht sieht, lässt sich auch nicht anklicken
    public static IUsable FindAt(Node3D asker, Vector2 screenPoint, bool skipsLootbags = false)
    {
        var camera = asker.GetViewport().GetCamera3D();

        if (camera is null)
            return null;

        var space   = asker.GetWorld3D().DirectSpaceState;
        var origin  = camera.ProjectRayOrigin(screenPoint);
        var end     = origin + camera.ProjectRayNormal(screenPoint) * PickRayMeters;
        var skipped = new Godot.Collections.Array<Rid>();

        for (var attempt = 0; attempt <= MaxSkipped; attempt++)
        {
            var query = PhysicsRayQueryParameters3D.Create(origin, end, CollisionLayers.Interactive, skipped);

            query.CollideWithAreas  = true;
            query.CollideWithBodies = false;

            var hit = space.IntersectRay(query);

            if (hit.Count == 0)
                return null;

            if (hit["collider"].AsGodotObject() is IUsable usable && !(skipsLootbags && usable is Lootbag))
                return WallFade.IsHidden(space, origin, hit["position"].AsVector3()) ? null : usable;

            skipped.Add(hit["rid"].AsRid());
        }

        return null;
    }
}
