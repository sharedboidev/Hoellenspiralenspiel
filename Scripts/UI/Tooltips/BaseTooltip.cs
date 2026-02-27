using Godot;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.UI.Tooltips;

public abstract partial class BaseTooltip : PanelContainer
{
    public const string        TitlePlaceholder = "Shown Object Title";
    private      RichTextLabel ObjectDescriptionLabel { get; set; }
    private      RichTextLabel ObjectTitleLabel       { get; set; }
    public       VBoxContainer Container              { get; set; }
    public       Color         ColorRed               => new(0.86f, 0.09f, 0.09f);
    public       Color         ColorGreen             => new(0.02f, 0.7f, 0.08f);

    public virtual void Show(ITooltipObjectContainer objectContainer)
    {
        if (objectContainer is null)
            return;

        FindUIComponents();
        SetDisplayedDataByItem(objectContainer.ContainedItem);
        SetPositionByNode(objectContainer);

        Visible = true;
    }

    public new virtual void Hide() => Visible = false;

    private void SetDisplayedDataByItem(ITooltipObject tooltipObject)
    {
        if (ObjectTitleLabel is null || ObjectDescriptionLabel is null || tooltipObject is null)
            return;

        ObjectTitleLabel.Text = tooltipObject.GetTooltipTitle();
        ObjectDescriptionLabel.Text = tooltipObject.GetTooltipDescription();
    }

    private void SetPositionByNode(ITooltipObjectContainer container)
    {
        var viewportRectSize = GetViewportRect().Size;
        var xPosition    = container.TooltipAnchorPoint.X + container.Size.X; 
        var yPosition    = container.TooltipAnchorPoint.Y;

        var viewportDeltaX = xPosition + Size.X - viewportRectSize.X;
        var viewportDeltaY = yPosition + Size.Y - viewportRectSize.Y;

        if (viewportDeltaX > 0)
            xPosition = container.TooltipAnchorPoint.X- Size.X;

        if (viewportDeltaY > 0)
            yPosition = container.TooltipAnchorPoint.Y - Size.Y + container.Size.Y;
        
        Position = new Vector2(xPosition, yPosition);
    }

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