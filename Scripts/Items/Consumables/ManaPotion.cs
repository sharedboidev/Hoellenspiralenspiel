using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Items.Consumables;

public partial class ManaPotion : ConsumableItem
{
    [Export]
    public float TotalManaRestoredPercentage { get; set; } = 20f;

    public override string ItembaseName => "Mana Potion";

    public override string GetTooltipDescription() => $"Restores {TotalManaRestoredPercentage:N0}% of your maximum Mana.";

    protected override void ApplyEffectOfConsumption(BaseUnit consumee)
    {
        if (consumee is not Player2D player)
            return;

        player.ManaCurrent += TotalManaRestoredPercentage / 100f * player.ManaMaximum;
    }
}