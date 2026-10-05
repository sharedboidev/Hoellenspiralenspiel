using Godot;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Saving;
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

    public override void _Ready()
    {
        //Im Pausenmenü steht _Process still. Show FPS soll trotzdem sofort wirken
        if (UserSettings.Instance is { } settings)
            settings.Changed += ShowStatus;
    }

    public override void _ExitTree()
    {
        if (UserSettings.Instance is { } settings)
            settings.Changed -= ShowStatus;
    }

    public override void _Process(double delta)
        => ShowStatus();

    //Die Bildrate zeigt die Einstellung Show FPS. Der Rest der Zeile hilft beim Testen und steht nur in Debug-Builds
    private void ShowStatus()
    {
        if (Status is null)
            return;

        var showsFps = UserSettings.Instance?.Current.Display.ShowFps == true;
        var fps      = showsFps ? $"{Engine.GetFramesPerSecond():N0} FPS" : string.Empty;

        Status.Visible = showsFps || OS.IsDebugBuild();

        if (OS.IsDebugBuild())
            Status.Text = $"{fps}{(showsFps ? " · " : string.Empty)}{Enemies?.Enemies.Count ?? 0} Gegner · {DescribeLook()}{DescribeCamera()}\n{DescribePlace()}";
        else
            Status.Text = fps;
    }

    private string DescribeLook()
    {
        if (Look is null || !Look.Enabled)
            return "PS1-Look aus";

        return $"PS1-Look mit {Look.Lines} Zeilen ({Look.Grain}), {(Look.RealShadows ? "Schatten aus Lichtern" : "Scheiben als Schatten")}";
    }

    private string DescribeCamera()
        => Camera is null ? string.Empty : $" · Kamera zeigt {Camera.ViewHeight:0.#} m Höhe aus {Camera.TargetDistance:0.#} m Abstand";

    //Mit dem Seed des Abstiegs im Feld Seed des Knotens Descent entsteht dieselbe Folge von Ebenen noch einmal
    private string DescribePlace()
    {
        if (Descent?.Level is not null)
        {
            var circle = Descent.Circle;
            var depth  = Descent.State.Depth;
            var where  = circle.HasFields ? $"Fläche {depth} von {circle.DepthCount}, {circle.NameDepth(depth)}" : $"Ebene {depth} von {circle.DepthCount}";

            return $"{circle.DisplayName} · {where} · Seed des Abstiegs {Descent.State.Seed} · Bereichslevel {Enemies?.AreaLevel}";
        }

        return Descent?.Place?.DisplayName ?? string.Empty;
    }
}
