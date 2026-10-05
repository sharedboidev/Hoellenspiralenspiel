namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Werte mit Base sind Prozent, alle anderen Anteile sind Brüche: 0,5 bedeutet 50 %
public static class CombatRules
{
    public const float BaseHitChance      = 100f;
    public const float BaseCriticalDamage = 50f;
    public const float BaseBlockReduction = 50f;

    //Eine Resistenz zählt bis zu ihrem Maximum. Das beginnt bei 75 %, Affixe heben es, doch mehr als 90 % zählt nie
    public const float BaseMaximumResistance = 75f;
    public const float ResistanceHardCap     = 90f;

    //Zusätzliche Minderung physischen Schadens nach der Rüstung, in Prozent
    public const float MaxPhysicalDamageReduction = 90f;

    public const float CrushMoreDamage = 0.2f;

    public const float PierceLessHitChance = 0.5f;

    public const float BleedDamageFraction = 0.5f;
    public const float BleedDurationSec    = 4f;

    public const float BurnDamageFraction = 0.25f;
    public const float BurnDurationSec    = 4f;
    public const int   BurnMaxStacks      = 10;

    public const float ShockActionFailureChance = 0.25f;
    public const float ShockDurationSec         = 4f;

    public const float ChillSlow        = 0.3f;
    public const float ChillDurationSec = 3f;

    public const double StatusTickIntervalSec = 0.5;

    public const float MinAttacksPerSecond = 0.1f;

    public const double MinSpellCooldownSec = 0.1;
}
