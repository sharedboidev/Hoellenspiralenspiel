using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Die Werte hat der User am 08.10.2026 vorgegeben: zwei Sprünge, jeder 25 % weniger als der davor. Weitere Sprünge gibt nur Proliferate
[TestFixture]
public class ChainSettingsTests
{
    private const float Tolerance = 0.0001f;

    private static readonly ChainSettings ChainLightning = new(800f, 500f, 2, 25f);

    private static StatSheet SheetWith(CombatStat stat, float value)
    {
        var sheet = new StatSheet();

        sheet.SetBase(stat, value);

        return sheet;
    }

    [Test]
    public void JederSprung_MachtEinViertelWenigerAlsDerDavor()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChainLightning.GetDamageFactor(0), Is.EqualTo(1f).Within(Tolerance), "das erste Ziel bekommt den vollen Schaden");
            Assert.That(ChainLightning.GetDamageFactor(1), Is.EqualTo(0.75f).Within(Tolerance));
            Assert.That(ChainLightning.GetDamageFactor(2), Is.EqualTo(0.5625f).Within(Tolerance));
            Assert.That(ChainLightning.GetDamageFactor(5), Is.EqualTo(0.2373f).Within(Tolerance), "mit Proliferate wird der Schaden nie null");
        });
    }

    [Test]
    public void Abnahme_BleibtZwischenNullUndHundert()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new ChainSettings(800f, 500f, 2, 0f).GetDamageFactor(3), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(new ChainSettings(800f, 500f, 2, 150f).GetDamageFactor(1), Is.Zero);
            Assert.That(new ChainSettings(800f, 500f, 2, -20f).GetDamageFactor(1), Is.EqualTo(1f).Within(Tolerance), "eine negative Abnahme verstärkt nicht");
            Assert.That(ChainLightning.GetDamageFactor(-1), Is.EqualTo(1f).Within(Tolerance));
        });
    }

    [Test]
    public void Proliferate_GibtWeitereSpruenge()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChainLightning.GetJumps(0), Is.EqualTo(2));
            Assert.That(ChainLightning.GetJumps(1), Is.EqualTo(3));
            Assert.That(ChainLightning.GetJumps(-2), Is.EqualTo(2), "weniger als keine Sprünge nimmt nichts weg");
            Assert.That(new ChainSettings(800f, 500f, -1, 25f).GetJumps(0), Is.Zero);
        });
    }

    [Test]
    public void Proliferate_KommtAusDemStat_ZusaetzlicheProjektileNicht()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChainSettings.GetProliferate(new StatSheet()), Is.Zero);
            Assert.That(ChainSettings.GetProliferate(SheetWith(CombatStat.Proliferate, 2)), Is.EqualTo(2));
            Assert.That(ChainSettings.GetProliferate(SheetWith(CombatStat.Proliferate, -1)), Is.Zero);
            Assert.That(ChainSettings.GetProliferate(SheetWith(CombatStat.ProjectileCount, 4)), Is.Zero);
        });
    }

    [Test]
    public void OhneStatBlatt_WirdGeworfen()
        => Assert.That(() => ChainSettings.GetProliferate(null), Throws.ArgumentNullException);

    [Test]
    public void EinMauspunktInReichweite_BleibtWoErIst()
        => Assert.That(ChainLightning.ClampToRange(300f, -400f), Is.EqualTo((300f, -400f)));

    [Test]
    public void EinMauspunktJenseitsDerReichweite_RuecktAufIhrenRand()
    {
        var (x, y) = ChainLightning.ClampToRange(1200f, 1600f, 20f);

        Assert.Multiple(() =>
        {
            Assert.That(x, Is.EqualTo(492f).Within(Tolerance), "820 Pixel ab der Mitte, die Richtung bleibt");
            Assert.That(y, Is.EqualTo(656f).Within(Tolerance));
            Assert.That(ChainLightning.ClampToRange(0f, 0f), Is.EqualTo((0f, 0f)));
        });
    }

    [Test]
    public void EinSchwaechererTreffer_BehaeltSeineChancen()
    {
        var hit = new HitRequest(40f, 80f, DamageType.Lightning, SkillKind.Spell, 90f, 10f, 150f)
        {
            AddedDamage = new PerElement<DamageRange>(new DamageRange(4f, 8f), default, default)
        };

        var weaker = hit.Times(0.75f);

        Assert.Multiple(() =>
        {
            Assert.That(weaker.MinDamage, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(weaker.MaxDamage, Is.EqualTo(60f).Within(Tolerance));
            Assert.That(weaker.AddedDamage.Fire, Is.EqualTo(new DamageRange(3f, 6f)));
            Assert.That(weaker.HitChance, Is.EqualTo(90f));
            Assert.That(weaker.CriticalHitChance, Is.EqualTo(10f));
            Assert.That(weaker.CriticalDamageBonus, Is.EqualTo(150f));
            Assert.That(weaker.DamageType, Is.EqualTo(DamageType.Lightning));
        });
    }
}
