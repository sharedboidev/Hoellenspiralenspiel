using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI.Settings;

//Schalter wirken und speichern sofort. Modus und Größe des Fensters greifen erst über Apply
//und springen zurück, wenn der Spieler sie nicht binnen ConfirmSec bestätigt, etwa weil er nichts mehr sieht
public partial class DisplayTab : VBoxContainer, ISettingsTab
{
    private static readonly int[] FpsLimits = [0, 30, 60, 120, 144, 240];

    private readonly List<PixelSize> offeredSizes = new();

    private Button          applyButton;
    private HSlider         brightnessSlider;
    private Label           brightnessValue;
    private DisplaySettings confirmedWindow;
    private ulong           confirmEndsMsec;
    private Label           confirmLabel;
    private Control         confirmRow;
    private CheckButton     ditherCheck;
    private OptionButton    fpsOption;
    private CheckButton     fpsCheck;
    private OptionButton    grainOption;
    private bool            isDirty;
    private bool            isDragging;
    private OptionButton    modeOption;
    private CheckButton     shadowCheck;
    private OptionButton    sizeOption;
    private CheckButton     vsyncCheck;
    private CheckButton     wobbleCheck;

    [Export]
    public double ConfirmSec { get; set; } = 10;

    public bool IsConfirming => confirmedWindow is not null;

    public override void _Ready()
    {
        modeOption       = GetNode<OptionButton>("%ModeOption");
        sizeOption       = GetNode<OptionButton>("%SizeOption");
        applyButton      = GetNode<Button>("%ApplyButton");
        confirmRow       = GetNode<Control>("%ConfirmRow");
        confirmLabel     = GetNode<Label>("%ConfirmLabel");
        vsyncCheck       = GetNode<CheckButton>("%VSyncCheck");
        fpsOption        = GetNode<OptionButton>("%FpsOption");
        fpsCheck         = GetNode<CheckButton>("%ShowFpsCheck");
        brightnessSlider = GetNode<HSlider>("%BrightnessSlider");
        brightnessValue  = GetNode<Label>("%BrightnessValue");
        grainOption      = GetNode<OptionButton>("%GrainOption");
        ditherCheck      = GetNode<CheckButton>("%DitherCheck");
        wobbleCheck      = GetNode<CheckButton>("%WobbleCheck");
        shadowCheck      = GetNode<CheckButton>("%ShadowCheck");

        modeOption.AddItem("Borderless Fullscreen", (int)DisplayMode.Borderless);
        modeOption.AddItem("Exclusive Fullscreen", (int)DisplayMode.Exclusive);
        modeOption.AddItem("Windowed", (int)DisplayMode.Windowed);

        foreach (var limit in FpsLimits)
            fpsOption.AddItem(limit == 0 ? "Unlimited" : $"{limit}", limit);

        grainOption.AddItem("Coarse", (int)PixelGrain.Coarse);
        grainOption.AddItem("Medium", (int)PixelGrain.Medium);
        grainOption.AddItem("Fine", (int)PixelGrain.Fine);

        brightnessSlider.MinValue = LookSettings.MinBrightness * 100;
        brightnessSlider.MaxValue = LookSettings.MaxBrightness * 100;

        modeOption.ItemSelected    += _ => ShowWindowChoice();
        sizeOption.ItemSelected    += _ => ShowWindowChoice();
        applyButton.Pressed        += ApplyWindow;
        GetNode<Button>("%KeepButton").Pressed   += KeepWindow;
        GetNode<Button>("%RevertButton").Pressed += RevertWindow;

        vsyncCheck.Toggled  += isOn => Change(settings => settings.Display.VSync = isOn);
        fpsCheck.Toggled    += isOn => Change(settings => settings.Display.ShowFps = isOn);
        ditherCheck.Toggled += isOn => Change(settings => settings.Look.Dithering = isOn);
        wobbleCheck.Toggled += isOn => Change(settings => settings.Look.VertexWobble = isOn);
        shadowCheck.Toggled += isOn => Change(settings => settings.Look.RealShadows = isOn);

        fpsOption.ItemSelected   += index => Change(settings => settings.Display.MaxFps = fpsOption.GetItemId((int)index));
        grainOption.ItemSelected += index => Change(settings => settings.Look.Grain = (PixelGrain)grainOption.GetItemId((int)index));

        brightnessSlider.ValueChanged += OnBrightnessChanged;
        brightnessSlider.DragStarted  += () => isDragging = true;
        brightnessSlider.DragEnded    += _ =>
        {
            isDragging = false;

            Save();
        };

        confirmRow.Hide();
    }

    public override void _Process(double delta)
    {
        if (!IsConfirming)
            return;

        var leftSec = (long)confirmEndsMsec - (long)Time.GetTicksMsec();

        if (leftSec <= 0)
        {
            RevertWindow();

            return;
        }

        confirmLabel.Text = $"Keep these display settings? Reverting in {Math.Ceiling(leftSec / 1000.0):0} s";
    }

    public void ShowCurrent()
    {
        var current = UserSettings.Instance?.Current ?? new GameSettings();
        var display = current.Display;
        var look    = current.Look;

        isDirty    = false;
        isDragging = false;

        FillSizes(display.WindowSize);

        modeOption.Select(modeOption.GetItemIndex((int)display.Mode));
        sizeOption.Select(Math.Max(0, offeredSizes.IndexOf(display.WindowSize)));
        ShowFpsLimit(display.MaxFps);
        grainOption.Select(grainOption.GetItemIndex((int)look.Grain));

        vsyncCheck.SetPressedNoSignal(display.VSync);
        fpsCheck.SetPressedNoSignal(display.ShowFps);
        ditherCheck.SetPressedNoSignal(look.Dithering);
        wobbleCheck.SetPressedNoSignal(look.VertexWobble);
        shadowCheck.SetPressedNoSignal(look.RealShadows);

        brightnessSlider.SetValueNoSignal(Mathf.Round(look.Brightness * 100));
        brightnessValue.Text = $"{brightnessSlider.Value:0} %";

        ShowWindowChoice();
    }

    //Eine Grenze, die nicht in der Liste steht, etwa von Hand in der Datei, bekommt einen eigenen Eintrag statt als Unlimited zu erscheinen
    private void ShowFpsLimit(int maxFps)
    {
        while (fpsOption.ItemCount > FpsLimits.Length)
            fpsOption.RemoveItem(fpsOption.ItemCount - 1);

        if (!FpsLimits.Contains(maxFps))
            fpsOption.AddItem($"{maxFps}", maxFps);

        fpsOption.Select(fpsOption.GetItemIndex(maxFps));
    }

    public void Commit()
    {
        if (IsConfirming)
            RevertWindow();

        Save();
    }

    //Escape nimmt eine unbestätigte Änderung zurück, statt das Fenster zu schließen
    public bool TakesInput(InputEvent inputEvent)
    {
        if (!IsConfirming || !inputEvent.IsActionPressed(InputActions.TogglePauseMenu))
            return false;

        RevertWindow();

        return true;
    }

    private void FillSizes(PixelSize current)
    {
        var room = UserSettings.Instance?.GetWindowRoom() ?? WindowSizes.Maximum;

        offeredSizes.Clear();
        offeredSizes.AddRange(WindowSizes.Offer(room));

        if (!offeredSizes.Contains(current))
            offeredSizes.Add(current);

        offeredSizes.Sort((first, second) => (first.Width * first.Height).CompareTo(second.Width * second.Height));

        sizeOption.Clear();

        foreach (var size in offeredSizes)
            sizeOption.AddItem($"{size.Width} x {size.Height}");
    }

    private (DisplayMode Mode, PixelSize Size) GetWindowChoice()
        => ((DisplayMode)modeOption.GetSelectedId(), offeredSizes.ElementAtOrDefault(sizeOption.Selected));

    private void ShowWindowChoice()
    {
        var (mode, size) = GetWindowChoice();
        var current      = UserSettings.Instance?.Current.Display ?? new DisplaySettings();

        sizeOption.Disabled  = mode != DisplayMode.Windowed || IsConfirming;
        applyButton.Disabled = IsConfirming || (mode == current.Mode && (mode != DisplayMode.Windowed || size == current.WindowSize));
    }

    private void ApplyWindow()
    {
        if (UserSettings.Instance is not { } settings)
            return;

        var (mode, size) = GetWindowChoice();

        confirmedWindow = settings.Current.Display.Copy();
        confirmEndsMsec = Time.GetTicksMsec() + (ulong)(ConfirmSec * 1000);

        settings.Preview(next =>
        {
            next.Display.Mode = mode;

            if (mode != DisplayMode.Windowed)
                return;

            next.Display.WindowWidth  = size.Width;
            next.Display.WindowHeight = size.Height;
        });

        SetConfirming(true);
    }

    private void KeepWindow()
    {
        if (!IsConfirming)
            return;

        confirmedWindow = null;

        UserSettings.Instance?.Save();

        SetConfirming(false);
    }

    private void RevertWindow()
    {
        if (!IsConfirming)
            return;

        var previous = confirmedWindow;

        confirmedWindow = null;

        UserSettings.Instance?.Preview(next =>
        {
            next.Display.Mode         = previous.Mode;
            next.Display.WindowWidth  = previous.WindowWidth;
            next.Display.WindowHeight = previous.WindowHeight;
        });

        SetConfirming(false);
        ShowCurrent();
    }

    //Solange eine Änderung auf Bestätigung wartet, bleibt alles andere stehen. Sonst speicherte ein anderer Schalter sie mit
    private void SetConfirming(bool isConfirming)
    {
        confirmRow.Visible = isConfirming;

        foreach (var control in new BaseButton[] { modeOption, vsyncCheck, fpsOption, fpsCheck, grainOption, ditherCheck, wobbleCheck, shadowCheck })
            control.Disabled = isConfirming;

        brightnessSlider.Editable = !isConfirming;

        if (GetParent() is TabContainer tabs)
            tabs.TabsVisible = !isConfirming;

        ShowWindowChoice();

        if (isConfirming)
            GetNode<Button>("%KeepButton").GrabFocus();
    }

    private void OnBrightnessChanged(double value)
    {
        brightnessValue.Text = $"{value:0} %";
        isDirty              = true;

        UserSettings.Instance?.Preview(settings => settings.Look.Brightness = (float)(value / 100));

        if (!isDragging)
            Save();
    }

    private void Change(Action<GameSettings> change)
        => UserSettings.Instance?.Change(change);

    private void Save()
    {
        if (!isDirty)
            return;

        isDirty = false;

        UserSettings.Instance?.Save();
    }
}
