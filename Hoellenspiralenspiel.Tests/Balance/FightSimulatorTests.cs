using System;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Balance;

[TestFixture]
public class FightSimulatorTests
{
    //Blitz trifft voll: keine Rüstung dazwischen, kein Effekt, der Schaden macht, kein Krit ohne Krit-Chance
    private static readonly EnemyDefinition Striker = new("striker", "Striker")
    {
        DamageMin         = 10f,
        DamageMax         = 10f,
        DamageType        = DamageType.Lightning,
        CriticalHitChance = 0f
    };

    //Weicht nicht aus und regeneriert mit Stärke 1 und Konstitution 1 nichts
    private static readonly EnemyDefinition Dummy = new("dummy", "Dummy")
    {
        Dodge     = 0,
        LifeBonus = 41
    };

    [Test]
    public void GleicherSeed_ErgibtDenselbenKampf()
    {
        var hero  = BalanceReportTests.HeroAt(1);
        var enemy = Fighter.Enemy(GameData.Enemy("skeleton"), 1);

        Assert.That(FightSimulator.Run(hero, enemy, 7), Is.EqualTo(FightSimulator.Run(hero, enemy, 7)));
    }

    [Test]
    public void FesterSchaden_BrauchtSoVieleSchlaegeWieDasLebenVerlangt_ImTaktDesGegners()
    {
        var attacker = Fighter.Enemy(Striker, 1);
        var defender = Fighter.Enemy(Dummy, 1);
        var life     = defender.CreateStats().GetFinalWhole(CombatStat.Life);
        var rate     = attacker.CreateStats().GetTotalMultiplier(CombatStat.Attackspeed);
        var hits     = (int)Math.Ceiling(life / 10.0);
        var expected = (hits - 1) * (Striker.AttackWindupSec + Striker.AttackRecoverySec) / rate + Striker.AttackWindupSec / rate;

        var result = FightSimulator.Run(attacker, defender, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Killed, Is.True);
            Assert.That(result.Landed, Is.EqualTo(hits));
            Assert.That(result.HitDamage, Is.EqualTo(hits * 10));
            Assert.That(result.Seconds, Is.EqualTo(expected).Within((hits + 1) * FightSimulator.StepSec), "Je Handlung höchstens ein Schritt Versatz, wie im Spiel");
        });
    }

    //Konstitution 30 regeneriert 10 Leben je Sekunde, der Angreifer macht rund 20 Schaden je Sekunde
    [Test]
    public void Lebensregeneration_HeiltZwischenDenTreffern()
    {
        var defender = Fighter.Enemy(Dummy with { Constitution = new AttributeGrowth(30) }, 1);
        var life     = defender.CreateStats().GetFinalWhole(CombatStat.Life);

        var result = FightSimulator.Run(Fighter.Enemy(Striker, 1), defender, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Killed, Is.True);
            Assert.That(result.HitDamage, Is.GreaterThan(life * 1.5f));
        });
    }

    [Test]
    public void Bleed_VerkuerztDenKampf_UndZaehltAlsEffektschaden()
    {
        var defender  = Fighter.Enemy(Dummy with { Armor = -1000 }, 1);
        var lightning = FightSimulator.Run(Fighter.Enemy(Striker, 1), defender, 1);
        var slash     = FightSimulator.Run(Fighter.Enemy(Striker with { DamageType = DamageType.Slash }, 1), defender, 1);

        Assert.Multiple(() =>
        {
            Assert.That(slash.EffectDamage, Is.GreaterThan(0));
            Assert.That(lightning.EffectDamage, Is.Zero);
            Assert.That(slash.Seconds, Is.LessThan(lightning.Seconds));
        });
    }

    [Test]
    public void OhneSchaden_EndetDerKampfAmZeitlimit()
    {
        var harmless = Fighter.Enemy(Striker with { DamageMin = 0f, DamageMax = 0f }, 1);
        var result   = FightSimulator.Run(harmless, Fighter.Enemy(Dummy, 1), 1, 30);

        Assert.Multiple(() =>
        {
            Assert.That(result.Killed, Is.False);
            Assert.That(result.Seconds, Is.EqualTo(30).Within(FightSimulator.StepSec));
            Assert.That(result.Actions, Is.GreaterThan(0));
        });
    }

    //9 Mana reichen für vier Zauber zu 2, danach bringen 0,5 Mana je Sekunde einen Zauber alle 4 s
    [Test]
    public void Zauber_KostenMana_UndOhneManaWartetDerHeld()
    {
        var spell  = SkillDefinition.ForSpell("zap", new SpellDefinition("Zap", 0, 0, DamageType.Lightning, 0)) with { ManaCost = 2f };
        var hero   = Fighter.Hero(new HeroBaseValues(), skills: [spell]);
        var target = Fighter.Enemy(Dummy with { LifeBonus = 10000 }, 1);

        var result = FightSimulator.Run(hero, target, 1, 20);

        Assert.That(result.Actions, Is.InRange(8, 10));
    }

    [Test]
    public void Zauber_KommtVorDemSchwert_SolangeManaDa_IstDanachSchlaegtDerHeld()
    {
        var spell  = SkillDefinition.ForSpell("zap", new SpellDefinition("Zap", 0, 0, DamageType.Lightning, 0)) with { ManaCost = 100f };
        var hero   = Fighter.Hero(new HeroBaseValues(), [GameData.Weapon("training_sword")], [spell, Fighter.StandardAttack]);
        var target = Fighter.Enemy(GameData.Enemy("skeleton"), 1);

        Assert.That(FightSimulator.Run(hero, target, 1).Killed, Is.True, "Der Zauber ist zu teuer, das Schwert erledigt es");
    }
}
