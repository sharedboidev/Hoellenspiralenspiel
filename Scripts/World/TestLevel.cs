using Godot;
using Hoellenspiralenspiel.Scripts.Controllers;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class TestLevel : Node3D
{
    [Export]
    public EnemyController Enemies { get; set; }

    [Export]
    public Label Status { get; set; }

    [Export]
    public Ps1Look Look { get; set; }

    public override void _Process(double delta)
    {
        if (Status is not null)
            Status.Text = $"{Engine.GetFramesPerSecond():N0} FPS · {Enemies?.Enemies.Count ?? 0} Gegner · {DescribeLook()}";
    }

    private string DescribeLook()
    {
        if (Look is null || !Look.Enabled)
            return "PS1-Look aus";

        return $"PS1-Look mit {Look.Lines} Zeilen, {(Look.RealShadows ? "Schatten aus Lichtern" : "Scheiben als Schatten")}";
    }
}
