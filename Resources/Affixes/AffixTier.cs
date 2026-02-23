using Godot;

namespace Hoellenspiralenspiel.Resources.Affixes;

[GlobalClass]
public partial class AffixTier : Resource
{
    [Export(PropertyHint.Range, "1,10,")]
    public int Tier { get; set; }

    [Export]
    public int MinItemLevelToAppearOn { get; set; }

    [Export(PropertyHint.Range, "0,99999,1")]
    public int Weight { get; set; }

    [Export]
    public float MinValue { get; set; }

    [Export]
    public float MaxValue { get; set; }

    [Export]
    public string ItemnameAddition { get; set; }
}