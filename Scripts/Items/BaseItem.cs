using System.Text;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.Items;

public abstract partial class BaseItem
        : Node2D,
          ITooltipObject
{
    [Export]
    public TextureRect Icon { get; set; }

    public             int      ItemLevel           { get; set; }
    public abstract    bool     IsStackable         { get; }
    public abstract    string   ItembaseName        { get; }
    protected          string   AffixedItembaseName { get; set; }
    protected          string   ExceptionalName     { get; set; }
    protected abstract bool     IsMagic             { get; }
    protected abstract bool     IsRare              { get; }
    protected abstract ItemType ItemType            { get; }

    public virtual string GetTooltipDescription()
        => string.Empty;

    public virtual string GetTooltipTitle()
    {
        var emil = new StringBuilder();
        emil.Append("[center]");

        if (IsMagic)
            emil.AppendLine($"[color=dodger_blue]{AffixedItembaseName}[/color]");
        else
        {
            if (IsRare)
            {
                emil.AppendLine($"[color=yellow]{ExceptionalName}[/color]");
                emil.AppendLine($"[color=yellow]{ItembaseName}[/color]");
            }
            else
                emil.AppendLine($"{ItembaseName}");
        }

        emil.Append("[/center]");

        return emil.ToString();
    }

    protected virtual void SetExceptionalName() { }

    protected virtual void SetAffixedItembaseName() { }

    public virtual void Init() { }

    public override void _Ready()
        => Icon = GetNode<TextureRect>("Icon");
}