using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;

namespace Hoellenspiralenspiel.Scripts.World.Levels;

//Wurzel eines handgebauten Orts wie dem Hub. Er wird wie eine Ebene unter den Knoten der Welt gehängt
public partial class Place : Node3D
{
    private const string TownPortalSpotName = "TownPortalSpot";

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export]
    public int AreaLevel { get; set; } = 1;

    //Nur im Hub steht das Gegenstück zum Town-Portal
    [Export]
    public bool HasTownPortal { get; set; }

    public Vector3 HeroStart => this.GetAllChildren<HeroStart>().FirstOrDefault()?.GlobalPosition ?? GlobalPosition;

    public Vector3 TownPortalSpot => GetNodeOrNull<Node3D>(TownPortalSpotName)?.GlobalPosition ?? HeroStart;
}
