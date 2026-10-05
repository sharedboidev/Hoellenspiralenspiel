using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Stats;

//Ein Sammel-Stat gibt seinen Wert an mehrere Stats weiter, etwa "+10% to all Elemental Resistances" an alle drei Resistenzen.
//Ein globales "Adds X to Y" steht in zwei Stats: X im Stat des Affixes, Y in seinem Gegenstück mit Max
public static class CombatStatGroups
{
    private static readonly Dictionary<CombatStat, CombatStat[]> Members = new()
    {
        [CombatStat.AllElementalResistances] = [CombatStat.FireResistance, CombatStat.FrostResistance, CombatStat.LightningResistance],
        [CombatStat.AllMaximumResistances]   = [CombatStat.MaxFireResistance, CombatStat.MaxFrostResistance, CombatStat.MaxLightningResistance]
    };

    private static readonly Dictionary<CombatStat, CombatStat> RangeMaximums = new()
    {
        [CombatStat.AddedPhysicalToAttacks]  = CombatStat.AddedPhysicalToAttacksMax,
        [CombatStat.AddedFireToAttacks]      = CombatStat.AddedFireToAttacksMax,
        [CombatStat.AddedFrostToAttacks]     = CombatStat.AddedFrostToAttacksMax,
        [CombatStat.AddedLightningToAttacks] = CombatStat.AddedLightningToAttacksMax,
        [CombatStat.AddedFireToSpells]       = CombatStat.AddedFireToSpellsMax,
        [CombatStat.AddedFrostToSpells]      = CombatStat.AddedFrostToSpellsMax,
        [CombatStat.AddedLightningToSpells]  = CombatStat.AddedLightningToSpellsMax
    };

    public static bool TryGetMembers(CombatStat stat, out IReadOnlyList<CombatStat> members)
    {
        var found = Members.TryGetValue(stat, out var list);

        members = list;

        return found;
    }

    public static bool TryGetRangeMaximum(CombatStat stat, out CombatStat maximum)
        => RangeMaximums.TryGetValue(stat, out maximum);
}
