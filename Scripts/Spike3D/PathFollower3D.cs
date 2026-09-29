using Godot;
using Hoellenspiralenspiel.Scripts.Core.Navigation;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

//Ohne Navigationsnetz im Level läuft die Einheit in gerader Linie
public sealed class PathFollower3D
{
    private const double RepathIntervalSec = 0.4;
    private const float  MinTargetShiftPx  = 48f;
    private const int    StaggerSteps      = 8;

    private static int createdCount;

    private readonly NavigationAgent3D agent;
    private readonly Node3D            body;
    private readonly RepathTimer       timer;

    public PathFollower3D(Node3D owner)
    {
        body  = owner;
        agent = owner.GetNodeOrNull<NavigationAgent3D>(nameof(NavigationAgent3D)) ?? CreateAgent(owner);
        timer = new RepathTimer(RepathIntervalSec, MinTargetShiftPx, RepathIntervalSec * (createdCount++ % StaggerSteps) / StaggerSteps);
    }

    public Vector3 GetDirectionTo(Vector3 target, double deltaSec)
    {
        var position = body.GlobalPosition;
        var direct   = WorldScale.OnGround(target - position).Normalized();

        if (NavigationServer3D.MapGetIterationId(agent.GetNavigationMap()) == 0)
            return direct;

        if (timer.IsDue(deltaSec, WorldScale.ToPx(target.X), WorldScale.ToPx(target.Z)))
            agent.TargetPosition = target;

        if (agent.IsNavigationFinished())
            return direct;

        var toNextPoint = WorldScale.OnGround(agent.GetNextPathPosition() - position);

        return toNextPoint.LengthSquared() < 0.0001f ? direct : toNextPoint.Normalized();
    }

    public Vector3 SnapToNavigation(Vector3 point)
    {
        var map = agent.GetNavigationMap();

        if (NavigationServer3D.MapGetIterationId(map) == 0)
            return point;

        return WorldScale.OnGround(NavigationServer3D.MapGetClosestPoint(map, point));
    }

    public void Reset()
        => timer.Reset();

    //Das Netz liegt eine Zellenhöhe über dem Boden, die Abstände müssen diesen Versatz überbrücken
    private static NavigationAgent3D CreateAgent(Node3D parent)
    {
        var created = new NavigationAgent3D
        {
            Name                  = nameof(NavigationAgent3D),
            AvoidanceEnabled      = false,
            PathDesiredDistance   = 0.6f,
            TargetDesiredDistance = 0.5f,
            PathMaxDistance       = 2f
        };

        parent.AddChild(created);

        return created;
    }
}
