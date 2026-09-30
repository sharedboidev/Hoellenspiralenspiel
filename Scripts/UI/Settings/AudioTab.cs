using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using Hoellenspiralenspiel.Scripts.Saving;

namespace Hoellenspiralenspiel.Scripts.UI.Settings;

//Regler wirken sofort. Gespeichert wird beim Loslassen, bei jedem Schritt mit Tastatur oder Mausrad und beim Schließen
public partial class AudioTab : VBoxContainer, ISettingsTab
{
    private readonly Dictionary<Slider, (Label Label, Func<AudioSettings, float> Read)> sliders = new();

    private bool              isDirty;
    private bool              isDragging;
    private AudioStreamPlayer previewSound;

    public override void _Ready()
    {
        previewSound = GetNode<AudioStreamPlayer>("%PreviewSound");

        BindSlider("Master", audio => audio.Master, (audio, share) => audio.Master = share, true);
        BindSlider("Music", audio => audio.Music, (audio, share) => audio.Music = share, false);
        BindSlider("Effects", audio => audio.Effects, (audio, share) => audio.Effects = share, true);
    }

    public void ShowCurrent()
    {
        var audio = UserSettings.Instance?.Current.Audio ?? new AudioSettings();

        isDirty    = false;
        isDragging = false;

        foreach (var (slider, (label, read)) in sliders)
        {
            slider.SetValueNoSignal(Mathf.Round(read(audio) * slider.MaxValue));

            label.Text = FormatShare(slider.Value);
        }
    }

    public void Commit()
        => Save();

    public bool TakesInput(InputEvent inputEvent)
        => false;

    private void BindSlider(string name, Func<AudioSettings, float> read, Action<AudioSettings, float> store, bool playsPreview)
    {
        var slider = GetNode<Slider>($"%{name}Slider");
        var label  = GetNode<Label>($"%{name}Value");

        sliders[slider] = (label, read);

        slider.ValueChanged += value =>
        {
            var share = (float)(value / slider.MaxValue);

            label.Text = FormatShare(value);
            isDirty    = true;

            UserSettings.Instance?.Preview(settings => store(settings.Audio, share));

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
    }

    private static string FormatShare(double value)
        => $"{value:0} %";

    private void Save()
    {
        if (!isDirty)
            return;

        isDirty = false;

        UserSettings.Instance?.Save();
    }
}
