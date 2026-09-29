using Godot;

namespace Hoellenspiralenspiel.Scripts.Environment;

//Verbindet eine Stelle in einer Ebene mit dem Hub. Es öffnet sich erst nach kurzer Wirkzeit
public partial class TownPortal : Passage
{
    private double secUntilOpen;

    [Export]
    public Node3D Look { get; set; }

    [Export]
    public double OpeningSec { get; set; } = 1.0;

    public override bool IsOpen => secUntilOpen <= 0;

    public override void _Ready()
    {
        base._Ready();

        secUntilOpen = OpeningSec;

        ShowOpening();
    }

    public override void _Process(double delta)
    {
        if (IsOpen)
            return;

        secUntilOpen -= delta;

        ShowOpening();
    }

    public void OpenAtOnce()
    {
        secUntilOpen = 0;

        ShowOpening();
    }

    private void ShowOpening()
    {
        if (Look is null)
            return;

        var share = OpeningSec <= 0 ? 1f : Mathf.Clamp(1f - (float)(secUntilOpen / OpeningSec), 0f, 1f);

        Look.Scale = new Vector3(1f, Mathf.Max(0.05f, share), 1f);
    }
}
