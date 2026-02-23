using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Items.Consumables;

public abstract partial class ConsumableItem : BaseItem
{
    public delegate void StacksizeReduced(int newStacksize, int oldStacksize);

    [Export]
    public int StacksizeMax { get; set; } = 1;

    public override bool IsStackable => StacksizeMax > 1;

    [Export]
    public int StacksizeCurrent { get; set; } = 1;

    public             bool       IsFull   => StacksizeCurrent == StacksizeMax;
    protected override ItemType   ItemType => ItemType.Consumable;
    public event StacksizeReduced OnStacksizeReduced;

    protected abstract void ApplyEffectOfConsumption(BaseUnit consumee);

    public void GetConsumedBy(BaseUnit consumee)
    {
        if (StacksizeCurrent > 0)
        {
            ApplyEffectOfConsumption(consumee);

            StacksizeCurrent--;

            OnStacksizeReduced?.Invoke(StacksizeCurrent, StacksizeCurrent + 1);
        }
    }

    public bool TryAddToStack(int amount)
    {
        if (StacksizeCurrent + amount > StacksizeMax)
            return false;

        StacksizeCurrent += amount;

        return true;
    }

    public bool CanFit(ConsumableItem consumable)
    {
        if (IsFull || consumable.GetType().FullName != GetType().FullName)
            return false;

        return StacksizeMax >= StacksizeCurrent + consumable.StacksizeCurrent;
    }
}