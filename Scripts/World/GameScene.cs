using Godot;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class GameScene : Node3D
{
    [Export]
    public EnemyController Enemies { get; set; }

    [Export]
    public Label Status { get; set; }

    [Export]
    public Ps1Look Look { get; set; }

    [Export]
    public Descent Descent { get; set; }

    [Export]
    public IsoCamera Camera { get; set; }

    public override void _Process(double delta)
    {
        if (Status is not null)
            Status.Text = $"{Engine.GetFramesPerSecond():N0} FPS · {Enemies?.Enemies.Count ?? 0} Gegner · {DescribeLook()}{DescribeCamera()}\n{DescribePlace()}";
    }

    private string DescribeLook()
    {
        if (Look is null || !Look.Enabled)
            return "PS1-Look aus";

        return $"PS1-Look mit {Look.Lines} Zeilen, {(Look.RealShadows ? "Schatten aus Lichtern" : "Scheiben als Schatten")}";
    }

    private string DescribeCamera()
        => Camera is null ? string.Empty : $" · Kamera zeigt {Camera.ViewHeight:0.#} m Höhe aus {Camera.TargetDistance:0.#} m Abstand";

    //Mit dem Seed des Abstiegs im Feld Seed des Knotens Descent entsteht dieselbe Folge von Ebenen noch einmal
    private string DescribePlace()
    {
        if (Descent?.Level is not null)
            return $"{Descent.Circle.DisplayName} · Ebene {Descent.State.Depth} von {Descent.Circle.LevelCount} · Seed des Abstiegs {Descent.State.Seed} · Bereichslevel {Enemies?.AreaLevel}";

        return Descent?.Place?.DisplayName ?? string.Empty;
    }
}
