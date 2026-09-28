using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Items;

[GlobalClass]
public partial class ConsumableBaseResource : ItemBaseResource
{
    [ExportGroup("Consumable")]
    [Export]
    public int MaxStackSize { get; set; } = 5;

    [Export]
    public ConsumableEffectKind Effect { get; set; }

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float Percent { get; set; } = 20f;

    protected override ItemDefinition CreateBaseDefinition()
        => ItemDefinition.ForConsumable(Id, DisplayName, new ConsumableEffect(Effect, Percent), MaxStackSize);
}
