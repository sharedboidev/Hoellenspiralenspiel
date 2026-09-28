using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Triggers;

[GlobalClass]
public partial class OnDamageTakenTrigger : ModTrigger
{
    [Export]
    public bool OnlyLandedHits { get; set; } = true;

    public override ModTriggerBinding Bind(Enemy owner, Action<BaseUnit, HitResult> fire)
        => new Binding(owner, fire, OnlyLandedHits);

    private sealed class Binding : ModTriggerBinding
    {
        private readonly Action<BaseUnit, HitResult> fire;
        private readonly bool                        onlyLandedHits;
        private readonly Enemy                       owner;

        public Binding(Enemy owner, Action<BaseUnit, HitResult> fire, bool onlyLandedHits)
        {
            this.owner          = owner;
            this.fire           = fire;
            this.onlyLandedHits = onlyLandedHits;

            owner.DamageTaken += OnDamageTaken;
        }

        public override void Release()
            => owner.DamageTaken -= OnDamageTaken;

        private void OnDamageTaken(BaseUnit victim, HitResult hit, BaseUnit attacker)
        {
            if (hit.HasLanded || !onlyLandedHits)
                fire(attacker, hit);
        }
    }
}
