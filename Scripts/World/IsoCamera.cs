using Godot;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class IsoCamera : Camera3D
{
    private float             baseDistance;
    private float             fogBegin;
    private float             fogEnd;
    private Godot.Environment fogEnvironment;
    private Vector3           offset;
    private float             viewHeight = 14f;

    [Export]
    public Node3D Target { get; set; }

    [Export]
    public float Distance { get; set; } = 40f;

    [Export]
    public bool UsePerspective { get; set; } = true;

    [Export]
    public float PerspectiveFov { get; set; } = 35f;

    //Grenzen für das Mausrad. Sie stehen vor ViewHeight, weil Godot die Felder in dieser Reihenfolge setzt
    [Export]
    public float MinViewHeight { get; set; } = 6f;

    [Export]
    public float MaxViewHeight { get; set; } = 14f;

    [Export]
    public float ZoomStep { get; set; } = 2f;

    //So viele Meter Welt zeigt das Bild in der Höhe, gemessen am Helden. Das Mausrad verstellt den Wert in Schritten, zum Testen
    [Export(PropertyHint.Range, "4,60,0.5")]
    public float ViewHeight
    {
        get => viewHeight;
        set
        {
            viewHeight = Mathf.Clamp(value, MinViewHeight, MaxViewHeight);

            if (IsInsideTree())
                ApplyProjection();
        }
    }

    //Abstand zum Helden in Metern. Bei orthogonaler Sicht bestimmt er nicht, wie groß die Welt erscheint
    public float TargetDistance => offset.Length();

    public override void _Ready()
    {
        ApplyProjection();

        //Der Nebel zählt ab der Kamera. Er rückt beim Zoomen mit, damit er gleich weit hinter dem Helden bleibt
        fogEnvironment = GetWorld3D()?.Environment;
        baseDistance   = TargetDistance;
        fogBegin       = fogEnvironment?.FogDepthBegin ?? 0f;
        fogEnd         = fogEnvironment?.FogDepthEnd ?? 0f;
    }

    public override void _Process(double delta)
        => Follow();

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!OS.IsDebugBuild() || @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F2 })
            return;

        UsePerspective = !UsePerspective;

        ApplyProjection();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } button)
            return;

        var step = button.ButtonIndex switch
        {
            MouseButton.WheelUp   => -ZoomStep,
            MouseButton.WheelDown => ZoomStep,
            _                     => 0f
        };

        if (step == 0f)
            return;

        ViewHeight += step;

        GetViewport().SetInputAsHandled();
    }

    //Die Perspektive rückt so weit heran, dass der Held so groß bleibt wie in der orthogonalen Sicht
    private void ApplyProjection()
    {
        if (UsePerspective)
        {
            Projection = ProjectionType.Perspective;
            Fov        = PerspectiveFov;
            offset     = GlobalBasis.Z * (ViewHeight / 2f / Mathf.Tan(Mathf.DegToRad(PerspectiveFov) / 2f));
        }
        else
        {
            Projection = ProjectionType.Orthogonal;
            Size       = ViewHeight;
            offset     = GlobalBasis.Z * Distance;
        }

        Follow();
        ShiftFog();
    }

    private void ShiftFog()
    {
        if (fogEnvironment is null)
            return;

        var shift = TargetDistance - baseDistance;

        fogEnvironment.FogDepthBegin = fogBegin + shift;
        fogEnvironment.FogDepthEnd   = fogEnd + shift;
    }

    private void Follow()
    {
        if (IsInstanceValid(Target))
            GlobalPosition = Target.GlobalPosition + offset;
    }
}
