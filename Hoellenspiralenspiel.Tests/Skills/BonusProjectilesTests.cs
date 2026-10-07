using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class BonusProjectilesTests
{
    private static StatSheet SheetWithProjectiles(float projectiles)
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.ProjectileCount, projectiles);

        return sheet;
    }

    private static WeaponProfile Bow(int extraArrows)
        => new WeaponProfile(10, 20, 1.2f, 5f, DamageType.Pierce, 600f, true, 1100f) with { ExtraProjectiles = extraArrows };

    [Test]
    public void EinProjektil_IstKeinBonus()
        => Assert.That(BonusProjectiles.FromStats(SheetWithProjectiles(1)), Is.Zero);

    [Test]
    public void JedesWeitereProjektil_ZaehltAlsBonus()
        => Assert.That(BonusProjectiles.FromStats(SheetWithProjectiles(3)), Is.EqualTo(2));

    [Test]
    public void WenigerAlsEinProjektil_GibtKeinenNegativenBonus()
        => Assert.That(BonusProjectiles.FromStats(SheetWithProjectiles(0)), Is.Zero);

    [Test]
    public void OhneDenStat_GibtEsKeinenBonus()
        => Assert.That(BonusProjectiles.FromStats(new StatSheet()), Is.Zero);

    [Test]
    public void DerBogen_ZaehltSeinePfeileDazu()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BonusProjectiles.ForBow(SheetWithProjectiles(2), Bow(1)), Is.EqualTo(2));
            Assert.That(BonusProjectiles.ForBow(SheetWithProjectiles(1), Bow(0)), Is.Zero);
            Assert.That(BonusProjectiles.ForBow(SheetWithProjectiles(1), null), Is.Zero, "ohne Waffe zählt nur der Stat");
        });
    }

    [Test]
    public void OhneStatBlatt_WirdGeworfen()
        => Assert.That(() => BonusProjectiles.FromStats(null), Throws.ArgumentNullException);
}
