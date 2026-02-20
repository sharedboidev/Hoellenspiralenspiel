using System.ComponentModel;

namespace Hoellenspiralenspiel.Scripts.Items.Weapons;

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