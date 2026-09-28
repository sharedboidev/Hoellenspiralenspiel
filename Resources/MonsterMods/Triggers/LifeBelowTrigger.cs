using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Triggers;

[GlobalClass]
public partial class LifeBelowTrigger : ModTrigger
{
    [Export(PropertyHint.Range, "0,100,1")]
    public float Percent { get; set; } = 50f;

    //Löst erneut aus, wenn das Leben zwischendurch wieder über die Schwelle gestiegen ist
    [Export]
    public bool Rearms { get; set; }

    public override ModTriggerBinding Bind(Enemy owner, Action<BaseUnit, HitResult> fire)
        => new Binding(owner, fire, Percent / 100f, Rearms);

    private sealed class Binding(Enemy owner, Action<BaseUnit, HitResult> fire, float fraction, bool rearms) : ModTriggerBinding
    {
        private readonly ThresholdLatch latch = new(fraction, rearms);

        public override void Advance(double deltaSec)
        {
            if (owner.IsDead || owner.LifeMaximum <= 0)
                return;

            if (latch.Update(owner.LifeCurrent / owner.LifeMaximum))
                fire(owner.LastAttacker, null);
        }
    }
}
