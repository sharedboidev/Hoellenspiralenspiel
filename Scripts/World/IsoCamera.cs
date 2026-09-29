using Godot;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class IsoCamera : Camera3D
{
    private Vector3 offset;
    private float   orthogonalSize;

    [Export]
    public Node3D Target { get; set; }

    [Export]
    public float Distance { get; set; } = 40f;

    [Export]
    public bool UsePerspective { get; set; }

    [Export]
    public float PerspectiveFov { get; set; } = 35f;

    public override void _Ready()
    {
        orthogonalSize = Size;

        ApplyProjection();
    }

    public override void _Process(double delta)
        => Follow();

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F2 })
            return;

        UsePerspective = !UsePerspective;

        ApplyProjection();
    }

    //Die Perspektive rückt so weit heran, dass der Held so groß bleibt wie in der orthogonalen Sicht
    private void ApplyProjection()
    {
        if (UsePerspective)
        {
            Projection = ProjectionType.Perspective;
            Fov        = PerspectiveFov;
            offset     = GlobalBasis.Z * (orthogonalSize / 2f / Mathf.Tan(Mathf.DegToRad(PerspectiveFov) / 2f));
        }
        else
        {
            Projection = ProjectionType.Orthogonal;
            Size       = orthogonalSize;
            offset     = GlobalBasis.Z * Distance;
        }

        Follow();
    }

    private void Follow()
    {
        if (IsInstanceValid(Target))
            GlobalPosition = Target.GlobalPosition + offset;
    }
}
