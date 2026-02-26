using System.Text;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Items.Consumables;

public partial class HealthPotion : ConsumableItem
{
    [Export]
    public float TotalHealthRestoredPercentage { get; set; } = 20;

    public override string ItembaseName => "Health Potion";

    protected override bool IsMagic => false;
    protected override bool IsRare  => false;

    protected override void AppendItembaseStats(StringBuilder emil)
        => emil.AppendLine($"Recovered Life: [color=lime_green]{TotalHealthRestoredPercentage:N0}[/color]%");

    protected override void ApplyEffectOfConsumption(BaseUnit consumee)
    {
        var healedAmount = consumee.LifeMaximum * TotalHealthRestoredPercentage / 100f;
        consumee.LifeCurrent += (int)healedAmount;

        consumee.InstatiateFloatingCombatText(new HitResult(healedAmount, HitType.Normal, LifeModificationMode.Heal), consumee.GetParent(), new Vector2(0, -128));
    }
}