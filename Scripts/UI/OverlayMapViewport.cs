using Godot;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class OverlayMapViewport : SubViewport
{
    [Export]
    public Camera3D Camera { get; set; }

    [Export]
    public Node3D Target { get; set; }

    //So viele Meter der Welt zeigt die Karte von oben nach unten
    [Export]
    public float ViewSizeMeters { get; set; } = 60f;

    [Export]
    public float DistanceMeters { get; set; } = 80f;

    public override void _Ready()
        => GetParent<SubViewportContainer>().Visible = false;

    public override void _Process(double delta)
    {
        var container = GetParent<SubViewportContainer>();

        if (Input.IsActionJustPressed(InputActions.ToggleOverlayMap))
            container.Visible = !container.Visible;

        if (container.Visible)
            LookLikeTheGameCamera();
    }

    //Die Karte blickt aus demselben Winkel wie das Spiel, nur von weiter weg
    private void LookLikeTheGameCamera()
    {
        var gameCamera = GetParent().GetViewport().GetCamera3D();

        if (gameCamera is null || Camera is null || !IsInstanceValid(Target))
            return;

        Camera.Projection     = Camera3D.ProjectionType.Orthogonal;
        Camera.Size           = ViewSizeMeters;
        Camera.GlobalBasis    = gameCamera.GlobalBasis;
        Camera.GlobalPosition = Target.GlobalPosition + gameCamera.GlobalBasis.Z * DistanceMeters;
    }
}
