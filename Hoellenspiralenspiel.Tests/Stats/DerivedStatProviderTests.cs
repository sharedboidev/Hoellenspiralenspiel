using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Stats;

[TestFixture]
public class DerivedStatProviderTests
{
    private static float ValueOf(CombatStat attribute, int attributeValue, CombatStat derivedStat)
        => DerivedStatProvider.GetModifiersFor(attribute, attributeValue)
                              .Single(modifier => modifier.AffectedStat == derivedStat)
                              .Value;

    private static readonly object[] Ceilings =
    [
        new object[] { CombatStat.Strength, CombatStat.PhysicalDamage, ModificationType.More, 1.00f },
        new object[] { CombatStat.Strength, CombatStat.Armor, ModificationType.More, 3.00f },
        new object[] { CombatStat.Dexterity, CombatStat.Attackspeed, ModificationType.More, 1.00f },
        new object[] { CombatStat.Dexterity, CombatStat.Dodge, ModificationType.More, 1.00f },
        new object[] { CombatStat.Intelligence, CombatStat.SpellDamage, ModificationType.More, 2.00f },
        new object[] { CombatStat.Intelligence, CombatStat.Mana, ModificationType.More, 3.00f },
        new object[] { CombatStat.Constitution, CombatStat.Life, ModificationType.More, 1.00f },
        new object[] { CombatStat.Constitution, CombatStat.Armor, ModificationType.Flat, 1000f },
        new object[] { CombatStat.Awareness, CombatStat.MeleeParry, ModificationType.More, 2.00f },
        new object[] { CombatStat.Awareness, CombatStat.MeleeBlock, ModificationType.More, 2.00f },
        new object[] { CombatStat.Awareness, CombatStat.CriticalHitChance, ModificationType.More, 2.00f },
        new object[] { CombatStat.Awareness, CombatStat.LightRadius, ModificationType.More, 4.00f }
    ];

    [TestCaseSource(nameof(Ceilings))]
    public void AbgeleiteterWert_NaehertSichDerObergrenzeAusDemDesigndokument(CombatStat attribute, CombatStat derivedStat, ModificationType type, float ceiling)
    {
        var modifier = DerivedStatProvider.GetModifiersFor(attribute, 100000).Single(m => m.AffectedStat == derivedStat);

        Assert.Multiple(() =>
        {
            Assert.That(modifier.ModificationType, Is.EqualTo(type));
            Assert.That(modifier.Value, Is.EqualTo(ceiling).Within(ceiling * 0.001f));
            Assert.That(modifier.Value, Is.LessThanOrEqualTo(ceiling));
        });
    }

    [TestCaseSource(nameof(Ceilings))]
    public void AbgeleiteterWert_WaechstMitDemAttribut(CombatStat attribute, CombatStat derivedStat, ModificationType type, float ceiling)
    {
        var previous = ValueOf(attribute, 0, derivedStat);

        //Bis 100 muss jeder Punkt spürbar sein, das ist der Bereich, den ein Charakter ohne Ausrüstung erreicht
        for (var attributeValue = 1; attributeValue <= 100; attributeValue++)
        {
            var current = ValueOf(attribute, attributeValue, derivedStat);

            Assert.That(current, Is.GreaterThan(previous), $"kein Zuwachs bei Attributwert {attributeValue}");

            previous = current;
        }

        for (var attributeValue = 101; attributeValue <= 1000; attributeValue++)
        {
            var current = ValueOf(attribute, attributeValue, derivedStat);

            Assert.That(current, Is.GreaterThanOrEqualTo(previous), $"Rückgang bei Attributwert {attributeValue}");

            previous = current;
        }
    }

    [TestCaseSource(nameof(Ceilings))]
    public void AbgeleiteterWert_ErreichtBeiAttribut100MindestensDieHaelfteDerObergrenze(CombatStat attribute, CombatStat derivedStat, ModificationType type, float ceiling)
        => Assert.That(ValueOf(attribute, 100, derivedStat), Is.GreaterThan(ceiling / 2));

    [TestCaseSource(nameof(Ceilings))]
    public void AbgeleiteterWert_StartetBeiZweiProzentBeziehungsweiseZweiPunkten(CombatStat attribute, CombatStat derivedStat, ModificationType type, float ceiling)
    {
        var expected = type == ModificationType.Flat ? 2f : 0.02f;

        Assert.That(ValueOf(attribute, 0, derivedStat), Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void Intelligenz4_ErgibtDenImSpielBeobachtetenZauberschaden()
        => Assert.That(ValueOf(CombatStat.Intelligence, 4, CombatStat.SpellDamage), Is.EqualTo(0.0297f).Within(0.0001f));

    [Test]
    public void Intelligenz1_ErgibtDenImSpielBeobachtetenZauberschaden()
        => Assert.That(ValueOf(CombatStat.Intelligence, 1, CombatStat.SpellDamage), Is.EqualTo(0.0221f).Within(0.0001f));

    [Test]
    public void JedesAttribut_BenutztSeineEigeneHerkunft()
    {
        var origins = StatSheet.Attributes
                               .Select(attribute => DerivedStatProvider.GetModifiersFor(attribute, 10).Select(m => m.OriginId).Distinct().ToArray())
                               .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(origins, Has.All.Length.EqualTo(1), "ein Attribut benutzt mehrere Herkünfte");
            Assert.That(origins.SelectMany(o => o).Distinct().Count(), Is.EqualTo(StatSheet.Attributes.Count), "zwei Attribute teilen sich eine Herkunft");
        });
    }

    [Test]
    public void KeinAttribut_LeitetEinAnderesAttributAb()
    {
        var derivedStats = StatSheet.Attributes.SelectMany(attribute => DerivedStatProvider.GetModifiersFor(attribute, 10)).Select(m => m.AffectedStat);

        Assert.That(derivedStats.Where(StatSheet.IsAttribute), Is.Empty);
    }

    [Test]
    public void AndereStats_LeitenNichtsAb()
        => Assert.That(DerivedStatProvider.GetModifiersFor(CombatStat.Armor, 50), Is.Empty);
}
