using System.Text;
using Godot;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.Items;

public abstract partial class BaseItem : Node2D,
                                         ITooltipObject
{
    [Export]
    public TextureRect Icon { get; set; }

    public abstract bool   IsStackable     { get; }
    public abstract string ItembaseName    { get; }
    protected       string ExceptionalName { get; set; }

    public virtual string GetTooltipDescription() => string.Empty;

    public virtual string GetTooltipTitle()
    {
        var emil = new StringBuilder();
        emil.Append("[center]");

        if (!string.IsNullOrWhiteSpace(ExceptionalName))
            emil.AppendLine($"[u]{ExceptionalName}[/u]");

        emil.AppendLine($"{ItembaseName}");
        emil.Append("[/center]");

        return emil.ToString();
    }

    protected virtual void SetExceptionalName() { }

    public virtual void Init() { }

    public override void _Ready() => Icon = GetNode<TextureRect>("Icon");
}