using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Resources.MonsterMods;

[GlobalClass]
public abstract partial class ModAction : Resource
{
    public abstract void Run(ModContext context);
}

//Other ist der Angreifer oder das Opfer aus dem Auslöser und fehlt bei Auslösern ohne Gegenüber
public sealed record ModContext(Enemy Owner, BaseUnit Other, HitResult Hit)
{
    public BaseUnit OtherOrTarget => GodotObject.IsInstanceValid(Other) ? Other : Owner.Target;
}

//Neue Werte nur am Ende anhängen: Resources speichern die Auswahl als Zahl
public enum ModAim
{
    Self,
    Target,
    Other,
    RandomPointAroundSelf,
    RandomPointAroundTarget
}
