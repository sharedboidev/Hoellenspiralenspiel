using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Utils;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.UI;

//Hält das Spiel an. Escape schließt zuerst offene Fenster, erst danach geht das Menü auf
public partial class PauseMenu : Control
{
    private Button resumeButton;

    [Export]
    public GameController Game { get; set; }

    //Während einer Reise hält der Abstieg das Spiel an. Das Menü bleibt dann zu, sonst liefe die Welt hinter dem Vorhang weiter
    [Export]
    public Descent Descent { get; set; }

    //Nach diesen Fenstern sucht das Menü, in der Spielszene ist das die Hud
    [Export]
    public Node WindowRoot { get; set; }

    [Export(PropertyHint.File, "*.tscn")]
    public string MainMenuPath { get; set; } = "res://Scenes/main_menu.tscn";

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        resumeButton = GetNode<Button>("%ResumeButton");

        resumeButton.Pressed                       += Resume;
        GetNode<Button>("%MainMenuButton").Pressed += ReturnToMainMenu;
        GetNode<Button>("%QuitButton").Pressed     += Quit;

        Hide();
    }

    //Vor der Oberfläche, damit die Leertaste keinen Knopf drückt, der noch den Fokus hat
    public override void _Input(InputEvent @event)
    {
        if (Descent?.IsTravelling == true)
            return;

        if (@event.IsActionPressed(InputActions.TogglePauseMenu))
        {
            if (Visible)
                Resume();
            else if (CloseWindows() == 0)
                Open();

            GetViewport().SetInputAsHandled();
        }
        else if (!Visible && @event.IsActionPressed(InputActions.CloseWindows) && CloseWindows() > 0)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public void Open()
    {
        GetTree().Paused = true;

        Show();

        resumeButton.GrabFocus();
    }

    public void Resume()
    {
        Hide();

        GetTree().Paused = false;
    }

    public int CloseWindows()
    {
        var openWindows = (WindowRoot ?? GetParent()).GetAllChildren<Node>()
                                                     .OfType<IClosableWindow>()
                                                     .Where(window => window.IsOpen)
                                                     .ToList();

        foreach (var window in openWindows)
            window.Close();

        return openWindows.Count;
    }

    public void ReturnToMainMenu()
    {
        Game?.SaveCharacter();

        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(MainMenuPath);
    }

    public void Quit()
    {
        Game?.SaveCharacter();

        GetTree().Quit();
    }
}
