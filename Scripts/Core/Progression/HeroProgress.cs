using System;

namespace Hoellenspiralenspiel.Scripts.Core.Progression;

public sealed class HeroProgress
{
    public int Level { get; private set; } = 1;

    public long XpTotal { get; private set; }

    public int AttributePoints { get; private set; }

    public long XpFloor => XpTable.GetTotalXpNeededForLevel(Level);

    public long XpForNextLevel => XpTable.GetTotalXpNeededForLevel(Level + 1);

    public long XpIntoLevel => XpTotal - XpFloor;

    public event Action Changed;

    public event Action LeveledUp;

    //Ein Gewinn bringt höchstens ein Level. Reicht die XP für mehr, folgt der nächste Aufstieg mit dem nächsten Gewinn
    public void Gain(long experience)
    {
        if (experience <= 0)
            return;

        XpTotal += experience;

        var leveledUp = Level < XpTable.MaxLevel && XpTotal >= XpForNextLevel;

        if (leveledUp)
        {
            Level++;
            AttributePoints++;
        }

        Changed?.Invoke();

        if (leveledUp)
            LeveledUp?.Invoke();
    }

    public long LoseForDeath()
    {
        var loss = DeathPenalty.GetXpLoss(XpTotal, XpFloor, XpForNextLevel);

        if (loss <= 0)
            return 0;

        XpTotal -= loss;

        Changed?.Invoke();

        return loss;
    }

    public bool SpendAttributePoint()
    {
        if (AttributePoints <= 0)
            return false;

        AttributePoints--;

        Changed?.Invoke();

        return true;
    }

    public void Restore(int level, long xpTotal, int attributePoints)
    {
        Level           = Math.Clamp(level, 1, XpTable.MaxLevel);
        XpTotal         = Math.Max(xpTotal, XpFloor);
        AttributePoints = Math.Max(0, attributePoints);

        Changed?.Invoke();
    }
}
