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
    ChargedShot,

    //Ein Wirbel mit der Nahkampfwaffe, der läuft, solange die Taste gehalten wird, und in Ticks jeden im Kreis trifft. Nur mit einer Nahkampfwaffe
    WeaponWhirl,

    //Ein Blitz ohne Flugzeit vom Wirkenden zum Ziel, der von dort auf weitere Gegner springt
    ChainBeam,

    //Ein Nebel unter der Maus, der eine Weile liegt, dabei wächst und jedem Gegner darin immer wieder einen Effekt gibt. Er macht keinen Schaden
    LingeringCloud
}
