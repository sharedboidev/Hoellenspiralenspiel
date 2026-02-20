using System.ComponentModel;

namespace Hoellenspiralenspiel.Scripts.Items.Weapons;

public enum WieldStrategy
{
    Undefined,
    [Description("One-Hand")]
    OneHand,
    [Description("Off-Hand")]
    OffHand,
    [Description("Two-Hand")]
    TwoHand
}