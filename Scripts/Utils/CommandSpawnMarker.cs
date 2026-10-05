using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Enemies;

namespace Hoellenspiralenspiel.Scripts.Utils;

public record CommandSpawnMarker : ISpawnDefinition
{
    public EnemyResource Enemy { get; set; }
    public int AmountToSpawn { get; set; }
    public int LevelOffset { get; set; }
    public float ScatterRadius { get; set; } = 400f;
    public float MinGap { get; set; } = 100f;
}