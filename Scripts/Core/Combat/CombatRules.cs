namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Alle Stellschrauben des Kampfes an einer Stelle. Anteile sind Brüche, 0,5 bedeutet 50 %
public static class CombatRules
{
    //Grundwerte jeder Einheit in Prozent
    public const float BaseHitChance      = 100f;
    public const float BaseCriticalDamage = 50f;
    public const float BaseBlockReduction = 50f;

    //Crush: mehr physischer Schaden
    public const float CrushMoreDamage = 0.2f;

    //Pierce: trifft seltener und ignoriert dafür die Rüstung
    public const float PierceLessHitChance = 0.5f;

    //Slash: Bleed verursacht einen Anteil des ungeminderten Treffers über die Dauer. Nur der stärkste Bleed wirkt
    public const float BleedDamageFraction = 0.5f;
    public const float BleedDurationSec    = 4f;

    //Fire: jeder Treffer legt einen Brand obendrauf, der einen Anteil des erlittenen Schadens über die Dauer verursacht
    public const float BurnDamageFraction = 0.25f;
    public const float BurnDurationSec    = 4f;
    public const int   BurnMaxStacks      = 10;

    //Lightning: Aktionen des Ziels schlagen mit dieser Chance fehl
    public const float ShockActionFailureChance = 0.25f;
    public const float ShockDurationSec         = 4f;

    //Frost: Bewegung und Angriffe werden langsamer
    public const float ChillSlow        = 0.3f;
    public const float ChillDurationSec = 3f;

    //Abstand zwischen zwei Schadenszahlen eines Schadens über Zeit
    public const double StatusTickIntervalSec = 0.5;
}
