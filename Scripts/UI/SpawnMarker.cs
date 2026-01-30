using System;
using Godot;
using Godot.Collections;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class SpawnMarker : PanelContainer
{
    private Dictionary<int, Vector2> spawnLocations = new();

    [Export]
    public PackedScene EnemyToSpawn { get; set; }

    [Export]
    public int AmountToSpawn { get; set; }

    public Vector2 GetSpawnlocationFor(int i) => spawnLocations[i];

    public override void _Ready()
    {
        SetVisible(false);

        FillSpawnMap();
    }

    private void FillSpawnMap()
    {
        var dimension     = (int)Math.Sqrt(AmountToSpawn) + 1;
        var assumedSizePx = 64;
        var offset        = new Vector2(assumedSizePx * (float)dimension / 2, assumedSizePx * (float)dimension / 2);
        var totalCount = 0;

        for (var i = 0; i < dimension; i++)
        {
            for (var j = 0; j < dimension; j++)
            {
                if(totalCount >= AmountToSpawn)
                    break;

                var newPosition = GlobalPosition + offset;
                newPosition += new Vector2(i * assumedSizePx, j * assumedSizePx);

                if (spawnLocations.ContainsKey(totalCount))
                    continue;

                spawnLocations.Add(totalCount, newPosition);
                totalCount++;
            }
        }
    }
}