using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI;

//Einstellungen aus dem Pausen- und dem Hauptmenü. Regler wirken sofort. Gespeichert wird beim Loslassen, bei jedem Schritt mit Tastatur oder Mausrad und beim Schließen.
//Escape schließt nur dieses Fenster und führt zurück ins Menü
public partial class SettingsWindow : Control, IClosableWindow
{
    private readonly Dictionary<Slider, Label> valueLabels = new();

    private AudioSettings     audio = new();
    private bool              isDirty;
    private bool              isDragging;
    private Slider            effectsSlider;
    private Slider            masterSlider;
    private Slider            musicSlider;
    private AudioStreamPlayer previewSound;

    public event Action Closed;

    public bool IsOpen => Visible;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        previewSound  = GetNode<AudioStreamPlayer>("%PreviewSound");
        masterSlider  = BindSlider("Master", share => audio.Master = share, true);
        musicSlider   = BindSlider("Music", share => audio.Music = share, false);
        effectsSlider = BindSlider("Effects", share => audio.Effects = share, true);

        GetNode<Button>("%BackButton").Pressed += Close;

        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible || !@event.IsActionPressed(InputActions.TogglePauseMenu))
            return;

        Close();

        GetViewport().SetInputAsHandled();
    }

    public void Open()
    {
        if (Visible)
            return;

        audio   = (UserSettings.Instance?.Current.Audio ?? new AudioSettings()).Copy();
        isDirty = false;

        ShowShare(masterSlider, audio.Master);
        ShowShare(musicSlider, audio.Music);
        ShowShare(effectsSlider, audio.Effects);

        Show();

        masterSlider.GrabFocus();
    }

    public void Close()
    {
        if (!Visible)
            return;

        Save();
        Hide();

        Closed?.Invoke();
    }

    private Slider BindSlider(string name, Action<float> store, bool playsPreview)
    {
        var slider = GetNode<Slider>($"%{name}Slider");
        var label  = GetNode<Label>($"%{name}Value");

        valueLabels[slider] = label;

        slider.ValueChanged += value =>
        {
            store((float)(value / slider.MaxValue));

            label.Text = FormatShare(value);
            isDirty    = true;

            UserSettings.Instance?.ApplyAudio(audio);

            //Tastatur und Mausrad kennen kein Loslassen
            if (!isDragging)
                Save();
        };

        slider.DragStarted += () => isDragging = true;

        slider.DragEnded += hasChanged =>
        {
            isDragging = false;

            if (!hasChanged)
                return;

            Save();

            if (playsPreview)
                previewSound.Play();
        };

        return slider;
    }

    private void ShowShare(Slider slider, float share)
    {
        slider.SetValueNoSignal(Mathf.Round(share * slider.MaxValue));

        valueLabels[slider].Text = FormatShare(slider.Value);
    }

    private static string FormatShare(double value)
        => $"{value:0} %";

    private void Save()
    {
        if (!isDirty || UserSettings.Instance is null)
            return;

        isDirty = false;

        var chosen = audio.Copy();

        UserSettings.Instance.Change(settings => settings.Audio = chosen);
    }
}
