using Godot;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class FloatingCombatText : Node2D
{
    public delegate void QueueFreedSignal();

    private const string FontColor = "font_color";
    private const string FontSize  = "font_size";

    [Export]
    public float DriftVelocity { get; set; } = 0.3f;

    [Export]
    public float VisibilityTimeSeconds { get; set; } = 2;

    [Export]
    public float FadeDelaySeconds { get; set; } = 1;

    public Label                  Display { get; set; }
    public int                    Value   { get; set; }
    public double                 Elapsed { get; set; }
    public event QueueFreedSignal QueueFreed;

    public override void _Ready()
    {
        base._Ready();

        Display = GetLabelComponent();
    }

    public void ShowInTree()
    {
        Show();

        GetTree()
               .CurrentScene
               .AddChild(this);
    }

    private Label GetLabelComponent() => GetNode<Label>(nameof(Label));

    public void SetFontSize(int size) => Display?.AddThemeFontSizeOverride(FontSize, size);

    public void SetFontColor(Color color) => Display?.AddThemeColorOverride(FontColor, color);

    public override void _Process(double delta)
    {
        Elapsed += delta;

        if (Elapsed >= FadeDelaySeconds)
            Modulate = new Color(Modulate, 1 - ((float)Elapsed - 1));

        if (Elapsed >= VisibilityTimeSeconds)
            QueueFree();

        Position += new Vector2(0, -DriftVelocity);
    }

    public void _freed() => QueueFreed?.Invoke();
}