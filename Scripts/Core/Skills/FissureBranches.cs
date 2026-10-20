using System;
using System.Collections.Generic;

namespace Hoellenspiralenspiel.Scripts.Core.Skills;

//Proliferate bei den Lavarissen eines Schlags: Jeder Hauptriss gibt höchstens so viele Nebenrisse, wie der Held Proliferate hat,
//je einen unter den ersten Gegnern, die er trifft. Jeder Gegner gibt je Schlag höchstens einen Nebenriss, Nebenrisse geben keine weiteren
public sealed class FissureBranches
{
    private readonly HashSet<ulong>        claimedUnits    = new();
    private readonly Dictionary<int, int> branchesByCrack = new();

    public FissureBranches(int perMainCrack)
    {
        PerMainCrack = Math.Max(0, perMainCrack);
    }

    public int PerMainCrack { get; }

    public int Count => claimedUnits.Count;

    //Wahr, wenn der Gegner jetzt einen Nebenriss bekommt. Ein Hauptriss, der sein Kontingent verbraucht hat, sperrt den Gegner nicht für die anderen
    public bool TryClaim(int mainCrack, ulong unitId)
    {
        if (mainCrack < 0 || PerMainCrack == 0 || claimedUnits.Contains(unitId))
            return false;

        var used = branchesByCrack.GetValueOrDefault(mainCrack);

        if (used >= PerMainCrack)
            return false;

        branchesByCrack[mainCrack] = used + 1;
        claimedUnits.Add(unitId);

        return true;
    }
}
