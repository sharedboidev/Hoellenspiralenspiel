namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Neue Werte nur am Ende anhängen: Resources speichern den Wert als Zahl
public enum SkillDelivery
{
    Weapon,
    Projectile,
    AreaAroundCaster,
    AreaAtPoint,
    WeaponSweep,

    //Ein Schlag auf das Ziel wie Weapon, aber nur mit einer Nahkampfwaffe. Weapon schießt mit einer Fernkampfwaffe
    MeleeStrike,

    //Ein Schuss in den Himmel, nach dem Pfeile auf das Zielgebiet fallen. Nur mit einem Bogen
    ArrowRain,

    //Ein Schuss mit dem Projektil der Waffe, der mit gehaltener Taste lädt und beim Loslassen Richtung Maus geht. Nur mit einem Bogen
    ChargedShot
}
