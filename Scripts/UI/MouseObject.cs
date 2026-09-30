using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Items;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class MouseObject : PanelContainer
{
    private static readonly Vector2 CursorOffset = new(5, 5);

    private TextureRect icon;

    private TextureRect Icon => icon ??= GetNode<TextureRect>("%Icon");

    [Export]
    public float CellPx { get; set; } = 28;

    public override void _Process(double delta)
    {
        if (Visible)
            GlobalPosition = GetGlobalMousePosition() + CursorOffset;
    }

    public void ShowItem(ItemInstance item)
    {
        Icon.Texture = ItemLibrary.GetIcon(item);

        if (item is not null)
        {
            CustomMinimumSize = new Vector2(item.Definition.Width, item.Definition.Height) * CellPx;

            //Von selbst wird ein Control nur größer, nie kleiner
            ResetSize();
        }

        SetVisible(item is not null);
    }
}
