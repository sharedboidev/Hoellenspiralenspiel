namespace Hoellenspiralenspiel.Scripts.Core.Combat;

//Eine ATTACK: ein Skill, der mit dem Schaden der Waffe skaliert.
//DealtAs wandelt den Schaden in eine andere Schadensart um, ohne Angabe gilt die Schadensart der Waffe
public sealed record AttackDefinition(string Name, float WeaponDamagePercent, DamageType? DealtAs = null)
{
    //Der Standardangriff verursacht 100 % Waffenschaden
    public static AttackDefinition Standard { get; } = new("Attack", 100f);
}
