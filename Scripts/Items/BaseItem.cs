using System.Text;
using Godot;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.Items;

public abstract partial class BaseItem
        : Node2D,
          ITooltipObject
{
    [Export]
    public TextureRect Icon { get; set; }

    public             int    ItemLevel           { get; set; }
    public abstract    bool   IsStackable         { get; }
    public abstract    string ItembaseName        { get; }
    protected          string AffixedItembaseName { get; set; }
    protected          string ExceptionalName     { get; set; }
    protected abstract bool   IsMagic             { get; }

    public virtual string GetTooltipDescription()
        => string.Empty;

    public virtual string GetTooltipTitle()
    {
        var emil = new StringBuilder();
        emil.Append("[center]");

        if (!string.IsNullOrWhiteSpace(ExceptionalName))
            emil.AppendLine($"[u]{ExceptionalName}[/u]");

        if (IsMagic)
            emil.AppendLine(AffixedItembaseName);
        else
            emil.AppendLine($"{ItembaseName}");
        
        emil.Append("[/center]");

        return emil.ToString();
    }

    protected virtual void SetUniqueName()  { }
    protected virtual void SetAffixedItembaseName() { }

    public virtual void Init() { }

    public override void _Ready()
        => Icon = GetNode<TextureRect>("Icon");
}