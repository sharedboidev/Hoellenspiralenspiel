using System.Linq;
using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Core.Enemies;

namespace Hoellenspiralenspiel.Resources.MonsterMods;

[GlobalClass]
public partial class MonsterModResource : Resource
{
    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [ExportGroup("Rolling")]
    [Export]
    public float Weight { get; set; } = 1f;

    [Export]
    public int MinLevel { get; set; } = 1;

    //Von Mods derselben Gruppe bekommt ein Monster höchstens einen
    [Export]
    public string ExclusiveGroup { get; set; } = string.Empty;

    [Export]
    public MonsterModFit Fit { get; set; }

    [ExportGroup("Effects")]
    [Export]
    public Array<StatModifierResource> Modifiers { get; set; } = new();

    [Export]
    public Array<MonsterModEffect> Effects { get; set; } = new();

    //Die Definition entsteht beim ersten Zugriff. Wer danach Werte der Resource ändert, sieht davon nichts
    public MonsterModDefinition Definition => field ??= new MonsterModDefinition(Id, DisplayName)
    {
        Weight         = Weight,
        MinLevel       = MinLevel,
        ExclusiveGroup = ExclusiveGroup ?? string.Empty,
        Fit            = Fit,
        Modifiers      = Modifiers.Where(modifier => modifier is not null).Select(modifier => modifier.ToModifier()).ToArray()
    };
}
