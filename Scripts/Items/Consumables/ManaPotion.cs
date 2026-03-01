using System.Text;
using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Items.Consumables;

public partial class ManaPotion : ConsumableItem
{
    [Export]
    public float TotalManaRestoredPercentage { get; set; } = 20f;

    public override string ItembaseName => "Mana Potion";

    protected override bool IsMagic => false;
    protected override bool IsRare  => false;

    protected override void AppendItembaseStats(StringBuilder emil)
        => emil.AppendLine($"Recovered Mana: [color=royal_blue]{TotalManaRestoredPercentage:N0}[/color]%");

    protected override void ApplyEffectOfConsumption(BaseUnit consumee)
    {
        if (consumee is not Player2D player)
            return;

        player.ManaCurrent += TotalManaRestoredPercentage / 100f * player.ManaMaximum;
    }
}