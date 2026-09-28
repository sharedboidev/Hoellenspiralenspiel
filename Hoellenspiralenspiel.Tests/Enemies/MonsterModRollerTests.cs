using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Enemies;

[TestFixture]
public class MonsterModRollerTests
{
    private static readonly MonsterTraits MeleeMonster  = new(1, false);
    private static readonly MonsterTraits RangedMonster = new(1, true);

    private static MonsterModDefinition Mod(string id, float weight = 1f, int minLevel = 1, string group = "", MonsterModFit fit = MonsterModFit.Any)
        => new(id, id)
        {
            Weight         = weight,
            MinLevel       = minLevel,
            ExclusiveGroup = group,
            Fit            = fit
        };

    [Test]
    public void Pick_LiefertDieVerlangteZahl()
    {
        var pool = new[] { Mod("a"), Mod("b"), Mod("c"), Mod("d") };

        var picked = MonsterModRoller.Pick(pool, 3, MeleeMonster, new SeededRandom(1));

        Assert.That(picked, Has.Count.EqualTo(3));
    }

    [Test]
    public void Pick_LiefertKeinenModDoppelt()
    {
        var pool = new[] { Mod("a"), Mod("b"), Mod("c"), Mod("d"), Mod("e") };

        for (var seed = 0; seed < 200; seed++)
        {
            var picked = MonsterModRoller.Pick(pool, 5, MeleeMonster, new SeededRandom(seed));

            Assert.That(picked.Select(mod => mod.Id).Distinct().Count(), Is.EqualTo(5), $"Seed {seed}");
        }
    }

    [Test]
    public void Pick_LiefertWeniger_WennDerVorratNichtReicht()
    {
        var pool = new[] { Mod("a"), Mod("b") };

        var picked = MonsterModRoller.Pick(pool, 5, MeleeMonster, new SeededRandom(1));

        Assert.That(picked.Select(mod => mod.Id), Is.EquivalentTo(new[] { "a", "b" }));
    }

    [Test]
    public void Pick_OhneVorratOderOhneAnzahl_LiefertNichts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MonsterModRoller.Pick([], 3, MeleeMonster, new SeededRandom(1)), Is.Empty);
            Assert.That(MonsterModRoller.Pick(new[] { Mod("a") }, 0, MeleeMonster, new SeededRandom(1)), Is.Empty);
        });
    }

    [Test]
    public void Pick_LaesstModsMitZuHohemLevelAus()
    {
        var pool = new[] { Mod("early"), Mod("late", minLevel: 20) };

        var atLevelOne    = MonsterModRoller.Pick(pool, 2, new MonsterTraits(1, false), new SeededRandom(1));
        var atLevelTwenty = MonsterModRoller.Pick(pool, 2, new MonsterTraits(20, false), new SeededRandom(1));

        Assert.Multiple(() =>
        {
            Assert.That(atLevelOne.Select(mod => mod.Id), Is.EquivalentTo(new[] { "early" }));
            Assert.That(atLevelTwenty, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Pick_GibtProjektilModsNurAnSchuetzen()
    {
        var pool = new[] { Mod("twin_shot", fit: MonsterModFit.ProjectileUsersOnly), Mod("cleave", fit: MonsterModFit.MeleeOnly) };

        var forMelee  = MonsterModRoller.Pick(pool, 2, MeleeMonster, new SeededRandom(1));
        var forRanged = MonsterModRoller.Pick(pool, 2, RangedMonster, new SeededRandom(1));

        Assert.Multiple(() =>
        {
            Assert.That(forMelee.Select(mod => mod.Id), Is.EquivalentTo(new[] { "cleave" }));
            Assert.That(forRanged.Select(mod => mod.Id), Is.EquivalentTo(new[] { "twin_shot" }));
        });
    }

    [Test]
    public void Pick_NimmtAusEinerGruppeHoechstensEinen()
    {
        var pool = new[] { Mod("fast", group: "speed"), Mod("faster", group: "speed"), Mod("tough") };

        for (var seed = 0; seed < 100; seed++)
        {
            var picked = MonsterModRoller.Pick(pool, 3, MeleeMonster, new SeededRandom(seed));

            Assert.Multiple(() =>
            {
                Assert.That(picked, Has.Count.EqualTo(2), $"Seed {seed}");
                Assert.That(picked.Count(mod => mod.ExclusiveGroup == "speed"), Is.EqualTo(1), $"Seed {seed}");
            });
        }
    }

    [Test]
    public void Pick_LaesstModsOhneGewichtAus()
    {
        var pool = new[] { Mod("never", 0f), Mod("always") };

        var picked = MonsterModRoller.Pick(pool, 2, MeleeMonster, new SeededRandom(1));

        Assert.That(picked.Select(mod => mod.Id), Is.EquivalentTo(new[] { "always" }));
    }

    [Test]
    public void Pick_FolgtDenGewichten()
    {
        var pool   = new[] { Mod("common", 3f), Mod("rare", 1f) };
        var random = new SeededRandom(99);
        var common = 0;

        const int rolls = 20_000;

        for (var i = 0; i < rolls; i++)
        {
            if (MonsterModRoller.Pick(pool, 1, MeleeMonster, random)[0].Id == "common")
                common++;
        }

        Assert.That(common / (float)rolls, Is.EqualTo(0.75f).Within(0.02f));
    }

    [Test]
    public void Pick_GleicherSeed_ErgibtGleicheMods()
    {
        var pool = new[] { Mod("a"), Mod("b"), Mod("c"), Mod("d"), Mod("e"), Mod("f") };

        var first  = MonsterModRoller.Pick(pool, 4, MeleeMonster, new SeededRandom(42)).Select(mod => mod.Id);
        var second = MonsterModRoller.Pick(pool, 4, MeleeMonster, new SeededRandom(42)).Select(mod => mod.Id);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void Pick_WuerfeltEinmalProMod()
    {
        var pool   = new[] { Mod("a"), Mod("b"), Mod("c") };
        var random = new FixedRandom(0.5f);

        MonsterModRoller.Pick(pool, 2, MeleeMonster, random);

        Assert.That(random.Draws, Is.EqualTo(2));
    }

    [Test]
    public void Modifier_TragenDieHerkunftDesMods()
    {
        var mod = new MonsterModDefinition("hasted", "Hasted")
        {
            Modifiers = new List<CombatStatModifier> { new(CombatStat.Attackspeed, ModificationType.Percentage, 0.33f) }
        };

        var stamped = mod.GetStampedModifiers().Single();

        Assert.Multiple(() =>
        {
            Assert.That(stamped.OriginId, Is.EqualTo("monster-mod:hasted"));
            Assert.That(stamped.Value, Is.EqualTo(0.33f));
            Assert.That(stamped.AffectedStat, Is.EqualTo(CombatStat.Attackspeed));
        });
    }

    [Test]
    public void Modifier_LassenSichAmStatBlattWiederEntfernen()
    {
        var sheet = new StatSheet();
        var mod   = new MonsterModDefinition("stalwart", "Stalwart")
        {
            Modifiers = new List<CombatStatModifier> { new(CombatStat.Life, ModificationType.More, 0.6f) }
        };

        var lifeBefore = sheet.GetFinal(CombatStat.Life);

        sheet.AddModifiers(mod.GetStampedModifiers());

        var lifeWithMod = sheet.GetFinal(CombatStat.Life);

        sheet.RemoveModifiersOf(mod.OriginId);

        Assert.Multiple(() =>
        {
            Assert.That(lifeWithMod, Is.EqualTo(lifeBefore * 1.6f).Within(0.01f));
            Assert.That(sheet.GetFinal(CombatStat.Life), Is.EqualTo(lifeBefore));
        });
    }

    [Test]
    public void ModOhneId_WirdAbgelehnt()
        => Assert.That(() => new MonsterModDefinition(" ", "x"), Throws.ArgumentException);
}
