using Godot;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Actions;

[GlobalClass]
public partial class TeleportAction : ModAction
{
    [Export]
    public ModAim Destination { get; set; } = ModAim.Target;

    //Zählt nur für die zufälligen Zielpunkte
    [Export]
    public float Radius { get; set; } = 300f;

    //So weit vor dem Ziel landet das Monster, auf der Seite, von der es kommt
    [Export]
    public float DistanceToDestination { get; set; } = 100f;

    public override void Run(ModContext context)
    {
        var owner = context.Owner;

        if (owner.IsDead || !ModAimResolver.TryResolve(context, Destination, Radius, out var aim))
            return;

        var destination = aim.CurrentPoint;
        var backOff     = destination.DirectionTo(owner.BodyCenter) * DistanceToDestination;

        owner.TeleportTo(owner.SnapToNavigation(destination + backOff));
    }
}
