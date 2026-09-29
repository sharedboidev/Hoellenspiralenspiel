using Godot;

namespace Hoellenspiralenspiel.Scripts.Spike3D;

public partial class HealthBar3D : Node3D
{
    private const float Width  = 1f;
    private const float Height = 0.1f;

    private static readonly Color BackColor = new(0.08f, 0.08f, 0.08f, 0.85f);
    private static readonly Color FillColor = new(0.8f, 0.1f, 0.1f);

    private MeshInstance3D fill;

    public override void _Ready()
    {
        var back = CreateBar(BackColor, 1);

        fill = CreateBar(FillColor, 2);

        AddChild(back);
        AddChild(fill);

        back.Position = new Vector3(-Width / 2f, 0, 0);
        fill.Position = new Vector3(-Width / 2f, 0, 0);
    }

    //Die Kamera dreht sich nie, der Balken übernimmt deshalb nur ihre Ausrichtung
    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        var camera = GetViewport().GetCamera3D();

        if (camera is not null)
            GlobalBasis = camera.GlobalBasis;
    }

    public void SetRatio(float ratio)
        => fill.Scale = new Vector3(Mathf.Clamp(ratio, 0.001f, 1f), 1, 1);

    private static MeshInstance3D CreateBar(Color color, int priority)
        => new()
        {
            Mesh = new QuadMesh
            {
                Size         = new Vector2(Width, Height),
                CenterOffset = new Vector3(Width / 2f, 0, 0)
            },
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode    = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency   = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor    = color,
                NoDepthTest    = true,
                RenderPriority = priority
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
}
