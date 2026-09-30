using Godot;
using Hoellenspiralenspiel.Interfaces;

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

        FindUIComponents();
        SetDisplayedDataByItem(objectContainer.ContainedItem);
        SetPositionByContainer(objectContainer);

        Visible = true;
    }

    public new virtual void Hide()
        => Visible = false;

    private void SetDisplayedDataByItem(ITooltipObject tooltipObject)
    {
        if (ObjectTitleLabel is null || ObjectDescriptionLabel is null || tooltipObject is null)
            return;

        ObjectTitleLabel.CustomMinimumSize       = Vector2.Zero;
        ObjectTitleLabel.Text                    = tooltipObject.GetTooltipTitle();
        ObjectDescriptionLabel.CustomMinimumSize = Vector2.Zero;
        ObjectDescriptionLabel.Text              = tooltipObject.GetTooltipDescription();

        ResetSize();
    }

    //Über dem Element, ist oben kein Platz, darunter. Er bleibt an allen vier Rändern im Bild, bei jeder Größe des Fensters
    private void SetPositionByContainer(ITooltipObjectContainer container)
    {
        var screen = GetViewportRect().Size;
        var anchor = container.TooltipAnchorPoint;
        var x      = anchor.X + container.Size.X / 2 - Size.X / 2;
        var y      = anchor.Y - Size.Y;

        if (y < 0)
            y = anchor.Y + container.Size.Y;

        x = Mathf.Clamp(x, 0, Mathf.Max(0, screen.X - Size.X));
        y = Mathf.Clamp(y, 0, Mathf.Max(0, screen.Y - Size.Y));

        GlobalPosition = new Vector2(x, y);
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