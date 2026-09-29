using Godot;
using Hoellenspiralenspiel.Resources.Enemies;

namespace Hoellenspiralenspiel.Resources.Levels;

[GlobalClass]
public partial class EnemyPoolEntry : Resource
{
    [Export]
    public EnemyResource Enemy { get; set; }

    [Export]
    public float Weight { get; set; } = 1f;

    [Export]
    public int MinAreaLevel { get; set; } = 1;

    //Gilt für Gruppen in Gängen. Marker in Räumen nennen ihre Zahl selbst
    [Export]
    public int MinGroupSize { get; set; } = 2;

    [Export]
    public int MaxGroupSize { get; set; } = 4;
}
