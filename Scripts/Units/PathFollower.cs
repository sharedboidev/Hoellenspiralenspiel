using Godot;
using Hoellenspiralenspiel.Scripts.Core.Navigation;

namespace Hoellenspiralenspiel.Scripts.Units;

//Ohne Navigationsnetz im Level läuft die Einheit wie früher in gerader Linie
public sealed class PathFollower
{
    private const double RepathIntervalSec = 0.4;
    private const float  MinTargetShiftPx  = 48f;
    private const int    StaggerSteps      = 8;

    private static int createdCount;

    private readonly NavigationAgent2D agent;
    private readonly Node2D            body;
    private readonly RepathTimer       timer;

    public PathFollower(Node2D owner)
    {
        //Der Pfad gilt für die Mitte der Kollisionsform. Vom Ursprung der Einheit aus gerechnet bliebe sie an Ecken hängen
        body  = owner.GetNodeOrNull<Node2D>(nameof(CollisionShape2D)) ?? owner;
        agent = body.GetNodeOrNull<NavigationAgent2D>(nameof(NavigationAgent2D)) ?? CreateAgent(body);

        //Der Versatz verteilt die Pfadsuche vieler Einheiten auf verschiedene Frames
        timer = new RepathTimer(RepathIntervalSec, MinTargetShiftPx, RepathIntervalSec * (createdCount++ % StaggerSteps) / StaggerSteps);
    }

    public Vector2 GetDirectionTo(Vector2 target, double deltaSec)
    {
        var position = body.GlobalPosition;
        var direct   = position.DirectionTo(target);

        if (NavigationServer2D.MapGetIterationId(agent.GetNavigationMap()) == 0)
            return direct;

        if (timer.IsDue(deltaSec, target.X, target.Y))
            agent.TargetPosition = target;

        if (agent.IsNavigationFinished())
            return direct;

        var toNextPoint = agent.GetNextPathPosition() - position;

        return toNextPoint.LengthSquared() < 1f ? direct : toNextPoint.Normalized();
    }

    public Vector2 SnapToNavigation(Vector2 point)
    {
        var map = agent.GetNavigationMap();

        return NavigationServer2D.MapGetIterationId(map) == 0 ? point : NavigationServer2D.MapGetClosestPoint(map, point);
    }

    public void Reset()
        => timer.Reset();

    private static NavigationAgent2D CreateAgent(Node2D parent)
    {
        var created = new NavigationAgent2D
        {
            Name                  = nameof(NavigationAgent2D),
            AvoidanceEnabled      = false,
            PathDesiredDistance   = 24f,
            TargetDesiredDistance = 16f,
            PathMaxDistance       = 96f
        };

        parent.AddChild(created);

        return created;
    }
}
