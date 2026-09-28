using System.ComponentModel;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

//Neue Werte nur am Ende anhängen: Resources speichern die Anforderung als Zahl
public enum Requirement
{
    Strength,
    Dexterity,
    Intelligence,
    Constitution,
    Awareness,
    [Description("Level")]
    CharacterLevel
}
