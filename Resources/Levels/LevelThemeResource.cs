using Godot;
using Godot.Collections;

namespace Hoellenspiralenspiel.Resources.Levels;

//Das Thema eines Höllenkreises: woraus seine Ebenen gebaut sind, wie sie aussehen und klingen und wer darin wohnt
[GlobalClass]
public partial class LevelThemeResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public Array<PackedScene> Rooms { get; set; } = new();

    [Export]
    public Array<EnemyPoolEntry> Enemies { get; set; } = new();

    [Export]
    public AudioStream Music { get; set; }

    [ExportGroup("Bau")]
    [Export]
    public Texture2D FloorTexture { get; set; }

    [Export]
    public Texture2D WallTexture { get; set; }

    [Export]
    public float WallHeight { get; set; } = 2.5f;

    [Export]
    public float PlinthHeight { get; set; } = 0.6f;

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float PlinthBrightness { get; set; } = 0.55f;

    [ExportGroup("Licht")]
    [Export]
    public Color AmbientColor { get; set; } = new(0.6f, 0.45f, 0.4f);

    [Export]
    public float AmbientEnergy { get; set; } = 0.22f;

    [Export]
    public Color FogColor { get; set; } = new(0.05f, 0.025f, 0.02f);

    [Export]
    public Color MoonlightColor { get; set; } = new(0.75f, 0.5f, 0.45f);

    [Export]
    public float MoonlightEnergy { get; set; } = 0.12f;
}
