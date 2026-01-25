using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Extensions;

public static class EnemyExtensions
{
    public static void MakeRare(this BaseEnemy enemy)
    {
        enemy.Movementspeed += 100f;
        enemy.LifeBase      += 250;
        enemy.Scale         *= 1.3f;
    }

    public static void MakeElite(this BaseEnemy enemy)
    {
        enemy.Movementspeed *= 1.5f;
        enemy.LifeBase      *= 2;
        enemy.Scale         *= 1.5f;
    }
}