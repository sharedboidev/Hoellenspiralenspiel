using Godot;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Affixes.Suffixes;

[GlobalClass]
public partial class Suffix : Affix
{
    public override AffixType Type => AffixType.Suffix;
}
