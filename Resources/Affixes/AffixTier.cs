using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Affixes;

[GlobalClass]
public partial class AffixTier : Resource
{
    [Export(PropertyHint.Range, "1,20,")]
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

    //Nur für "Adds X to Y": aus dieser Spanne kommt das Y, aus MinValue bis MaxValue das X
    [Export]
    public float MinValueTo { get; set; }

    [Export]
    public float MaxValueTo { get; set; }

    public AffixTierDefinition ToDefinition()
        => new(Tier, MinItemLevelToAppearOn, Weight, MinValue, MaxValue, ItemnameAddition, MinValueTo, MaxValueTo);
}
