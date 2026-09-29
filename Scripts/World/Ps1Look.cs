using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.World;

public partial class Ps1Look : Node
{
    public delegate void ChangedEventHandler();

    private const string BlobShadowGroup = "blob_shadows";

    private static readonly int[] LineSteps = [240, 360, 480];

    private static readonly StringName Snap            = "snap";
    private static readonly StringName SnapResolution  = "snap_resolution";
    private static readonly StringName Affine          = "affine";
    private static readonly StringName Resolution      = "resolution";
    private static readonly StringName ColorLevelsKey  = "color_levels";
    private static readonly StringName DitherKey       = "dither";
    private static readonly StringName OutlineWidthKey = "outline_width";

    private readonly Dictionary<Light3D, bool> shadowOfLight = new();
    private          int                       outlineWidth  = 1;

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
        ApplyOutline();

        Changed?.Invoke();
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
        => shader == SurfaceShader || shader == WallFade.MasonryShader || UnitSight.IsUnitShader(shader);
}
