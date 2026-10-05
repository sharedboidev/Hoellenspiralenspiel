using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Enemies;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class SpawnMarker : Marker3D, ISpawnDefinition
{
    [Export]
    public EnemyResource Enemy { get; set; }

    [Export]
    public int AmountToSpawn { get; set; } = 1;

    [Export]
    public int LevelOffset { get; set; }

    //In Pixeln wie alle Reichweiten. Passt die Gruppe nicht hinein, wächst der Umkreis
    [Export]
    public float ScatterRadius { get; set; } = 400f;

    //So viel Luft bleibt mindestens zwischen zwei Körpern
    [Export]
    public float MinGap { get; set; } = 100f;
}
