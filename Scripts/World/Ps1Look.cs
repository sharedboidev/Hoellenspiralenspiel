using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Settings;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.World;

//Pixelgröße, Dithering, wackelnde Eckpunkte, Schatten und Helligkeit kommen aus den Einstellungen.
//F1, F3 und F4 schalten in Debug-Builds zum Testen um, ohne die Einstellungen anzufassen.
//Die Welt rendert in WorldViewport, der so groß ist wie das Raster der PS1. Screen zeigt sein Bild auf dem Fenster,
//jede Zelle als ganze Bildschirmpixel ohne Glättung. Das Fenster selbst rendert keine 3D-Welt, nur die Oberfläche
public partial class Ps1Look : Node
{
    public delegate void ChangedEventHandler();

    private const string BlobShadowGroup = "blob_shadows";

    private static readonly StringName Snap            = "snap";
    private static readonly StringName SnapResolution  = "snap_resolution";
    private static readonly StringName Affine          = "affine";
    private static readonly StringName ColorLevelsKey  = "color_levels";
    private static readonly StringName DitherKey       = "dither";
    private static readonly StringName OutlineWidthKey = "outline_width";

    //Ohne Fenster, etwa headless, gilt die Leinwand des Projekts
    private static readonly PixelSize FallbackWindow = new(2560, 1440);

    private readonly Dictionary<Light3D, (bool HadShadow, uint CasterMask)> shadowOfLight = new();
    private          LookSettings                                           appliedLook;
    private          int                                                    outlineWidth   = 1;
    private          PixelSize                                              cells          = new(426, 240);
    private          Vector2                                                snapResolution = new(852, 480);

    [Export]
    public bool Enabled { get; set; } = true;

    [Export]
    public int ColorLevels { get; set; } = 32;

    //Stärke des Musters, wenn Dithering in den Einstellungen an ist
    [Export(PropertyHint.Range, "0,2,0.05")]
    public float DitherStrength { get; set; } = 1f;

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float AffineTextures { get; set; } = 1f;

    //Auf diesem Raster rasten die Eckpunkte ein, unabhängig von der Pixelgröße. Auf dem groben Raster zappelt alles, was sich bewegt
    [Export]
    public PixelGrain SnapGrain { get; set; } = PixelGrain.Fine;

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

    //Hier rendert die Welt, in Zellen der PS1 statt in Pixeln des Fensters. Er sieht sie durch die Kamera des Spiels
    [Export]
    public SubViewport WorldViewport { get; set; }

    //Zeigt das Bild aus WorldViewport auf dem Fenster, ungeglättet und um ganze Zellen vergrößert
    [Export]
    public TextureRect Screen { get; set; }

    //Liegt im WorldViewport über der Welt: 15 Bit Farbtiefe mit Punktmuster, gerechnet je Zelle
    [Export]
    public ColorRect Dither { get; set; }

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

    //Zeilen des Bildes, also Zellen von oben nach unten
    public int Lines => cells.Height;

    public event ChangedEventHandler Changed;

    public override void _Ready()
    {
        GetTree().NodeAdded       += OnNodeAdded;
        GetViewport().SizeChanged += Apply;

        //Das Fenster zeigt nur die Oberfläche und das Bild aus WorldViewport. Sonst renderte es die Welt ein zweites Mal in voller Größe
        if (WorldViewport is not null)
            GetViewport().Disable3D = true;

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
        GetViewport().Disable3D   =  false;

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
        var window = GetWindowPixels();
        var cell   = Enabled ? PixelGrid.CellSize(window.Height, Grain) : 1;

        cells = PixelGrid.CellsToCover(window, cell);

        //Das Bild in Pixeln des Fensters. Geht das Fenster nicht in Zellen auf, ragt es an den Rändern hinaus
        var picture = new Vector2(cells.Width * cell, cells.Height * cell);
        var (x, y)  = PixelGrid.Offset(window, cells, cell);

        //Das Raster der Eckpunkte zählt in Zellen seiner Stufe über das ganze Bild
        snapResolution = picture / PixelGrid.CellSize(window.Height, SnapGrain);

        if (WorldViewport is not null)
        {
            WorldViewport.Size = new Vector2I(cells.Width, cells.Height);

            //Die Kamera bleibt im Fenster, damit Maus und Schilder weiter in dessen Leinwand rechnen. Der Viewport leiht sie sich
            if (GetViewport().GetCamera3D() is { } camera)
                RenderingServer.ViewportAttachCamera(WorldViewport.GetViewportRid(), camera.GetCameraRid());

            if (Screen is not null)
            {
                //Lage und Größe in der Leinwand der Oberfläche, die Godot auf das Fenster streckt
                var toCanvas = GetViewport().GetFinalTransform().AffineInverse();

                Screen.Texture  ??= WorldViewport.GetTexture();
                Screen.Position =   toCanvas * new Vector2(x, y);
                Screen.Size     =   toCanvas.BasisXform(picture);
            }
        }

        if (Dither is not null)
        {
            Dither.Visible = Enabled;

            if (Dither.Material is ShaderMaterial material)
            {
                material.SetShaderParameter(ColorLevelsKey, (float)ColorLevels);
                material.SetShaderParameter(DitherKey, Dithering ? DitherStrength : 0f);
            }
        }

        TuneTree(World ?? GetParent());
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

        if (UnitOutline.MaterialOverride is ShaderMaterial material)
            material.SetShaderParameter(OutlineWidthKey, Math.Max(1, OutlineWidth));
    }

    //In echten Pixeln des Fensters, damit jede Zelle gleich groß ist. Die Leinwand der Oberfläche wäre gestreckt
    private PixelSize GetWindowPixels()
    {
        var pixels = GetWindow()?.Size ?? Vector2I.Zero;

        return pixels.X > 0 && pixels.Y > 0 ? new PixelSize(pixels.X, pixels.Y) : FallbackWindow;
    }

    private void OnNodeAdded(Node node)
    {
        if (node is not (MeshInstance3D or Light3D) && !node.IsInGroup(BlobShadowGroup))
            return;

        Callable.From(() =>
        {
            if (IsInstanceValid(node) && node.IsInsideTree())
                Tune(node);
        }).CallDeferred();
    }

    private void TuneTree(Node node)
    {
        Tune(node);

        foreach (var child in node.GetChildren())
            TuneTree(child);
    }

    private void Tune(Node node)
    {
        if (node.IsInGroup(BlobShadowGroup) && node is Node3D blobShadow)
            blobShadow.Visible = Enabled && !RealShadows;

        switch (node)
        {
            case MeshInstance3D mesh:
                for (var surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
                    Tune(mesh.GetActiveMaterial(surface));

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

    private void Tune(Material material)
    {
        if (material is not ShaderMaterial surface || !IsPs1Shader(surface.Shader))
            return;

        surface.SetShaderParameter(Snap, Enabled && SnapVertices ? 1f : 0f);
        surface.SetShaderParameter(SnapResolution, snapResolution);
        surface.SetShaderParameter(Affine, Enabled ? AffineTextures : 0f);
    }

    private bool IsPs1Shader(Shader shader)
        => shader == SurfaceShader || shader == ObjectOutline.Shader || shader == WallFade.MasonryShader || shader == WallFade.MarkShader || shader == LevelMarks.FloorShader || UnitSight.IsUnitShader(shader);
}
