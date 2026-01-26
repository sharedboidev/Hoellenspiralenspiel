using System;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Environment = System.Environment;

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
    }

    public new virtual void Hide() => Position = new Vector2(-900, -900);

    private void SetDisplayedDataByItem(ITooltipObject tooltipObject)
    {
        if (ObjectTitleLabel is null || ObjectDescriptionLabel is null)
            return;

        ObjectTitleLabel.Text =  $"[center][u]{tooltipObject.TooltipTitle} {DateTime.Now.Second}[/u][/center]";

        ObjectDescriptionLabel.Text = $"{Environment.NewLine}" +
                                      $"{tooltipObject.GetTooltipDescription()}{Environment.NewLine}" +
                                      $"{Environment.NewLine}";
    }

    private void SetPositionByNode(ITooltipObjectContainer container)
    {
        var xPosition = container.Position.X;// - Size.X + 10;
        var yPosition = container.Position.Y - 500;// - Size.Y + 10;

        if (xPosition <= 0) xPosition = container.Position.X + container.Size.X + 20;
        if (yPosition <= 0) yPosition = container.Position.Y + container.Size.Y + 20;

        GlobalPosition = new Vector2(xPosition, yPosition);
    }

    private void FindUIComponents()
    {
        Container              ??= GetNode<MarginContainer>("MarginContainer").GetNode<VBoxContainer>("VBoxContainer");
        ObjectDescriptionLabel ??= Container?.GetNode<RichTextLabel>("%ObjectDescription");
        ObjectTitleLabel       ??= Container?.GetNode<RichTextLabel>("%ObjectTitle");
    }
}