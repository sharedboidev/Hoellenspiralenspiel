using Hoellenspiralenspiel.Enums;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

//Ein lokaler Affix verändert die Werte des Items selbst, ein globaler den Charakter
public sealed record ItemAffix(AffixType Type, CombatStat Stat, ModificationType Modification, float Value, string NameAddition, bool IsLocal);
