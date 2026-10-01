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

    [ExportGroup("Kreis")]
    //1 bis 9. Der Kreis ist offen, sobald der Held so viele Kreise freigeschaltet hat. 0 ist nur für einen Testkreis
    [Export(PropertyHint.Range, "0,9,1")]
    public int Number { get; set; } = 1;

    //Ein Testkreis steht außerhalb der Kette der neun Kreise: sein Portal im Hub ist immer offen, sein Boss schaltet nichts frei
    [Export]
    public bool IsTestCircle { get; set; }

    //Wer Räume, Gegner oder Aufbau des Kreises ändert, zählt hier hoch. Ein gespeicherter Abstieg mit anderem Stand beginnt neu, die Checkpoints bleiben
    [Export(PropertyHint.Range, "1,1000,1")]
    public int ContentVersion { get; set; } = 1;

    [Export(PropertyHint.Range, "1,20,1")]
    public int LevelCount { get; set; } = 4;

    //Bereichslevel der ersten Ebene, jede weitere liegt eins höher
    [Export]
    public int FirstAreaLevel { get; set; } = 1;

    [ExportGroup("Inhalt")]
    [Export]
    public Array<PackedScene> Rooms { get; set; } = new();

    [Export]
    public Array<EnemyPoolEntry> Enemies { get; set; } = new();

    [Export]
    public AudioStream Music { get; set; }

    //Bilder an Mauern und auf dem Boden, etwa Blutspuren. Der Aufbau der Ebene verteilt sie nach dem Seed der Ebene
    [ExportGroup("Spuren")]
    [Export]
    public Array<LevelMarkResource> Marks { get; set; } = new();

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
