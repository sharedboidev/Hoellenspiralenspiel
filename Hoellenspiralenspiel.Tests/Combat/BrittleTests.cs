using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

//Die Regeln hat der User am 08.10.2026 vorgegeben: 30 % mehr physischer Schaden für 3 s, jeder Feueranteil löst Brittle,
//bevor der Mehrschaden zählt, auch ein Tick von Burn. Bleed wächst mit. Wer mit Brittle stirbt, zerspringt für 15 % seines Lebens als Kälte
[TestFixture]
public class BrittleTests
{
    private const double Frame     = 1.0 / 60.0;
    private const float  Tolerance = 0.001f;

    private static readonly object Hero = new();

    private static (StatSheet Sheet, StatusEffectTracker Tracker) BrittleDefender()
    {
        var sheet   = new StatSheet();
        var tracker = new StatusEffectTracker(sheet);

        tracker.Apply(StatusEffectRules.CreateApplication(StatusEffectKind.Brittle, Hero));

        return (sheet, tracker);
    }

    private static HitRequest Request(DamageType damageType, float addedFire = 0f, float damage = 100f)
        => new(damage, damage, damageType, SkillKind.Attack)
        {
            AddedDamage = new PerElement<DamageRange>(new DamageRange(addedFire, addedFire), default, default)
        };

    private static List<StatusTick> Run(StatusEffectTracker tracker, double seconds)
    {
        var ticks = new List<StatusTick>();
        var steps = (int)System.Math.Round(seconds / Frame);

        for (var i = 0; i < steps; i++)
            tracker.Advance(Frame, ticks);

        return ticks;
    }

    #region Mehrschaden

    [Test]
    public void Brittle_GibtDreissigProzentMehrPhysischenSchaden()
    {
        var (sheet, _) = BrittleDefender();

        //Auch ein leeres Stat-Blatt mindert ein wenig, darum der Vergleich mit derselben Einheit ohne Brittle
        int Damage(DamageType damageType, StatSheet defender)
            => HitResolver.Resolve(Request(damageType, damage: 1000f), defender, Rolls.Create()).FinalDamage;

        Assert.Multiple(() =>
        {
            Assert.That(Damage(DamageType.Slash, sheet), Is.EqualTo(Damage(DamageType.Slash, new StatSheet()) * 1.3f).Within(1f));
            Assert.That(Damage(DamageType.Crush, sheet), Is.EqualTo(Damage(DamageType.Crush, new StatSheet()) * 1.3f).Within(1f), "Crush macht 20 % mehr, Brittle legt 30 % darauf");
            Assert.That(Damage(DamageType.Pierce, sheet), Is.EqualTo(1300), "Pierce geht an der Rüstung vorbei");
        });
    }

    [Test]
    public void Brittle_LaesstElementarschadenUnveraendert()
    {
        var (sheet, _) = BrittleDefender();

        var result = HitResolver.Resolve(Request(DamageType.Frost), sheet, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(result.FinalDamage, Is.EqualTo(100));
            Assert.That(result.RemovedEffects, Is.Empty, "Kälte löst Brittle nicht");
        });
    }

    [Test]
    public void Brittle_GiltAuchFuerBleed()
    {
        var (sheet, tracker) = BrittleDefender();
        var plain            = new StatusEffectTracker(new StatSheet());

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 5f, 2f));
        plain.Apply(new StatusEffectApplication(StatusEffectKind.Bleed, 5f, 2f));

        var brittleBleed = Run(tracker, 2.1).Where(tick => tick.Kind == StatusEffectKind.Bleed).Sum(tick => tick.Damage);
        var plainBleed   = Run(plain, 2.1).Where(tick => tick.Kind == StatusEffectKind.Bleed).Sum(tick => tick.Damage);

        Assert.Multiple(() =>
        {
            Assert.That(plainBleed, Is.EqualTo(10));
            Assert.That(brittleBleed, Is.EqualTo(13));
            Assert.That(sheet.GetTotalMultiplier(CombatStat.PhysicalDamageTaken), Is.EqualTo(1.3f).Within(Tolerance), "nach 2,1 s läuft Brittle noch");
        });
    }

    #endregion

    #region Feuer

    [Test]
    public void FeuerAnteil_LoestBrittle_BevorDerMehrschadenZaehlt()
    {
        var (sheet, _) = BrittleDefender();

        var withFire = HitResolver.Resolve(Request(DamageType.Slash, addedFire: 10f), sheet, Rolls.Create());
        var fire     = HitResolver.Resolve(Request(DamageType.Fire), sheet, Rolls.Create());

        Assert.Multiple(() =>
        {
            Assert.That(withFire.FinalDamage, Is.EqualTo(110), "100 physisch ohne Mehrschaden und 10 Feuer");
            Assert.That(withFire.RemovedEffects, Is.EqualTo(new[] { StatusEffectKind.Brittle }));
            Assert.That(fire.FinalDamage, Is.EqualTo(100));
            Assert.That(fire.RemovedEffects, Is.EqualTo(new[] { StatusEffectKind.Brittle }));
        });
    }

    [Test]
    public void TrefferOhneFeuer_LoestNichts()
    {
        var (sheet, _) = BrittleDefender();

        Assert.That(HitResolver.Resolve(Request(DamageType.Slash), sheet, Rolls.Create()).RemovedEffects, Is.Empty);
    }

    [Test]
    public void VerfehlterFeuertreffer_LoestNichts()
    {
        var (sheet, _) = BrittleDefender();

        var result = HitResolver.Resolve(Request(DamageType.Fire) with { HitChance = 50f }, sheet, Rolls.Create(hit: 0.9f));

        Assert.Multiple(() =>
        {
            Assert.That(result.HasLanded, Is.False);
            Assert.That(result.RemovedEffects, Is.Empty);
        });
    }

    [Test]
    public void BurnTick_LoestBrittle()
    {
        var (sheet, tracker) = BrittleDefender();

        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Burn, 10f, 4f));

        Run(tracker, 0.4);
        var beforeTick = tracker.IsActive(StatusEffectKind.Brittle);

        Run(tracker, 0.2);

        Assert.Multiple(() =>
        {
            Assert.That(beforeTick, Is.True, "vor dem ersten Tick nach 0,5 s bleibt Brittle");
            Assert.That(tracker.IsActive(StatusEffectKind.Brittle), Is.False);
            Assert.That(tracker.IsActive(StatusEffectKind.Burn), Is.True);
            Assert.That(sheet.GetTotalMultiplier(CombatStat.PhysicalDamageTaken), Is.EqualTo(1f).Within(Tolerance));
        });
    }

    [Test]
    public void RemoveAllRemovedBy_LoestNurWasDieSchadensartNennt()
    {
        var (sheet, tracker) = BrittleDefender();
        var ended            = new List<StatusEffectKind>();

        tracker.Ended += ended.Add;
        tracker.Apply(new StatusEffectApplication(StatusEffectKind.Chill, 0.3f, 3f));

        tracker.RemoveAllRemovedBy(DamageType.Frost);
        var afterFrost = tracker.IsActive(StatusEffectKind.Brittle);

        tracker.RemoveAllRemovedBy(DamageType.Fire);

        Assert.Multiple(() =>
        {
            Assert.That(afterFrost, Is.True);
            Assert.That(tracker.IsActive(StatusEffectKind.Brittle), Is.False);
            Assert.That(tracker.IsActive(StatusEffectKind.Chill), Is.True, "Feuer löst Chill nicht");
            Assert.That(ended, Is.EqualTo(new[] { StatusEffectKind.Brittle }));
            Assert.That(sheet.Modifiers.Any(modifier => modifier.AffectedStat == CombatStat.PhysicalDamageTaken), Is.False);
        });
    }

    #endregion

    #region Dauer und Urheber

    [Test]
    public void Brittle_HaeltDreiSekunden_UndLaesstSichAuffrischen()
    {
        var (_, tracker) = BrittleDefender();
        var other        = new object();

        Run(tracker, 2.5);
        tracker.Apply(StatusEffectRules.CreateApplication(StatusEffectKind.Brittle, other));
        Run(tracker, 2.5);

        var refreshed = tracker.IsActive(StatusEffectKind.Brittle);

        Run(tracker, 0.6);

        Assert.Multiple(() =>
        {
            Assert.That(refreshed, Is.True, "die Auffrischung nach 2,5 s läuft wieder volle 3 s");
            Assert.That(tracker.GetInstanceCount(StatusEffectKind.Brittle), Is.Zero);
        });
    }

    [Test]
    public void Urheber_IstDerZuletztAufgefrischte()
    {
        var (_, tracker) = BrittleDefender();
        var other        = new object();

        var first = tracker.GetSource(StatusEffectKind.Brittle);

        Run(tracker, 0.5);
        tracker.Apply(StatusEffectRules.CreateApplication(StatusEffectKind.Brittle, other));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(Hero));
            Assert.That(tracker.GetSource(StatusEffectKind.Brittle), Is.SameAs(other));
            Assert.That(tracker.GetSource(StatusEffectKind.Chill), Is.Null);
        });
    }

    [Test]
    public void CreateApplication_KenntNurEffekteOhneTrefferschaden()
    {
        var brittle = StatusEffectRules.CreateApplication(StatusEffectKind.Brittle, Hero);

        Assert.Multiple(() =>
        {
            Assert.That(brittle.Magnitude, Is.EqualTo(0.3f));
            Assert.That(brittle.DurationSec, Is.EqualTo(3f));
            Assert.That(brittle.Source, Is.SameAs(Hero));
            Assert.That(StatusEffectRules.CreateApplication(StatusEffectKind.Chill).Magnitude, Is.EqualTo(CombatRules.ChillSlow));
            Assert.That(StatusEffectRules.CreateApplication(StatusEffectKind.Bleed), Is.Null);
            Assert.That(StatusEffectRules.CreateApplication(StatusEffectKind.Burn), Is.Null);
        });
    }

    #endregion

    #region Zerspringen

    [Test]
    public void Zerspringen_MachtFuenfzehnProzentDesLebensAlsKaelte()
    {
        var shatter = new SpellDefinition("Brittle Shatter", 0f, 0f, DamageType.Frost, 10f);

        var hero    = new StatSheet();
        var request = BrittleShatter.CreateRequest(hero, shatter, 1000f);

        //Wie jeder Zauber wächst sie mit dem Zauberschaden, den der Held schon ohne Intelligenz ein wenig hat
        var asSpell = HitRequests.ForSpell(hero, shatter with { MinDamage = 150f, MaxDamage = 150f });

        Assert.Multiple(() =>
        {
            Assert.That(request.MinDamage, Is.EqualTo(asSpell.MinDamage).Within(Tolerance));
            Assert.That(request.MaxDamage, Is.EqualTo(asSpell.MaxDamage).Within(Tolerance));
            Assert.That(request.DamageType, Is.EqualTo(DamageType.Frost));
            Assert.That(request.SkillKind, Is.EqualTo(SkillKind.Spell));
            Assert.That(request.CriticalHitChance, Is.EqualTo(asSpell.CriticalHitChance).Within(Tolerance), "die Chance des Zaubers, erhöht wie jeder Zauber");
            Assert.That(CombatFormulas.GetHitChance(request), Is.EqualTo(100f), "die Explosion verfehlt nicht");
        });
    }

    [Test]
    public void Zerspringen_WaechstMitDemUrheber()
    {
        var shatter = new SpellDefinition("Brittle Shatter", 0f, 0f, DamageType.Frost, 10f);
        var hero    = new StatSheet();

        hero.Update(sheet =>
        {
            sheet.AddModifier(new CombatStatModifier(CombatStat.FrostDamage, ModificationType.Percentage, 0.5f));
            sheet.AddModifier(new CombatStatModifier(CombatStat.CriticalHitChance, ModificationType.Percentage, 1f));
            sheet.SetBase(CombatStat.HitChance, 60f);
        });

        var request = BrittleShatter.CreateRequest(hero, shatter, 1000f);
        var plain   = BrittleShatter.CreateRequest(new StatSheet(), shatter, 1000f);

        Assert.Multiple(() =>
        {
            Assert.That(request.MinDamage, Is.EqualTo(plain.MinDamage * 1.5f).Within(Tolerance), "50 % mehr Kälteschaden");
            Assert.That(request.CriticalHitChance, Is.EqualTo(plain.CriticalHitChance * 2f).Within(0.05f), "100 % erhöhte Krit-Chance");
            Assert.That(request.HitChance, Is.EqualTo(100f), "auch mit wenig Trefferchance");
            Assert.That(BrittleShatter.CreateRequest(hero, shatter, -5f).MaxDamage, Is.Zero);
        });
    }

    #endregion

    #region Stat ohne Herkunft

    [Test]
    public void GetTotalMultiplierWithout_LaesstNurDieGenannteHerkunftWeg()
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            s.AddModifier(new CombatStatModifier(CombatStat.PhysicalDamageTaken, ModificationType.More, 0.3f, "Status:Brittle"));
            s.AddModifier(new CombatStatModifier(CombatStat.PhysicalDamageTaken, ModificationType.More, 0.1f, "Curse"));
            s.AddModifier(new CombatStatModifier(CombatStat.PhysicalDamageTaken, ModificationType.Percentage, 0.2f, "Status:Brittle"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetTotalMultiplier(CombatStat.PhysicalDamageTaken), Is.EqualTo(1.2f * 1.3f * 1.1f).Within(Tolerance));
            Assert.That(sheet.GetTotalMultiplierWithout(CombatStat.PhysicalDamageTaken, ["Status:Brittle"]), Is.EqualTo(1.1f).Within(Tolerance));
            Assert.That(sheet.GetTotalMultiplierWithout(CombatStat.PhysicalDamageTaken, []), Is.EqualTo(1.2f * 1.3f * 1.1f).Within(Tolerance));
        });
    }

    #endregion
}
