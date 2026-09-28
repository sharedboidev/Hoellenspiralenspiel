using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Neue Werte nur am Ende anhängen: Szenen speichern die Schadensart als Zahl
public enum DamageType
{
    Crush,
    Pierce,
    Slash,
    Fire,
    Frost,
    Lightning
}

public static class DamageTypeExtensions
{
    public static bool IsPhysical(this DamageType damageType)
        => damageType is DamageType.Crush or DamageType.Pierce or DamageType.Slash;

    public static bool IsElemental(this DamageType damageType)
        => !damageType.IsPhysical();

    public static CombatStat GetMitigatingStat(this DamageType damageType)
        => damageType switch
        {
            DamageType.Fire      => CombatStat.FireResistance,
            DamageType.Frost     => CombatStat.FrostResistance,
            DamageType.Lightning => CombatStat.LightningResistance,
            _                    => CombatStat.Armor
        };

    public static CombatStat GetScalingStat(this DamageType damageType)
        => damageType.IsPhysical() ? CombatStat.PhysicalDamage : CombatStat.ElementalDamage;

    public static float GetDamageFactor(this DamageType damageType)
        => damageType == DamageType.Crush ? 1f + CombatRules.CrushMoreDamage : 1f;

    public static float GetHitChanceFactor(this DamageType damageType)
        => damageType == DamageType.Pierce ? 1f - CombatRules.PierceLessHitChance : 1f;
}
