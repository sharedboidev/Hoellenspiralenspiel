using Hoellenspiralenspiel.Scripts.Core.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Combat;

[TestFixture]
public class LeechTrackerTests
{
    private const float Tolerance = 0.001f;

    [Test]
    public void Leech_HeiltGleichmaessigUeberDreiSekunden()
    {
        var leech = new LeechTracker();

        leech.Add(30f);

        Assert.Multiple(() =>
        {
            Assert.That(leech.Pending, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(leech.Advance(1.0), Is.EqualTo(10f).Within(Tolerance));
            Assert.That(leech.Pending, Is.EqualTo(20f).Within(Tolerance));
            Assert.That(leech.Advance(0.5), Is.EqualTo(5f).Within(Tolerance));
            Assert.That(leech.Advance(1.5), Is.EqualTo(15f).Within(Tolerance));
            Assert.That(leech.IsActive, Is.False);
            Assert.That(leech.Pending, Is.Zero);
        });
    }

    [Test]
    public void MehrereLeeches_HeilenZugleich()
    {
        var leech = new LeechTracker();

        leech.Add(30f);
        leech.Advance(1.0);
        leech.Add(60f);

        Assert.Multiple(() =>
        {
            Assert.That(leech.InstanceCount, Is.EqualTo(2));
            Assert.That(leech.Pending, Is.EqualTo(80f).Within(Tolerance));
            Assert.That(leech.Advance(1.0), Is.EqualTo(10f + 20f).Within(Tolerance), "beide heilen in derselben Sekunde");
            Assert.That(leech.Advance(1.0), Is.EqualTo(10f + 20f).Within(Tolerance));
            Assert.That(leech.InstanceCount, Is.EqualTo(1), "der erste ist durch");
            Assert.That(leech.Advance(1.0), Is.EqualTo(20f).Within(Tolerance));
            Assert.That(leech.IsActive, Is.False);
        });
    }

    [Test]
    public void LangerSchritt_HeiltNurDenRest()
    {
        var leech = new LeechTracker();

        leech.Add(30f);

        Assert.Multiple(() =>
        {
            Assert.That(leech.Advance(5.0), Is.EqualTo(30f).Within(Tolerance));
            Assert.That(leech.Advance(1.0), Is.Zero);
        });
    }

    [Test]
    public void VieleKleineSchritte_HeilenGenauDenBetrag()
    {
        var leech  = new LeechTracker();
        var healed = 0f;

        leech.Add(7f);

        for (var step = 0; step < 400; step++)
            healed += leech.Advance(1.0 / 60);

        Assert.That(healed, Is.EqualTo(7f).Within(Tolerance));
    }

    [TestCase(0f)]
    [TestCase(-5f)]
    [TestCase(float.NaN)]
    public void OhneBetrag_StartetKeinLeech(float amount)
    {
        var leech = new LeechTracker();

        leech.Add(amount);

        Assert.That(leech.IsActive, Is.False);
    }

    [Test]
    public void Clear_BeendetJedenLeech()
    {
        var leech = new LeechTracker();

        leech.Add(30f);
        leech.Add(10f);
        leech.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(leech.IsActive, Is.False);
            Assert.That(leech.Advance(1.0), Is.Zero);
        });
    }
}
