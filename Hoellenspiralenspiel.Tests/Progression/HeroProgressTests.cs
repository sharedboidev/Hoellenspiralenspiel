using Hoellenspiralenspiel.Scripts.Core.Progression;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Progression;

[TestFixture]
public class HeroProgressTests
{
    private const long XpForLevel2 = 525;
    private const long XpForLevel3 = 1760;

    [Test]
    public void Start_IstLevelEinsOhneXp()
    {
        var progress = new HeroProgress();

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(1));
            Assert.That(progress.XpTotal, Is.Zero);
            Assert.That(progress.AttributePoints, Is.Zero);
            Assert.That(progress.XpFloor, Is.Zero);
            Assert.That(progress.XpForNextLevel, Is.EqualTo(XpForLevel2));
        });
    }

    [Test]
    public void Gewinn_UnterDerSchwelle_LaesstDasLevel()
    {
        var progress = new HeroProgress();

        progress.Gain(XpForLevel2 - 1);

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(1));
            Assert.That(progress.XpIntoLevel, Is.EqualTo(XpForLevel2 - 1));
        });
    }

    [Test]
    public void Gewinn_AufDerSchwelle_BringtLevelUndPunkt()
    {
        var progress  = new HeroProgress();
        var levelUps  = 0;

        progress.LeveledUp += () => levelUps++;

        progress.Gain(XpForLevel2);

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(2));
            Assert.That(progress.AttributePoints, Is.EqualTo(1));
            Assert.That(progress.XpFloor, Is.EqualTo(XpForLevel2));
            Assert.That(progress.XpForNextLevel, Is.EqualTo(XpForLevel3));
            Assert.That(levelUps, Is.EqualTo(1));
        });
    }

    [Test]
    public void Gewinn_FuerZweiLevel_BringtErstEines()
    {
        var progress = new HeroProgress();

        progress.Gain(XpForLevel3);

        Assert.That(progress.Level, Is.EqualTo(2));

        progress.Gain(1);

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(3));
            Assert.That(progress.AttributePoints, Is.EqualTo(2));
        });
    }

    [TestCase(0)]
    [TestCase(-50)]
    public void Gewinn_OhneXp_AendertNichts(long experience)
    {
        var progress = new HeroProgress();
        var changes  = 0;

        progress.Changed += () => changes++;

        progress.Gain(experience);

        Assert.Multiple(() =>
        {
            Assert.That(progress.XpTotal, Is.Zero);
            Assert.That(changes, Is.Zero);
        });
    }

    [Test]
    public void Gewinn_AufDemHoechstenLevel_BringtKeinLevel()
    {
        var progress = new HeroProgress();

        progress.Restore(XpTable.MaxLevel, 0, 0);
        progress.Gain(1_000_000);

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(XpTable.MaxLevel));
            Assert.That(progress.AttributePoints, Is.Zero);
        });
    }

    [Test]
    public void Tod_KostetEinZehntelDerLevelspanne()
    {
        var progress = new HeroProgress();

        progress.Gain(300);

        var loss = progress.LoseForDeath();

        Assert.Multiple(() =>
        {
            Assert.That(loss, Is.EqualTo(52));
            Assert.That(progress.XpTotal, Is.EqualTo(248));
        });
    }

    [Test]
    public void Tod_KostetKeinLevel()
    {
        var progress = new HeroProgress();

        progress.Gain(XpForLevel2 + 10);

        var loss = progress.LoseForDeath();

        Assert.Multiple(() =>
        {
            Assert.That(loss, Is.EqualTo(10));
            Assert.That(progress.Level, Is.EqualTo(2));
            Assert.That(progress.XpTotal, Is.EqualTo(XpForLevel2));
        });
    }

    [Test]
    public void Tod_OhneXp_MeldetKeineAenderung()
    {
        var progress = new HeroProgress();
        var changes  = 0;

        progress.Changed += () => changes++;

        Assert.Multiple(() =>
        {
            Assert.That(progress.LoseForDeath(), Is.Zero);
            Assert.That(changes, Is.Zero);
        });
    }

    [Test]
    public void Punkt_LaesstSichNurAusgeben_WennEinerDaIst()
    {
        var progress = new HeroProgress();

        Assert.That(progress.SpendAttributePoint(), Is.False);

        progress.Gain(XpForLevel2);

        Assert.Multiple(() =>
        {
            Assert.That(progress.SpendAttributePoint(), Is.True);
            Assert.That(progress.AttributePoints, Is.Zero);
            Assert.That(progress.SpendAttributePoint(), Is.False);
        });
    }

    [Test]
    public void Wiederherstellen_LoestKeinenAufstiegAus()
    {
        var progress = new HeroProgress();
        var levelUps = 0;

        progress.LeveledUp += () => levelUps++;

        progress.Restore(2, XpForLevel3 + 5, 3);

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(2));
            Assert.That(progress.XpTotal, Is.EqualTo(XpForLevel3 + 5));
            Assert.That(progress.AttributePoints, Is.EqualTo(3));
            Assert.That(levelUps, Is.Zero);
        });
    }

    [Test]
    public void Wiederherstellen_BegrenztUngueltigeWerte()
    {
        var progress = new HeroProgress();

        progress.Restore(0, -20, -4);

        Assert.Multiple(() =>
        {
            Assert.That(progress.Level, Is.EqualTo(1));
            Assert.That(progress.XpTotal, Is.Zero);
            Assert.That(progress.AttributePoints, Is.Zero);
        });

        progress.Restore(3, 100, 0);

        Assert.That(progress.XpTotal, Is.EqualTo(XpForLevel3));
    }
}
