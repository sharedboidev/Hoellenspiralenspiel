using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Scripts.UI;

namespace Hoellenspiralenspiel.Scripts.Units.Enemies;

public sealed class MonsterModRuntime
{
    private const int AnnouncementFontSize = 26;

    private static readonly Color AnnouncementColor = new(1f, 0.82f, 0.25f);

    private static long lastBuffNumber;

    private readonly List<RunningEffect> effects = new();
    private readonly Enemy               owner;
    private readonly List<TimedBuff>     timedBuffs = new();

    public MonsterModRuntime(Enemy owner, IReadOnlyList<MonsterModResource> mods)
    {
        this.owner = owner;

        foreach (var mod in mods)
        {
            foreach (var effect in mod.Effects)
            {
                if (effect?.Trigger is not null)
                    effects.Add(new RunningEffect(owner, effect));
            }
        }
    }

    public void Advance(double deltaSec)
    {
        foreach (var effect in effects)
            effect.Advance(deltaSec);

        for (var i = timedBuffs.Count - 1; i >= 0; i--)
        {
            timedBuffs[i].SecLeft -= deltaSec;

            if (timedBuffs[i].SecLeft > 0)
                continue;

            owner.Stats.RemoveModifiersOf(timedBuffs[i].OriginId);

            timedBuffs.RemoveAt(i);
        }
    }

    public void AddModifiers(IEnumerable<StatModifierResource> modifiers, double durationSec)
    {
        var originId = $"monster-buff:{++lastBuffNumber}";
        var stamped  = new List<CombatStatModifier>();

        foreach (var modifier in modifiers)
        {
            if (modifier is not null)
                stamped.Add(modifier.ToModifier(originId));
        }

        if (stamped.Count == 0)
            return;

        owner.Stats.AddModifiers(stamped);

        if (durationSec > 0)
            timedBuffs.Add(new TimedBuff(originId) { SecLeft = durationSec });
    }

    public void Release()
    {
        foreach (var effect in effects)
            effect.Release();

        effects.Clear();
    }

    private sealed class TimedBuff(string originId)
    {
        public string OriginId { get; } = originId;
        public double SecLeft  { get; set; }
    }

    private sealed class RunningEffect
    {
        private readonly ModTriggerBinding binding;
        private readonly MonsterModEffect  effect;
        private readonly TriggerGate       gate;
        private readonly Enemy             owner;

        public RunningEffect(Enemy owner, MonsterModEffect effect)
        {
            this.owner  = owner;
            this.effect = effect;

            gate    = new TriggerGate(effect.ChancePercent, effect.CooldownSec);
            binding = effect.Trigger.Bind(owner, Fire);
        }

        public void Advance(double deltaSec)
        {
            gate.Advance(deltaSec);
            binding.Advance(deltaSec);
        }

        public void Release()
            => binding.Release();

        private void Fire(BaseUnit other, HitResult hit)
        {
            if (!IsOwnerUsable() || !gate.TryPass(GameRandom.Shared))
                return;

            if (!string.IsNullOrEmpty(effect.Announcement))
                CombatText.Show(owner, effect.Announcement, AnnouncementColor, AnnouncementFontSize);

            //Ein Treffer wird mitten im Physikschritt gemeldet, neue Projektile und Monster dürfen erst danach entstehen
            Callable.From(() => Run(new ModContext(owner, other, hit))).CallDeferred();
        }

        private void Run(ModContext context)
        {
            if (!IsOwnerUsable())
                return;

            foreach (var action in effect.Actions)
                action?.Run(context);
        }

        private bool IsOwnerUsable()
            => GodotObject.IsInstanceValid(owner) && owner.IsInsideTree();
    }
}
