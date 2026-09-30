using System;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.UI.Settings;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI;

//Einstellungen aus dem Pausen- und dem Hauptmenü, ein Reiter je Bereich.
//Escape schließt nur dieses Fenster und führt zurück ins Menü. Vorher dürfen die Reiter eine Eingabe für sich nehmen
public partial class SettingsWindow : Control, IClosableWindow
{
    private ISettingsTab[] tabs = [];

    public event Action Closed;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        tabs = GetNode<TabContainer>("%Tabs").GetChildren().OfType<ISettingsTab>().ToArray();

        GetNode<Button>("%BackButton").Pressed += Close;

        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;

        if (tabs.Any(tab => tab.TakesInput(@event)))
        {
            GetViewport().SetInputAsHandled();

            return;
        }

        if (!@event.IsActionPressed(InputActions.TogglePauseMenu))
            return;

        Close();

        GetViewport().SetInputAsHandled();
    }

    public void Open()
    {
        if (Visible)
            return;

        foreach (var tab in tabs)
            tab.ShowCurrent();

        Show();

        GetNode<TabContainer>("%Tabs").GetTabBar().GrabFocus();
    }

    public void Close()
    {
        if (!Visible)
            return;

        foreach (var tab in tabs)
            tab.Commit();

        Hide();

        Closed?.Invoke();
    }
}
