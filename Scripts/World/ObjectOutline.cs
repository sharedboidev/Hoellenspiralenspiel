using System.Linq;
using Godot;

namespace Hoellenspiralenspiel.Scripts.World;

//Was der Held benutzen kann, trägt einen Umriss wie die Figuren. Den bekommen nur Teile mit dem Shader ps1_object,
//ohne Aufruf ist er dunkel. Den Rand zieht ps1_unit_outline
public static class ObjectOutline
{
    private const string ShaderPath = "res://Shaders/Ps1/ps1_object.gdshader";

    private const int NoOutline      = 0;
    private const int DarkOutline    = 1;
    private const int GlowingOutline = 2;

    private static readonly StringName OutlineParameter = "outline";
    private static readonly StringName GlowHueParameter = "glow_hue";

    public static Shader Shader { get; } = GD.Load<Shader>(ShaderPath);

    //Ohne Deckkraft wird der Rand dunkel, sonst leuchtet er in einer hellen Fassung dieser Farbe
    public static void Show(Node root, Color glow)
        => Apply(root, glow.A > 0f ? GlowingOutline : DarkOutline, glow.H);

    public static void Hide(Node root)
        => Apply(root, NoOutline, 0f);

    private static void Apply(Node root, int outline, float glowHue)
    {
        foreach (var mesh in root.FindChildren("*", nameof(GeometryInstance3D), true, false).OfType<GeometryInstance3D>())
        {
            mesh.SetInstanceShaderParameter(OutlineParameter, outline);
            mesh.SetInstanceShaderParameter(GlowHueParameter, glowHue);
        }
    }
}
