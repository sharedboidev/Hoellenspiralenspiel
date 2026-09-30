using System;
using Godot;
using Hoellenspiralenspiel.Resources.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Environment;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

//Öffnet sich am Portal eines Kreises und bietet seine Checkpoints an
public partial class CircleDialog : Control, IClosableWindow
{
    private const string NewDescentText     = "New Descent";
    private const string ConfirmDescentText = "Reroll all levels?";

    private Button        closeButton;
    private DescentState  descent;
    private Hero          hero;
    private bool          isConfirming;
    private VBoxContainer levelList;
    private Button        newDescentButton;
    private CirclePortal  portal;
    private Label         stateLabel;
    private Label         titleLabel;

    [Export]
    public Vector2 LevelButtonSize { get; set; } = new(420, 72);

    [Export]
    public int LevelFontSize { get; set; } = 32;

    public LevelThemeResource Circle => portal?.Circle;

    public event Action<LevelThemeResource, int> LevelChosen;
    public event Action<LevelThemeResource>      NewDescentRequested;

    public override void _Ready()
    {
        titleLabel       = GetNode<Label>("%TitleLabel");
        stateLabel       = GetNode<Label>("%StateLabel");
        levelList        = GetNode<VBoxContainer>("%LevelList");
        newDescentButton = GetNode<Button>("%NewDescentButton");
        closeButton      = GetNode<Button>("%CloseButton");

        newDescentButton.Pressed += OnNewDescentPressed;
        closeButton.Pressed      += Close;

        Hide();
    }

    //Wer vom Portal wegläuft, braucht den Dialog nicht mehr
    public override void _Process(double delta)
    {
        if (Visible && (!IsInstanceValid(portal) || !IsInstanceValid(hero) || !portal.IsInReachOf(hero)))
            Close();
    }

    public bool IsOpen => Visible;

    public void ShowFor(CirclePortal usedPortal, DescentState state, Hero user)
    {
        if (usedPortal?.Circle is null || state is null)
            return;

        portal  = usedPortal;
        descent = state;
        hero    = user;

        Refresh();
        Show();
    }

    public void Choose(int depth)
    {
        if (Circle is null || !descent.HasReached(depth))
            return;

        var circle = Circle;

        Close();

        LevelChosen?.Invoke(circle, depth);
    }

    public void Close()
    {
        isConfirming = false;
        portal       = null;

        Hide();
    }

    private void Refresh()
    {
        titleLabel.Text       = Circle.DisplayName;
        stateLabel.Text       = descent.HasBegun ? "The levels stay as you left them." : "No descent has begun yet.";
        newDescentButton.Text = isConfirming ? ConfirmDescentText : NewDescentText;

        newDescentButton.Disabled = !descent.HasBegun;

        foreach (var child in levelList.GetChildren())
        {
            levelList.RemoveChild(child);

            child.QueueFree();
        }

        for (var depth = 1; depth <= Circle.LevelCount; depth++)
            levelList.AddChild(CreateLevelButton(depth));
    }

    private Button CreateLevelButton(int depth)
    {
        var isReached = descent.HasReached(depth);
        var button = new Button
        {
            Name              = $"Level{depth}",
            Text              = isReached ? $"Level {depth}" : $"Level {depth} · not reached",
            Disabled          = !isReached,
            CustomMinimumSize = LevelButtonSize,
            FocusMode         = FocusModeEnum.None
        };

        button.AddThemeFontSizeOverride("font_size", LevelFontSize);

        button.Pressed += () => Choose(depth);

        return button;
    }

    //Der erste Klick fragt nach, erst der zweite würfelt neu
    private void OnNewDescentPressed()
    {
        if (!isConfirming)
        {
            isConfirming = true;

            Refresh();

            return;
        }

        isConfirming = false;

        NewDescentRequested?.Invoke(Circle);

        Refresh();

        stateLabel.Text = "A new descent awaits.";
    }
}
