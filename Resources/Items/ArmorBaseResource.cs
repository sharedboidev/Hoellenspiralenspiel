using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Items;

//Ein Schild ist eine Rüstung im Platz Offhand
[GlobalClass]
public partial class ArmorBaseResource : EquippableBaseResource
{
    [ExportGroup("Armor")]
    [Export]
    public ItemSlot Slot { get; set; } = ItemSlot.Torso;

    [Export]
    public int Armor { get; set; }

    protected override ItemDefinition CreateBaseDefinition()
        => ItemDefinition.ForArmor(Id, DisplayName, Slot, Armor) with { Guard = Guard };
}
