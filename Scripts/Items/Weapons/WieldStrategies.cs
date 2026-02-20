using System.ComponentModel;

namespace Hoellenspiralenspiel.Scripts.Items.Weapons;

public enum WieldStrategy
{
    Undefined,
    [Description("Off-Hand")]  OffHand,
    [Description("Main-Hand")] MainHand,
    [Description("One-Hand")]  OneHand,
    [Description("Two-Hand")]  TwoHand
}