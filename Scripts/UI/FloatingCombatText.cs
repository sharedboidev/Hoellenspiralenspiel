using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

//Die Zahl liegt auf der 2D-Ebene und folgt einem Punkt der Welt. Ein Label3D pro Zahl wäre bei vielen Treffern zu teuer
public partial class FloatingCombatText : Label
{
    private double elapsedSec;
    private float  liftPx;

    [Export]
    public float DriftPxPerSec { get; set; } = 18f;

    [Export]
    public float VisibilityTimeSec { get; set; } = 2f;

    [Export]
    public float FadeDelaySec { get; set; } = 1f;

    public Vector3 WorldAnchor { get; set; }

    public override void _Ready()
        => Follow();

    public override void _Process(double delta)
    {
        elapsedSec += delta;
        liftPx     += DriftPxPerSec * (float)delta;

        if (elapsedSec >= VisibilityTimeSec)
        {
            QueueFree();

            return;
        }

        if (elapsedSec >= FadeDelaySec)
        {
            var fadeSec = Mathf.Max(VisibilityTimeSec - FadeDelaySec, 0.001f);

            Modulate = new Color(Modulate, Mathf.Clamp(1f - ((float)elapsedSec - FadeDelaySec) / fadeSec, 0f, 1f));
        }

        Follow();
    }

    private void Follow()
    {
        var camera = GetViewport().GetCamera3D();

        Visible = camera is not null && !camera.IsPositionBehind(WorldAnchor);

        if (Visible)
            Position = camera.UnprojectPosition(WorldAnchor) - Size / 2f - new Vector2(0, liftPx);
    }
}
