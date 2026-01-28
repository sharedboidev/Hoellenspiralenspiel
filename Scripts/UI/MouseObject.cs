using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class MouseObject : PanelContainer,
                                   ITooltipObjectContainer
{
    private TextureRect    Icon               => GetNode<TextureRect>("%Icon");
    public  ITooltipObject ContainedItem      { get; set; }
    public  Vector2        TooltipAnchorPoint => GetGlobalMousePosition() + new Vector2(5,5);
    public  bool           HasItem            => ContainedItem is not null;

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
}