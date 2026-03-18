using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Extensions;

public static class EnemyExtensions
{
    public static void MakeRare(this BaseEnemy enemy)
    {
        enemy.Movementspeed += 100f;
        enemy.StrengthBase  += 25;
        enemy.Scale         *= 1.3f;
        enemy.XpGranted     =  (int)(enemy.XpGranted * 1.3f);
        enemy.LifeCurrent   =  enemy.LifeMaximum;
    }

    public static void MakeElite(this BaseEnemy enemy)
    {
        enemy.Movementspeed *= 1.5f;
        enemy.StrengthBase  += 25;
        enemy.Scale         *= 1.5f;
        enemy.XpGranted     =  (int)(enemy.XpGranted * 1.5f);
        enemy.LifeCurrent   =  enemy.LifeMaximum;
    }
}