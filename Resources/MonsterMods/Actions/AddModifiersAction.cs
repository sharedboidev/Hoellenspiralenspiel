using Godot;
using Godot.Collections;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Actions;

[GlobalClass]
public partial class AddModifiersAction : ModAction
{
    [Export]
    public Array<StatModifierResource> Modifiers { get; set; } = new();

    //0 bedeutet: bleibt bis zum Tod
    [Export]
    public double DurationSec { get; set; }

    public override void Run(ModContext context)
        => context.Owner.AddModifiers(Modifiers, DurationSec);
}
