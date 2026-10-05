using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Extensions;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Wurzel einer handgebauten Raumszene. Der Ursprung ist die Mitte des Raums, Boden und Mauern entstehen beim Aufbau der Ebene
public partial class RoomTemplate : Node3D
{
    public const string PreviewName = "EditorPreview";

    [Export]
    public int WidthCells { get; set; } = 3;

    [Export]
    public int HeightCells { get; set; } = 3;

    [Export]
    public RoomRole Role { get; set; }

    [Export]
    public float Weight { get; set; } = 1f;

    [Export]
    public int MinAreaLevel { get; set; } = 1;

    //Ein Pflichtraum erscheint in jeder Ebene genau einmal
    [Export]
    public bool IsRequired { get; set; }

    [Export]
    public bool CanRotate { get; set; } = true;

    //0 erlaubt die Vorlage beliebig oft
    [Export]
    public int MaxPerLevel { get; set; }

    //Nur auf Flächen: Die Vorlage bringt ihre Mauern selbst mit, der Aufbau zieht keine um sie herum und gibt ihr keine Zone.
    //Jede offene Kante braucht dann eine Tür, denn der Generator geht nur durch Türen hinein
    [Export]
    public bool OpenToField { get; set; }

    public RoomBlueprint ToBlueprint(string id)
        => new()
        {
            Id           = id,
            Width        = Math.Max(1, WidthCells),
            Height       = Math.Max(1, HeightCells),
            Role         = Role,
            Weight       = Weight,
            MinAreaLevel = MinAreaLevel,
            IsRequired   = IsRequired,
            CanRotate    = CanRotate,
            MaxPerLevel  = MaxPerLevel,
            OpenToField  = OpenToField,
            Doors        = ReadDoors()
        };

    private List<DoorSpot> ReadDoors()
    {
        var doors = new List<DoorSpot>();

        foreach (var door in this.GetAllChildren<RoomDoor>())
        {
            var spot = GetSpotOf(GetPlaceInRoom(door));

            if (!doors.Contains(spot))
                doors.Add(spot);
        }

        return doors;
    }

    //Die Vorlage hängt beim Auslesen noch nicht im Szenenbaum, globale Positionen gibt es deshalb nicht
    private Vector3 GetPlaceInRoom(Node3D node)
    {
        var place = node.Transform;

        for (var parent = node.GetParent(); parent is Node3D between && parent != this; parent = parent.GetParent())
            place = between.Transform * place;

        return place.Origin;
    }

    //Eine Tür gehört zu der Seite, deren Rand ihr am nächsten liegt
    private DoorSpot GetSpotOf(Vector3 place)
    {
        var halfWidth  = WidthCells * LevelGrid.CellMeters / 2f;
        var halfHeight = HeightCells * LevelGrid.CellMeters / 2f;
        var nearest    = CellSide.North;
        var shortest   = Math.Abs(place.Z + halfHeight);

        foreach (var (side, distance) in new[]
                 {
                     (CellSide.East, Math.Abs(place.X - halfWidth)),
                     (CellSide.South, Math.Abs(place.Z - halfHeight)),
                     (CellSide.West, Math.Abs(place.X + halfWidth))
                 })
        {
            if (distance >= shortest)
                continue;

            nearest  = side;
            shortest = distance;
        }

        var offset = nearest.IsAlongX()
                             ? Math.Clamp(Mathf.FloorToInt((place.X + halfWidth) / LevelGrid.CellMeters), 0, WidthCells - 1)
                             : Math.Clamp(Mathf.FloorToInt((place.Z + halfHeight) / LevelGrid.CellMeters), 0, HeightCells - 1);

        return new DoorSpot(nearest, offset);
    }
}
