using Hoellenspiralenspiel.Resources.Enemies;

namespace Hoellenspiralenspiel.Interfaces;

public interface ISpawnDefinition
{
    EnemyResource Enemy { get; set; }
    int AmountToSpawn { get; set; }
    int LevelOffset { get; set; }
    float ScatterRadius { get; set; }
    float MinGap { get; set; }
}