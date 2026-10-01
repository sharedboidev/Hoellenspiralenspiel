using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;

namespace Hoellenspiralenspiel.Resources.Levels;

//Eine Spur an den Mauern oder auf dem Boden eines Kreises, etwa ein blutiger Handabdruck. Der Aufbau der Ebene verteilt sie nach Seed
[GlobalClass]
public partial class LevelMarkResource : Resource
{
    //Ein Bild mit Alphakanal, der Rest der Fläche bleibt durchsichtig
    [Export]
    public Texture2D Texture { get; set; }

    [Export]
    public MarkPlace Place { get; set; } = MarkPlace.Wall;

    [Export]
    public float WidthMeters { get; set; } = 1f;

    [Export]
    public float HeightMeters { get; set; } = 1f;

    //An der Mauer je Zelle und Seite mit Boden davor, auf dem Boden je Versuch auf der Ebene. Ein Begleiter würfelt je Zelle neben seiner Spur
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Chance { get; set; } = 0.05f;

    //0 erlaubt die Spur beliebig oft je Ebene, auf dem Boden dann einen Versuch
    [Export]
    public int MaxPerLevel { get; set; }

    //Gesetzt, erscheint die Spur nie allein, sondern nur neben dieser Spur an der Mauer: in deren Zelle und den beiden Nachbarzellen
    [Export]
    public LevelMarkResource Near { get; set; }

    //Nur am Boden: auch in den Zellen der Gänge, nicht nur an den Plätzen, die die Räume anbieten
    [Export]
    public bool InCorridors { get; set; }

    [ExportGroup("Mauer")]
    //Höhe der Mitte über dem Boden, gewürfelt zwischen beiden Werten. Der Sockel reicht bis 0,6 m, das Mauerwerk bis 2,5 m
    [Export]
    public float MinCenterHeight { get; set; } = 1.2f;

    [Export]
    public float MaxCenterHeight { get; set; } = 1.6f;

    public MarkRule ToRule(string id, string nearId)
        => new()
        {
            Id              = id,
            Place           = Place,
            WidthMeters     = WidthMeters,
            HeightMeters    = HeightMeters,
            Chance          = Chance,
            MaxPerLevel     = MaxPerLevel,
            MinCenterHeight = MinCenterHeight,
            MaxCenterHeight = MaxCenterHeight,
            Near            = nearId ?? string.Empty,
            InCorridors     = InCorridors
        };
}
