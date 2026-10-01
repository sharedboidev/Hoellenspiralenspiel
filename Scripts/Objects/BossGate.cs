using Godot;
using Hoellenspiralenspiel.Scripts.Skills;

namespace Hoellenspiralenspiel.Scripts.Objects;

//Ein Gitter in einer Tür des Boss-Raums. Offen hängt es unsichtbar über der Tür und hat keine Kollision,
//geschlossen fällt es herab und sperrt wie eine Mauer. Die Wegfindung kennt es nicht, sie wird beim Aufbau der Ebene gebacken
public partial class BossGate : StaticBody3D
{
    private const string BarsName     = "Bars";
    private const float  RaisedMeters = 3.2f;
    private const double DropSec      = 0.35;
    private const double LiftSec      = 0.6;

    private Node3D           bars;
    private Tween            motion;
    private CollisionShape3D shape;

    public bool IsClosed { get; private set; }

    public override void _Ready()
    {
        CollisionLayer   = CollisionLayers.Walls;
        CollisionMask    = 0;
        InputRayPickable = false;

        shape = GetNode<CollisionShape3D>(nameof(CollisionShape3D));
        bars  = GetNode<Node3D>(BarsName);

        shape.Disabled = true;
        bars.Visible   = false;
        bars.Position  = Vector3.Up * RaisedMeters;
    }

    public void SetClosed(bool closed)
    {
        if (closed == IsClosed)
            return;

        IsClosed = closed;

        shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, !closed);

        motion?.Kill();

        motion = CreateTween();

        if (closed)
        {
            bars.Visible = true;

            motion.TweenProperty(bars, "position", Vector3.Zero, DropSec).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        }
        else
        {
            motion.TweenProperty(bars, "position", Vector3.Up * RaisedMeters, LiftSec).SetEase(Tween.EaseType.Out);
            motion.TweenCallback(Callable.From(() => bars.Visible = false));
        }
    }
}
