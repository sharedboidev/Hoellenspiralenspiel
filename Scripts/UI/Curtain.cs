using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

//Verdeckt das Bild, solange ein Ort abgebaut und der nächste aufgebaut wird
public partial class Curtain : ColorRect
{
    private Tween fade;

    [Export]
    public double FadeSec { get; set; } = 0.35;

    public bool IsDown => Visible && Modulate.A >= 1f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        Hide();
    }

    public void Drop()
    {
        fade?.Kill();

        Modulate = Colors.White;

        Show();
    }

    public void Lift()
    {
        if (!Visible)
            return;

        fade?.Kill();

        fade = CreateTween();

        fade.TweenProperty(this, "modulate:a", 0f, FadeSec);
        fade.TweenCallback(Callable.From(Hide));
    }
}
