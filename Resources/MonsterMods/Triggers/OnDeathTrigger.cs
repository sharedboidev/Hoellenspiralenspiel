using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Triggers;

[GlobalClass]
public partial class OnDeathTrigger : ModTrigger
{
    public override ModTriggerBinding Bind(Enemy owner, Action<BaseUnit, HitResult> fire)
        => new Binding(owner, fire);

    private sealed class Binding : ModTriggerBinding
    {
        private readonly Action<BaseUnit, HitResult> fire;
        private readonly Enemy                       owner;

        public Binding(Enemy owner, Action<BaseUnit, HitResult> fire)
        {
            this.owner = owner;
            this.fire  = fire;

            owner.Died += OnDied;
        }

        public override void Release()
            => owner.Died -= OnDied;

        private void OnDied(BaseUnit unit)
            => fire(owner.LastAttacker, null);
    }
}
