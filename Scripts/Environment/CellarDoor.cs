using Godot;

namespace Hoellenspiralenspiel.Scripts.Environment;

public partial class CellarDoor : Area3D
{
    [Export]
    public Node3D Glow { get; set; }

    public bool IsHovered { get; private set; }

    public int TimesOpened { get; private set; }

    public override void _Ready()
    {
        MouseEntered += () => SetHovered(true);
        MouseExited  += () => SetHovered(false);

        SetHovered(false);
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            Open();
    }

    public void Open()
    {
        TimesOpened++;

        GD.Print("Init Scene Transition!!");
    }

    public void SetHovered(bool hovered)
    {
        IsHovered = hovered;

        if (Glow is not null)
            Glow.Visible = hovered;
    }
}
