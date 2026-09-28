using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Affixes.Prefixes;

[GlobalClass]
public partial class Prefix : Affix
{
    public override AffixType Type => AffixType.Prefix;
}
