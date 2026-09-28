using Godot;
using Godot.Collections;

namespace Hoellenspiralenspiel.Resources.MonsterMods;

//Ein Auslöser und die Aktionen, die er startet. Jeder Auslöser lässt sich mit jeder Aktion kombinieren
[GlobalClass]
public partial class MonsterModEffect : Resource
{
    [Export]
    public ModTrigger Trigger { get; set; }

    [Export]
    public Array<ModAction> Actions { get; set; } = new();

    [Export(PropertyHint.Range, "0,100,0.1")]
    public float ChancePercent { get; set; } = 100f;

    [Export]
    public double CooldownSec { get; set; }

    //Erscheint als Schrift über dem Monster, wenn der Effekt auslöst
    [Export]
    public string Announcement { get; set; } = string.Empty;
}
