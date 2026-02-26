using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Objects;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class MouseObject : PanelContainer,
                                   ITooltipObjectContainer
{
    private PackedScene    lootbagScene = ResourceLoader.Load<PackedScene>("res://Scenes/Objects/lootbag.tscn");
    private TextureRect    Icon               => GetNode<TextureRect>("%Icon");
    public  bool           HasItem            => ContainedItem is not null;
    public  ITooltipObject ContainedItem      { get; set; }
    public  Vector2        TooltipAnchorPoint => GetGlobalMousePosition() + new Vector2(5, 5);

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        GlobalPosition = TooltipAnchorPoint;
    }

    public void Show(BaseItem withdrawnItem)
    {
        ContainedItem = withdrawnItem;
        Icon.Texture  = withdrawnItem.Icon.Texture;

        SetVisible(true);
    }

    public BaseItem RetrieveItem()
    {
        SetVisible(false);

        var returningItem = ContainedItem;
        ContainedItem = null;
        Icon.Texture  = null;

        return (BaseItem)returningItem;
    }

    public void DropItem()
    {
        var itemToDrop = RetrieveItem();

        if (itemToDrop is null)
            return;

        var globalMousePosition = GetGlobalMousePosition();
        var playerPosition      = GetTree().CurrentScene.GetNode<Player2D>("%Player 2D").GlobalPosition;
        var clickDirection      = (globalMousePosition - playerPosition).Normalized();
        var dropAtPosition      = playerPosition + clickDirection * 30;

        InstantiateLootbag(dropAtPosition, itemToDrop);

        GD.Print($"{itemToDrop?.Name ?? "Nothing"} dropped by Player.");
    }

    private void InstantiateLootbag(Vector2 atPosition, BaseItem loot)
    {
        var lootbagInstance = lootbagScene.Instantiate<Lootbag>();
        lootbagInstance.GlobalPosition =  atPosition;
        lootbagInstance.ContainedItem  =  loot;
        lootbagInstance.LootClicked    += LootbagInstanceOnLootClicked;

        GetTree().CurrentScene.GetNode<Node2D>("Environment").AddChild(lootbagInstance);
    }

    private void LootbagInstanceOnLootClicked(Lootbag sender, BaseItem lootedItem)
    {
        GD.Print($"{lootedItem?.Name ?? "Nothing"} looted.");

        GetParent<Character.Inventory>().SetItem(lootedItem);

        sender?.QueueFree();
    }
}