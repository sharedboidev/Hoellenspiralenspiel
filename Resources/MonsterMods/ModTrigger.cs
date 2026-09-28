using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Resources.MonsterMods;

//Eine Resource gehört allen Monstern mit diesem Mod gemeinsam. Was pro Monster läuft, steht deshalb in der Bindung
[GlobalClass]
public abstract partial class ModTrigger : Resource
{
    public abstract ModTriggerBinding Bind(Enemy owner, Action<BaseUnit, HitResult> fire);
}

public abstract class ModTriggerBinding
{
    public virtual void Advance(double deltaSec) { }

    public virtual void Release() { }
}
