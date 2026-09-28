using Godot;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public sealed record EnemyRarityLook(float Scale, float XpFactor, int LootRolls, Color NameColor)
{
    public static EnemyRarityLook Normal { get; } = new(1f, 1f, 1, Colors.White);
}
