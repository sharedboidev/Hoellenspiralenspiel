using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Tests.Balance;

internal readonly record struct FightResult(bool Killed, double Seconds, int Actions, int Landed, int HitDamage, int EffectDamage);

internal readonly record struct FightSummary(int Runs, int Kills, double MeanSeconds, double MinSeconds, double MaxSeconds, double MeanActions)
{
    public bool AllKilled => Kills == Runs;
}

//Ein Angreifer schlägt auf einen Verteidiger ein, der stillhält, bis der Verteidiger fällt oder die Zeit abläuft.
//Gerechnet wird in Schritten der Physik wie im Spiel: Leben regeneriert, Effekte ticken im Takt von 0,5 s, Abklingzeiten und Mana laufen.
//Nicht enthalten: Laufwege, Flugzeit von Projektilen, Verzögerung von Flächen, mehrere Ziele, Effekte der Mods
internal static class FightSimulator
{
    public const double StepSec         = 1.0 / 60;
    public const double DefaultLimitSec = 600;

    public static FightResult Run(Fighter attacker, Fighter defender, int seed, double limitSec = DefaultLimitSec)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);

        var attackerStats = attacker.CreateStats();
        var defenderStats = defender.CreateStats();
        var effects       = new StatusEffectTracker(defenderStats);
        var ticks         = new List<StatusTick>();
        var cooldowns     = new SkillCooldowns();
        var cycle         = new AttackCycle();
        var random        = new SeededRandom(seed);
        var life          = (float)defenderStats.GetFinalWhole(CombatStat.Life);
        var mana          = (float)attackerStats.GetFinalWhole(CombatStat.Mana);
        var steps         = (int)Math.Ceiling(limitSec / StepSec);
        var actions       = 0;
        var landed        = 0;
        var hitDamage     = 0;
        var effectDamage  = 0;

        SkillDefinition acting = null;

        for (var step = 1; step <= steps; step++)
        {
            life = Regenerate(life, defenderStats);

            if (effects.HasAny)
            {
                ticks.Clear();
                effects.Advance(StepSec, ticks);

                foreach (var tick in ticks)
                {
                    life         -= tick.Damage;
                    effectDamage += tick.Damage;
                }
            }

            if (life <= 0)
                return new FightResult(true, step * StepSec, actions, landed, hitDamage, effectDamage);

            if (cooldowns.HasAny)
                cooldowns.Advance(StepSec);

            if (attacker.PaysMana)
                mana = Math.Min(attackerStats.GetFinalWhole(CombatStat.Mana), mana + attackerStats.GetFinal(CombatStat.Manaregeneration) * (float)StepSec);

            if (cycle.IsReady && Choose(attacker, cooldowns, mana) is { } skill)
            {
                if (attacker.PaysMana)
                    mana -= skill.ManaCost;

                cooldowns.Start(skill.Id, attacker.GetCooldownSec(skill));

                var (windupSec, recoverySec) = attacker.GetActionTiming(attackerStats, skill);

                cycle.Start(windupSec, recoverySec);

                acting = skill;
                actions++;
            }

            if (!cycle.Advance(StepSec) || acting is null)
                continue;

            var hit = HitResolver.Resolve(HitRequests.ForSkill(attackerStats, attacker.Weapon, acting), defenderStats, random);

            if (!hit.HasLanded)
                continue;

            landed++;
            hitDamage += hit.FinalDamage;
            life      -= hit.FinalDamage;

            if (life <= 0)
                return new FightResult(true, step * StepSec, actions, landed, hitDamage, effectDamage);

            foreach (var effect in hit.InflictedEffects)
                effects.Apply(effect);
        }

        return new FightResult(false, steps * StepSec, actions, landed, hitDamage, effectDamage);
    }

    public static FightSummary Summarize(Fighter attacker, Fighter defender, int runs = 200, int firstSeed = 1, double limitSec = DefaultLimitSec)
    {
        var results = Enumerable.Range(firstSeed, runs).Select(seed => Run(attacker, defender, seed, limitSec)).ToList();

        return new FightSummary(runs,
                                results.Count(result => result.Killed),
                                results.Average(result => result.Seconds),
                                results.Min(result => result.Seconds),
                                results.Max(result => result.Seconds),
                                results.Average(result => result.Actions));
    }

    //Wie BaseUnit.RegenerateLife: ganze Punkte je Sekunde, nie über das Maximum
    private static float Regenerate(float life, StatSheet stats)
    {
        var maximum      = stats.GetFinalWhole(CombatStat.Life);
        var regeneration = stats.GetFinalWhole(CombatStat.Liferegeneration);

        if (regeneration <= 0 || life >= maximum)
            return life;

        return Math.Min(maximum, life + regeneration * (float)StepSec);
    }

    private static SkillDefinition Choose(Fighter attacker, SkillCooldowns cooldowns, float mana)
    {
        var availableMana = attacker.PaysMana ? mana : SkillGate.UnlimitedMana;

        return attacker.Skills.FirstOrDefault(skill => SkillGate.Check(skill, cooldowns, availableMana) == SkillUseCheck.Ready);
    }
}
