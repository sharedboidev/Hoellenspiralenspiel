using System;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

public sealed class Purse
{
    public int Amount { get; private set; }

    public event Action Changed;

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        Amount = (int)Math.Min(int.MaxValue, (long)Amount + amount);

        Changed?.Invoke();
    }

    public bool CanAfford(int price)
        => price >= 0 && price <= Amount;

    public bool TrySpend(int price)
    {
        if (!CanAfford(price))
            return false;

        if (price == 0)
            return true;

        Amount -= price;

        Changed?.Invoke();

        return true;
    }

    public int TakeAll()
    {
        var taken = Amount;

        if (taken == 0)
            return 0;

        Amount = 0;

        Changed?.Invoke();

        return taken;
    }

    public void Restore(int amount)
    {
        Amount = Math.Max(0, amount);

        Changed?.Invoke();
    }

    //Bewegt höchstens, was die Quelle hat und das Ziel noch fasst
    public static int Move(Purse from, Purse to, int amount)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var moved = Math.Min(Math.Min(Math.Max(0, amount), from.Amount), int.MaxValue - to.Amount);

        if (moved <= 0 || from == to)
            return 0;

        from.TrySpend(moved);
        to.Add(moved);

        return moved;
    }
}
