using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Scripts.Core.Hud;

namespace Hoellenspiralenspiel.Scripts.UI.Tooltips;

public abstract partial class BaseTooltip : PanelContainer
{
    private RichTextLabel ObjectDescriptionLabel { get; set; }
    private RichTextLabel ObjectTitleLabel       { get; set; }
    public  VBoxContainer Container              { get; set; }

    public virtual void Show(ITooltipObjectContainer objectContainer)
    {
        if (objectContainer is null)
            return;

        Fill(objectContainer.ContainedItem);
        PlaceBy(objectContainer);

        Visible = true;
    }

    public new virtual void Hide()
        => Visible = false;

    protected void Fill(ITooltipObject tooltipObject)
    {
        FindUIComponents();

        if (ObjectTitleLabel is null || ObjectDescriptionLabel is null || tooltipObject is null)
            return;

        ObjectTitleLabel.CustomMinimumSize       = Vector2.Zero;
        ObjectTitleLabel.Text                    = tooltipObject.GetTooltipTitle();
        ObjectDescriptionLabel.CustomMinimumSize = Vector2.Zero;
        ObjectDescriptionLabel.Text              = tooltipObject.GetTooltipDescription();

        ResetSize();
    }

    protected void PlaceBy(ITooltipObjectContainer container)
    {
        var screen = GetViewportRect().Size;
        var placed = TooltipPlacement.Place(ToBox(container), Size.X, Size.Y, screen.X, screen.Y);

        GlobalPosition = new Vector2(placed.X, placed.Y);
    }

    protected static ScreenBox ToBox(ITooltipObjectContainer container)
        => new(container.TooltipAnchorPoint.X, container.TooltipAnchorPoint.Y, container.Size.X, container.Size.Y);

    private void FindUIComponents()
    {
        Container              ??= GetNode<MarginContainer>("MarginContainer").GetNode<VBoxContainer>("VBoxContainer");
        ObjectDescriptionLabel ??= Container?.GetNode<RichTextLabel>("%ObjectDescription");
        ObjectTitleLabel       ??= Container?.GetNode<RichTextLabel>("%ObjectTitle");

        if (ObjectDescriptionLabel != null)
        {
            ObjectDescriptionLabel.FitContent        = true;
            ObjectDescriptionLabel.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        }

        if (Container != null)
            Container.SizeFlagsVertical = SizeFlags.ShrinkBegin;
    }
}