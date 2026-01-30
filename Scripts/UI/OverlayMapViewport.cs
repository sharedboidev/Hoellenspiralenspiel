using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class OverlayMapViewport : SubViewport
{
    [Export]
    public Node2D Camera { get; set; }

    [Export]
    public Player2D Player { get; set; }

    public override void _Ready()
    {
        World2D                                   = GetTree().Root.World2D;
        GetParent<SubViewportContainer>().Visible = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        Camera.Position = Player.Position;

        if (Input.IsActionJustPressed("Tab"))
        {
            var parent = GetParent<SubViewportContainer>();
            parent.Visible = !parent.Visible;
        }
    }
}