using System;
using Godot;
using Hoellenspiralenspiel.Resources.Enemies;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public partial class SpawnMarker3D : Marker3D
{
    private const float SpacingMeters = 0.64f;

    [Export]
    public EnemyResource Enemy { get; set; }

    //Die Gegner-Resource verweist auf die 2D-Szene, die 3D-Szene steht deshalb im Vergleich am Marker
    [Export]
    public PackedScene Scene { get; set; }

    [Export]
    public int AmountToSpawn { get; set; } = 1;

    [Export]
    public int LevelOffset { get; set; }

    public Vector3 GetSpawnPosition(int index)
    {
        var perRow = (int)Math.Sqrt(AmountToSpawn) + 1;
        var corner = -SpacingMeters * perRow / 4f;

        return GlobalPosition + new Vector3(corner + index / perRow * SpacingMeters, 0, corner + index % perRow * SpacingMeters);
    }
}
