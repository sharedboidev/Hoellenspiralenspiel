using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Stats;

//Die Erwartungen stammen aus dem laufenden Spiel: Die Laufzeitprüfung der Etappe 2 von M8 las sie an einem neuen Helden ab
[TestFixture]
public class HeroBaseValuesTests
{
    private static StatSheet SheetOf(HeroBaseValues values)
    {
        var sheet = new StatSheet();

        values.Apply(sheet);

        return sheet;
    }

    [Test]
    public void NeuerHeld_Hat60Leben_Und9Mana()
    {
        var sheet = SheetOf(new HeroBaseValues());

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinalWhole(CombatStat.Life), Is.EqualTo(60), "5 + 1 + 3 aus den Attributen, 50 Zuschlag, 2 % mehr aus Konstitution 1");
            Assert.That(sheet.GetFinalWhole(CombatStat.Mana), Is.EqualTo(9), "3 + 1 + 5 aus den Attributen");
        });
    }

    [Test]
    public void Grundwerte_LandenImBlatt()
    {
        var sheet = SheetOf(new HeroBaseValues { Movementspeed = 500f });

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetBase(CombatStat.Dodge), Is.EqualTo(6f));
            Assert.That(sheet.GetBase(CombatStat.Manaregeneration), Is.EqualTo(0.5f));
            Assert.That(sheet.GetBase(CombatStat.LightRadius), Is.EqualTo(100f));
            Assert.That(sheet.GetBase(CombatStat.Life), Is.EqualTo(50f));
            Assert.That(sheet.GetFinal(CombatStat.Movementspeed), Is.EqualTo(500f));
            Assert.That(sheet.GetFinal(CombatStat.Manaregeneration), Is.EqualTo(0.5f));
        });
    }

    [Test]
    public void Attribute_SpeisenDieAbgeleitetenWerte()
    {
        var sheet = SheetOf(new HeroBaseValues { Strength = 4, Constitution = 6, Intelligence = 3 });

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinalWhole(CombatStat.Strength), Is.EqualTo(4));
            Assert.That(sheet.GetEffectiveBase(CombatStat.Life), Is.EqualTo(5 + 4 + 18 + 50), "Lebensformel plus Zuschlag");
            Assert.That(sheet.GetEffectiveBase(CombatStat.Mana), Is.EqualTo(3 + 1 + 15), "Manaformel");
            Assert.That(sheet.GetFinalWhole(CombatStat.Liferegeneration), Is.EqualTo(2), "Stärke 4 / 5 + Konstitution 6 / 3, abgerundet");
        });
    }

    [Test]
    public void JedeEinheit_TrifftSicher_KritisiertMit50Prozent_UndBlocktDieHaelfte()
    {
        var sheet = new StatSheet();

        UnitBaseValues.Apply(sheet);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinal(CombatStat.HitChance), Is.EqualTo(CombatRules.BaseHitChance));
            Assert.That(sheet.GetFinal(CombatStat.CriticalDamage), Is.EqualTo(CombatRules.BaseCriticalDamage));
            Assert.That(sheet.GetFinal(CombatStat.BlockReduction), Is.EqualTo(CombatRules.BaseBlockReduction));
            Assert.That(sheet.GetFinal(CombatStat.ProjectileCount), Is.EqualTo(1f));
        });
    }
}
