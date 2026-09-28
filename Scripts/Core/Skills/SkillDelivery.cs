namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Wie ein Skill seine Wirkung ins Ziel bringt.
//Neue Werte nur am Ende anhängen: Resources speichern den Wert als Zahl
public enum SkillDelivery
{
    //Der Treffer kommt von der Waffe: Nahkampfwaffen treffen direkt, Fernkampfwaffen schießen ihr Projektil
    Weapon,

    //Ein Projektil des Skills fliegt in die gezielte Richtung
    Projectile,

    //Eine Fläche um den Wirkenden
    AreaAroundCaster,

    //Eine Fläche am gezielten Punkt
    AreaAtPoint
}
