using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

//Das Schild liegt auf der 2D-Ebene und folgt seinem Träger. Schrift in der 3D-Welt ginge in den Bildzeilen des PS1-Looks unter
public partial class NameTag : VBoxContainer
{
    private const int   NameFontSize  = 20;
    private const int   ModsFontSize  = 15;
    private const float WidthPx       = 360f;
    private const int   OutlineSize   = 4;
    private const int   LineSpacingPx = -4;

    private static readonly Color ModsColor = new(0.8f, 0.8f, 0.8f);

    private Node3D bearer;
    private float  heightMeters;

    public bool IsShown { get; set; } = true;

    public static NameTag Create(Node3D bearer, float heightMeters, string name, Color nameColor, string mods)
    {
        var tag = new NameTag
        {
            Name                = "NameTag",
            bearer              = bearer,
            heightMeters        = heightMeters,
            MouseFilter         = MouseFilterEnum.Ignore,
            CustomMinimumSize   = new Vector2(WidthPx, 0),
            GrowVertical        = GrowDirection.Begin,
            Visible             = false
        };

        tag.AddThemeConstantOverride("separation", LineSpacingPx);
        tag.AddChild(CreateLine(name, nameColor, NameFontSize));
        tag.AddChild(CreateLine(mods, ModsColor, ModsFontSize));

        return tag;
    }

    public override void _Process(double delta)
    {
        //Beim Abbau eines Orts hängt der Träger schon nicht mehr im Baum, freigegeben wird er erst danach
        if (!IsInstanceValid(bearer) || !bearer.IsInsideTree())
        {
            QueueFree();

            return;
        }

        var camera = GetViewport().GetCamera3D();
        var anchor = bearer.GlobalPosition + Vector3.Up * heightMeters;

        Visible = IsShown && camera is not null && !camera.IsPositionBehind(anchor);

        if (Visible)
            Position = camera.UnprojectPosition(anchor) - new Vector2(Size.X / 2f, Size.Y);
    }

    private static Label CreateLine(string text, Color color, int fontSize)
    {
        var line = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };

        line.AddThemeFontSizeOverride("font_size", fontSize);
        line.AddThemeColorOverride("font_color", color);
        line.AddThemeColorOverride("font_outline_color", Colors.Black);
        line.AddThemeConstantOverride("outline_size", OutlineSize);

        return line;
    }
}
