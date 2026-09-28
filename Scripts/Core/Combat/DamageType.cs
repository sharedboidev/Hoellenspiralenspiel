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

    //Der Stat des Verteidigers, der Schaden dieser Art mindert
    public static CombatStat GetMitigatingStat(this DamageType damageType)
        => damageType switch
        {
            DamageType.Fire      => CombatStat.FireResistance,
            DamageType.Frost     => CombatStat.FrostResistance,
            DamageType.Lightning => CombatStat.LightningResistance,
            _                    => CombatStat.Armor
        };

    //Der Stat des Angreifers, der Schaden dieser Art verstärkt
    public static CombatStat GetScalingStat(this DamageType damageType)
        => damageType.IsPhysical() ? CombatStat.PhysicalDamage : CombatStat.ElementalDamage;
}
