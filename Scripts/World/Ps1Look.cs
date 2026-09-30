using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.World;

//Pixelgröße, Dithering, wackelnde Eckpunkte, Schatten und Helligkeit kommen aus den Einstellungen.
//F1, F3 und F4 schalten in Debug-Builds zum Testen um, ohne die Einstellungen anzufassen
public partial class Ps1Look : Node
{
    public delegate void ChangedEventHandler();

    private const string BlobShadowGroup = "blob_shadows";

    private static readonly StringName Snap            = "snap";
    private static readonly StringName SnapResolution  = "snap_resolution";
    private static readonly StringName Affine          = "affine";
    private static readonly StringName Resolution      = "resolution";
    private static readonly StringName ColorLevelsKey  = "color_levels";
    private static readonly StringName DitherKey       = "dither";
    private static readonly StringName OutlineWidthKey = "outline_width";

    private readonly Dictionary<Light3D, (bool HadShadow, uint CasterMask)> shadowOfLight = new();
    private          LookSettings                                           appliedLook;
    private          int                                                    outlineWidth  = 1;

    [Export]
    public bool Enabled { get; set; } = true;

    [Export]
    public int ColorLevels { get; set; } = 32;

    //Stärke des Musters, wenn Dithering in den Einstellungen an ist
    [Export(PropertyHint.Range, "0,2,0.05")]
    public float DitherStrength { get; set; } = 1f;

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float AffineTextures { get; set; } = 1f;

    //Breite des Rands um Held und Gegner in Pixeln der PS1, 0 schaltet ihn ab. Lässt sich im laufenden Spiel verstellen
    [Export(PropertyHint.Range, "0,4,1")]
    public int OutlineWidth
    {
        get => outlineWidth;
        set
        {
            outlineWidth = Math.Clamp(value, 0, 4);

            if (IsInsideTree())
                ApplyOutline();
        }
    }

    [Export]
    public Shader SurfaceShader { get; set; }

    //Das Rechteck über dem Bild, das den Rand um die Figuren zieht
    [Export]
    public GeometryInstance3D UnitOutline { get; set; }

    [Export]
    public ColorRect Screen { get; set; }

    [Export]
    public Node World { get; set; }

    //Die Helligkeit wirkt über die Umgebung der Welt, also vor dem Vergröbern
    [Export]
    public WorldEnvironment Surroundings { get; set; }

    public PixelGrain Grain        { get; set; } = PixelGrain.Coarse;
    public bool       Dithering    { get; set; } = true;
    public bool       SnapVertices { get; set; } = true;

    //Die PS1 kannte keine Schatten aus Lichtern, Figuren standen auf dunklen Scheiben
    public bool RealShadows { get; set; } = true;

    public int Lines => Mathf.RoundToInt(GetResolution().Y);

    public event ChangedEventHandler Changed;

    public override void _Ready()
    {
        GetTree().NodeAdded       += OnNodeAdded;
        GetViewport().SizeChanged += Apply;

        if (UserSettings.Instance is { } settings)
        {
            settings.Changed += OnSettingsChanged;

            TakeSettings(settings.Current.Look);
        }

        //Erst nach dem Aufbau der Szene hat jeder Gegner sein eigenes Material
        Callable.From(Apply).CallDeferred();
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded       -= OnNodeAdded;
        GetViewport().SizeChanged -= Apply;

        if (UserSettings.Instance is { } settings)
            settings.Changed -= OnSettingsChanged;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!OS.IsDebugBuild() || @event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        switch (key.Keycode)
        {
            case Key.F1:
                Enabled = !Enabled;

                break;
            case Key.F3:
                Grain = (PixelGrain)(((int)Grain + 1) % Enum.GetValues<PixelGrain>().Length);

                break;
            case Key.F4:
                RealShadows = !RealShadows;

                break;
            default:
                return;
        }

        Apply();
    }

    public void Apply()
    {
        var resolution = GetResolution();

        if (Screen is not null)
        {
            Screen.Visible = Enabled;

            if (Screen.Material is ShaderMaterial screenMaterial)
            {
                screenMaterial.SetShaderParameter(Resolution, resolution);
                screenMaterial.SetShaderParameter(ColorLevelsKey, (float)ColorLevels);
                screenMaterial.SetShaderParameter(DitherKey, Dithering ? DitherStrength : 0f);
            }
        }

        TuneTree(World ?? GetParent(), resolution);
        ApplyOutline();

        Changed?.Invoke();
    }

    //Nur wenn sich der Look ändert. Sonst höbe schon ein Regler für den Ton die Umschaltungen mit F3 und F4 auf
    private void OnSettingsChanged()
    {
        var look = UserSettings.Instance.Current.Look;

        if (appliedLook?.SameAs(look) == true)
            return;

        TakeSettings(look);
        Apply();
    }

    private void TakeSettings(LookSettings look)
    {
        appliedLook  = look.Copy();
        Grain        = look.Grain;
        Dithering    = look.Dithering;
        SnapVertices = look.VertexWobble;
        RealShadows  = look.RealShadows;

        if (Surroundings?.Environment is { } environment)
        {
            environment.AdjustmentEnabled    = !Mathf.IsEqualApprox(look.Brightness, 1f);
            environment.AdjustmentBrightness = look.Brightness;
        }
    }

    private void ApplyOutline()
    {
        if (UnitOutline is null)
            return;

        //Ohne Rand liest niemand den Rauheitskanal, dann spart Godot sich dessen Aufbau
        UnitOutline.Visible = OutlineWidth > 0;

        if (UnitOutline.MaterialOverride is not ShaderMaterial material)
            return;

        material.SetShaderParameter(OutlineWidthKey, Math.Max(1, OutlineWidth));
        material.SetShaderParameter(SnapResolution, GetResolution());
    }

    //Gerechnet in echten Pixeln des Fensters, damit jede Zelle gleich groß ist. Die Leinwand der Oberfläche wäre gestreckt
    private Vector2 GetResolution()
    {
        var pixels = GetWindow()?.Size ?? Vector2I.Zero;

        if (pixels.X <= 0 || pixels.Y <= 0)
            return new Vector2(PixelGrid.TargetLines(Grain) * 16f / 9f, PixelGrid.TargetLines(Grain));

        var cell = PixelGrid.CellSize(pixels.Y, Grain);

        return new Vector2((float)pixels.X / cell, (float)pixels.Y / cell);
    }

    private void OnNodeAdded(Node node)
    {
        if (node is not (MeshInstance3D or Light3D) && !node.IsInGroup(BlobShadowGroup))
            return;

        Callable.From(() =>
        {
            if (IsInstanceValid(node) && node.IsInsideTree())
                Tune(node, GetResolution());
        }).CallDeferred();
    }

    private void TuneTree(Node node, Vector2 resolution)
    {
        Tune(node, resolution);

        foreach (var child in node.GetChildren())
            TuneTree(child, resolution);
    }

    private void Tune(Node node, Vector2 resolution)
    {
        if (node.IsInGroup(BlobShadowGroup) && node is Node3D blobShadow)
            blobShadow.Visible = Enabled && !RealShadows;

        switch (node)
        {
            case MeshInstance3D mesh:
                for (var surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
                    Tune(mesh.GetActiveMaterial(surface), resolution);

                break;
            case Light3D light:
                if (!shadowOfLight.TryGetValue(light, out var original))
                    shadowOfLight[light] = original = (light.ShadowEnabled, light.ShadowCasterMask);

                //Kein Licht scheint durch Mauern. Die Schatten von allem anderen wirft ein Licht nur, wenn es das von sich aus tut und der Look sie zulässt
                light.ShadowEnabled    = true;
                light.ShadowCasterMask = original.HadShadow && (!Enabled || RealShadows) ? original.CasterMask : WallSegment.ShadowLayer;

                break;
        }
    }

    private void Tune(Material material, Vector2 resolution)
    {
        if (material is not ShaderMaterial surface || !IsPs1Shader(surface.Shader))
            return;

        surface.SetShaderParameter(Snap, Enabled && SnapVertices ? 1f : 0f);
        surface.SetShaderParameter(SnapResolution, resolution);
        surface.SetShaderParameter(Affine, Enabled ? AffineTextures : 0f);
    }

    private bool IsPs1Shader(Shader shader)
        => shader == SurfaceShader || shader == ObjectOutline.Shader || shader == WallFade.MasonryShader || UnitSight.IsUnitShader(shader);
}
