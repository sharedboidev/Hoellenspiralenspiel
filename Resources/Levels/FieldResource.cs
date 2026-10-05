using Godot;
using Godot.Collections;

namespace Hoellenspiralenspiel.Resources.Levels;

//Eine freie Fläche eines Kreises: welche Vorlagen auf ihr stehen, wer auf ihr wohnt und wie sie aussieht.
//Die Rolle einer Vorlage ergibt sich aus ihrem Platz hier, nicht aus ihrer Szene
[GlobalClass]
public partial class FieldResource : Resource
{
    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [ExportGroup("Vorlagen")]
    [Export]
    public Array<PackedScene> Ruins { get; set; } = new();

    //Der Eingang in den Dungeon der Fläche: Falltür, Höhle oder Gebäude
    [Export]
    public PackedScene Entrance { get; set; }

    //Auf die Fläche kommt nur das Event, das der Plan des Abstiegs ihr gibt
    [Export]
    public Array<PackedScene> Events { get; set; } = new();

    //Steht auf der letzten Fläche eines Kreises statt des Ausgangs
    [Export]
    public PackedScene Arena { get; set; }

    [ExportGroup("Inhalt")]
    //Leer nimmt die Fläche den Pool ihres Kreises
    [Export]
    public Array<EnemyPoolEntry> Enemies { get; set; } = new();

    //Felsen, tote Bäume und Knochen für die Zellen mit Hindernis. Jede Szene braucht eine Kollision auf der Ebene Walls
    [Export]
    public Array<PackedScene> Props { get; set; } = new();

    //Leer spielt die Musik des Kreises
    [Export]
    public AudioStream Music { get; set; }

    [ExportGroup("Bau")]
    //Leer liegt der Boden des Kreises
    [Export]
    public Texture2D GroundTexture { get; set; }

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

    //Ab der Kamera gemessen, wie im WorldEnvironment des Spiels
    [Export]
    public float FogDepthBegin { get; set; } = 31.7f;

    [Export]
    public float FogDepthEnd { get; set; } = 55.7f;
}
