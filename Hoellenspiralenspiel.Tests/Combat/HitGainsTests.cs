using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class HitGainsTests
{
    private const string Ring      = "Item:Ring";
    private const float  Tolerance = 0.001f;

    private static StatSheet Attacker(params (CombatStat Stat, float Value)[] flats)
    {
        var sheet = new StatSheet();

        sheet.AddModifiers(flats.Select(flat => new CombatStatModifier(flat.Stat, ModificationType.Flat, flat.Value, Ring)));

        return sheet;
    }

    //Ein Angriff mit 100 physischem Schaden und 75 Feuer
    private static HitResult LandedAttack(DamageType damageType = DamageType.Slash, SkillKind skillKind = SkillKind.Attack)
        => new()
        {
            DamageType  = damageType,
            SkillKind   = skillKind,
            Avoidance   = HitAvoidance.None,
            FinalDamage = 175,
            AddedDamage = new PerElement<int>(75, 0, 0)
        };

    [Test]
    public void LebenJeTreffer_GiltFuerGelandeteAngriffe()
    {
        var attacker = Attacker((CombatStat.LifeOnHit, 5f));

        Assert.Multiple(() =>
        {
            Assert.That(HitGains.GetLifeOnHit(attacker, LandedAttack()), Is.EqualTo(5f));
            Assert.That(HitGains.GetLifeOnHit(attacker, LandedAttack(skillKind: SkillKind.Spell)), Is.Zero, "Zauber nicht");
            Assert.That(HitGains.GetLifeOnHit(attacker, LandedAttack() with { Avoidance = HitAvoidance.Dodged }), Is.Zero, "ausgewichen");
        });
    }

    [Test]
    public void ZehnGetroffeneGegner_GebenZehnmalLeben()
    {
        var attacker = Attacker((CombatStat.LifeOnHit, 3f));
        var hits     = Enumerable.Repeat(LandedAttack(), 10);

        Assert.That(hits.Sum(hit => HitGains.GetLifeOnHit(attacker, hit)), Is.EqualTo(30f));
    }

    [Test]
    public void Leech_SaugtNurAusDemPhysischenSchaden()
    {
        var attacker = Attacker((CombatStat.Leech, 3f), (CombatStat.ManaLeech, 2f));

        Assert.Multiple(() =>
        {
            Assert.That(HitGains.GetLifeLeech(attacker, LandedAttack()), Is.EqualTo(3f).Within(Tolerance), "3 % von 100, das Feuer zählt nicht");
            Assert.That(HitGains.GetManaLeech(attacker, LandedAttack()), Is.EqualTo(2f).Within(Tolerance));
            Assert.That(HitGains.GetLifeLeech(attacker, LandedAttack(DamageType.Lightning)), Is.Zero, "ein gewandelter Angriff ist nicht physisch");
            Assert.That(HitGains.GetLifeLeech(attacker, LandedAttack(skillKind: SkillKind.Spell)), Is.Zero);
        });
    }

    [Test]
    public void LebenUndManaJeKill_KommenAusDenStats()
    {
        var attacker = Attacker((CombatStat.LifeOnKill, 12f), (CombatStat.ManaOnKill, 6f));

        Assert.Multiple(() =>
        {
            Assert.That(HitGains.GetLifeOnKill(attacker), Is.EqualTo(12f));
            Assert.That(HitGains.GetManaOnKill(attacker), Is.EqualTo(6f));
            Assert.That(HitGains.GetLifeOnKill(new StatSheet()), Is.Zero);
        });
    }

    [Test]
    public void NegativeWerte_NehmenNichts()
    {
        var attacker = Attacker((CombatStat.LifeOnHit, -4f), (CombatStat.Leech, -2f));

        Assert.Multiple(() =>
        {
            Assert.That(HitGains.GetLifeOnHit(attacker, LandedAttack()), Is.Zero);
            Assert.That(HitGains.GetLifeLeech(attacker, LandedAttack()), Is.Zero);
        });
    }
}
