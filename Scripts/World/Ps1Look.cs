using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.World;

//Wie sich Held und Gegner von der Umgebung abheben. F6 schaltet durch, zum Vergleich
public enum UnitContrast
{
    Off,
    Outline,
    HoverOutline,
    Rim,
    Brightness,
    OutlineAndRim,
    All
}

public partial class Ps1Look : Node
{
    public delegate void ChangedEventHandler();

    private const string BlobShadowGroup = "blob_shadows";

    private static readonly int[] LineSteps = [240, 360, 480];

    private static readonly StringName Snap           = "snap";
    private static readonly StringName SnapResolution = "snap_resolution";
    private static readonly StringName Affine         = "affine";
    private static readonly StringName Resolution     = "resolution";
    private static readonly StringName ColorLevelsKey = "color_levels";
    private static readonly StringName DitherKey      = "dither";

    private static readonly StringName UnitOutlineKey         = "unit_outline";
    private static readonly StringName UnitHoverOutlineKey    = "unit_hover_outline";
    private static readonly StringName UnitRimKey             = "unit_rim";
    private static readonly StringName EnvironmentContrastKey = "environment_contrast";

    private readonly Dictionary<Light3D, bool> shadowOfLight = new();

    [Export]
    public bool Enabled { get; set; } = true;

    //Die PS1 zeigte 240 Zeilen. Die Breite folgt aus dem Seitenverhältnis des Fensters
    [Export]
    public int Lines { get; set; } = 240;

    [Export]
    public int ColorLevels { get; set; } = 32;

    [Export(PropertyHint.Range, "0,2,0.05")]
    public float Dither { get; set; } = 1f;

    [Export]
    public bool SnapVertices { get; set; } = true;

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float AffineTextures { get; set; } = 1f;

    //Die PS1 kannte keine Schatten aus Lichtern, Figuren standen auf dunklen Scheiben
    [Export]
    public bool RealShadows { get; set; }

    [Export]
    public UnitContrast Contrast { get; set; } = UnitContrast.Outline;

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float RimStrength { get; set; } = 0.5f;

    [Export]
    public Shader SurfaceShader { get; set; }

    [Export]
    public Shader UnitShader { get; set; }

    [Export]
    public Shader UnitCutoutShader { get; set; }

    //Das Rechteck über dem Bild, das den Rand um die Figuren zieht
    [Export]
    public GeometryInstance3D UnitOutline { get; set; }

    [Export]
    public ColorRect Screen { get; set; }

    [Export]
    public Node World { get; set; }

    public event ChangedEventHandler Changed;

    public override void _Ready()
    {
        GetTree().NodeAdded        += OnNodeAdded;
        GetViewport().SizeChanged += Apply;

        //Erst nach dem Aufbau der Szene hat jeder Gegner sein eigenes Material
        Callable.From(Apply).CallDeferred();
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded        -= OnNodeAdded;
        GetViewport().SizeChanged -= Apply;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        switch (key.Keycode)
        {
            case Key.F1:
                Enabled = !Enabled;

                break;
            case Key.F3:
                Lines = LineSteps[(Array.IndexOf(LineSteps, Lines) + 1) % LineSteps.Length];

                break;
            case Key.F4:
                RealShadows = !RealShadows;

                break;
            case Key.F6:
                Contrast = (UnitContrast)(((int)Contrast + 1) % Enum.GetValues<UnitContrast>().Length);

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
                screenMaterial.SetShaderParameter(DitherKey, Dither);
            }
        }

        TuneTree(World ?? GetParent(), resolution);
        ApplyContrast(resolution);

        Changed?.Invoke();
    }

    public string DescribeContrast()
        => Contrast switch
        {
            UnitContrast.Outline       => "Umriss",
            UnitContrast.HoverOutline  => "Umriss beim Anvisieren",
            UnitContrast.Rim           => "Randlicht",
            UnitContrast.Brightness    => "Helligkeitskontrast",
            UnitContrast.OutlineAndRim => "Umriss und Randlicht",
            UnitContrast.All           => "alles zusammen",
            _                          => "ohne Kontrasthilfe"
        };

    private void ApplyContrast(Vector2 resolution)
    {
        var outline = Contrast is UnitContrast.Outline or UnitContrast.OutlineAndRim or UnitContrast.All;
        var hover   = outline || Contrast == UnitContrast.HoverOutline;
        var rim     = Contrast is UnitContrast.Rim or UnitContrast.OutlineAndRim or UnitContrast.All;
        var muted   = Contrast is UnitContrast.Brightness or UnitContrast.All;

        RenderingServer.GlobalShaderParameterSet(UnitOutlineKey, outline ? 1f : 0f);
        RenderingServer.GlobalShaderParameterSet(UnitHoverOutlineKey, hover ? 1f : 0f);
        RenderingServer.GlobalShaderParameterSet(UnitRimKey, rim ? RimStrength : 0f);
        RenderingServer.GlobalShaderParameterSet(EnvironmentContrastKey, muted ? 1f : 0f);

        if (UnitOutline is null)
            return;

        //Ohne Rand liest niemand den Rauheitskanal, dann spart Godot sich dessen Aufbau
        UnitOutline.Visible = hover;

        (UnitOutline.MaterialOverride as ShaderMaterial)?.SetShaderParameter(SnapResolution, resolution);
    }

    private Vector2 GetResolution()
    {
        var size = GetViewport().GetVisibleRect().Size;

        return size.Y <= 0 ? new Vector2(Lines * 16f / 9f, Lines) : new Vector2(Mathf.Round(Lines * size.X / size.Y), Lines);
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
                if (!shadowOfLight.TryGetValue(light, out var hadShadow))
                    shadowOfLight[light] = hadShadow = light.ShadowEnabled;

                //Kein Licht scheint durch Mauern. Die Schatten von allem anderen wirft ein Licht nur, wenn es das von sich aus tut und der Look sie zulässt
                light.ShadowEnabled    = true;
                light.ShadowCasterMask = hadShadow && (!Enabled || RealShadows) ? uint.MaxValue : WallSegment.ShadowLayer;

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
        => shader == SurfaceShader || shader == UnitShader || shader == UnitCutoutShader || shader == WallFade.MasonryShader;
}
