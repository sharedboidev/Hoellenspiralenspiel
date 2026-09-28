using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Resources.MonsterMods.Triggers;

[GlobalClass]
public partial class IntervalTrigger : ModTrigger
{
    [Export]
    public double PeriodSec { get; set; } = 2;

    [Export]
    public bool OnlyInCombat { get; set; } = true;

    public override ModTriggerBinding Bind(Enemy owner, Action<BaseUnit, HitResult> fire)
        => new Binding(owner, fire, PeriodSec, OnlyInCombat);

    private sealed class Binding(Enemy owner, Action<BaseUnit, HitResult> fire, double periodSec, bool onlyInCombat) : ModTriggerBinding
    {
        private readonly RepeatingTimer timer = new(periodSec);

        public override void Advance(double deltaSec)
        {
            if (onlyInCombat && !owner.IsInCombat)
            {
                timer.Reset();

                return;
            }

            if (timer.Advance(deltaSec))
                fire(null, null);
        }
    }
}
