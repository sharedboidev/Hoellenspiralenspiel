using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

//Ein lokaler Affix verändert die Werte des Items selbst, ein globaler den Charakter.
//Bei "Adds X to Y" ist Value das X und ValueTo das Y, sonst ist ValueTo 0. Ein hybrider Affix trägt in Hybrid seinen zweiten Stat
public sealed record ItemAffix(AffixType        Type,
                               CombatStat       Stat,
                               ModificationType Modification,
                               float            Value,
                               string           NameAddition,
                               bool             IsLocal,
                               float            ValueTo = 0f,
                               ItemAffixLine    Hybrid  = null)
{
    public bool HasRange => ValueTo > 0f;
}

public sealed record ItemAffixLine(CombatStat Stat, ModificationType Modification, float Value, bool IsLocal);
