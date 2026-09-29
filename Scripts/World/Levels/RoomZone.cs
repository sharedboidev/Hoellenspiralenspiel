using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Die Fläche eines Raums, als Quader unter diesem Knoten. Wer außerhalb steht, sieht nicht hinein:
//Die Mauern des Raums bleiben dann zu, und was sich darin bewegt, bleibt verborgen
public partial class RoomZone : Area3D
{
    private static readonly List<RoomZone> Zones = new();

    private static int lastId;

    public static int Version { get; private set; }

    public int Id { get; private set; }

    public static RoomZone Create(Vector2 sizeMeters, float heightMeters)
    {
        var zone = new RoomZone { Name = nameof(RoomZone) };

        zone.AddChild(new CollisionShape3D
        {
            Name      = nameof(CollisionShape3D),
            Shape     = new BoxShape3D { Size = new Vector3(sizeMeters.X, heightMeters, sizeMeters.Y) },
            Position  = Vector3.Up * (heightMeters / 2f),
            DebugFill = false
        });

        return zone;
    }

    public static RoomZone FindAt(Vector3 point)
    {
        foreach (var zone in Zones)
        {
            if (zone.Contains(point))
                return zone;
        }

        return null;
    }

    public static int GetIdAt(Vector3 point)
        => FindAt(point)?.Id ?? WallOpeningRule.NoRoom;

    public override void _EnterTree()
    {
        Id               = ++lastId;
        CollisionLayer   = 0;
        CollisionMask    = 0;
        Monitoring       = false;
        Monitorable      = false;
        InputRayPickable = false;

        Zones.Add(this);

        Version++;
    }

    public override void _ExitTree()
    {
        Zones.Remove(this);

        Version++;
    }

    //Die Höhe zählt nicht, ein Raum reicht vom Boden bis über die Köpfe
    public bool Contains(Vector3 point)
    {
        foreach (var child in GetChildren())
        {
            if (child is not CollisionShape3D { Shape: BoxShape3D box } shape)
                continue;

            var local = shape.GlobalTransform.AffineInverse() * point;

            if (Mathf.Abs(local.X) <= box.Size.X / 2f && Mathf.Abs(local.Z) <= box.Size.Z / 2f)
                return true;
        }

        return false;
    }
}
