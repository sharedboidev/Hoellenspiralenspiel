using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Items.Consumables;

public partial class HealthPotion : ConsumableItem
{
    [Export]
    public float TotalHealthRestoredPercentage { get; set; } = 20;

    public override string GetTooltipDescription() => $"Restores {TotalHealthRestoredPercentage:N}% of your maximum Life.";

    protected override void ApplyEffectOfConsumption(BaseUnit consumee)
    {
        var healedAmount = consumee.LifeMaximum * 0.2;
        consumee.LifeCurrent += (int)healedAmount;
    }
}