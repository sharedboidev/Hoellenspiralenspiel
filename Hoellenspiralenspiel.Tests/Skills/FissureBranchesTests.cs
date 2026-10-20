using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Die Regel hat der User am 08.10.2026 vorgegeben: Bei Proliferate +2 und fünf vom Hauptriss getroffenen Gegnern gehen nur vom ersten
//und zweiten Nebenrisse ab. Nebenrisse erzeugen keine weiteren
[TestFixture]
public class FissureBranchesTests
{
    [Test]
    public void Proliferate2_NurDieErstenZweiGetroffenen()
    {
        var branches = new FissureBranches(2);

        var claims = new bool[5];

        for (var unit = 0; unit < claims.Length; unit++)
            claims[unit] = branches.TryClaim(0, (ulong)unit);

        Assert.Multiple(() =>
        {
            Assert.That(claims, Is.EqualTo(new[] { true, true, false, false, false }));
            Assert.That(branches.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void JederGegner_GibtJeSchlagHoechstensEinenNebenriss()
    {
        var branches = new FissureBranches(3);

        Assert.Multiple(() =>
        {
            Assert.That(branches.TryClaim(0, 7), Is.True);
            Assert.That(branches.TryClaim(0, 7), Is.False, "derselbe Riss noch einmal");
            Assert.That(branches.TryClaim(1, 7), Is.False, "ein anderer Hauptriss desselben Schlags");
        });
    }

    [Test]
    public void JederHauptriss_HatSeinEigenesKontingent()
    {
        var branches = new FissureBranches(1);

        Assert.Multiple(() =>
        {
            Assert.That(branches.TryClaim(0, 1), Is.True);
            Assert.That(branches.TryClaim(0, 2), Is.False);
            Assert.That(branches.TryClaim(1, 2), Is.True, "Gegner 2 ging bei Riss 0 leer aus und bekommt seinen Nebenriss von Riss 1");
        });
    }

    [Test]
    public void OhneProliferate_UndAusNebenrissen_KeineNebenrisse()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new FissureBranches(0).TryClaim(0, 1), Is.False);
            Assert.That(new FissureBranches(-2).PerMainCrack, Is.Zero);
            Assert.That(new FissureBranches(2).TryClaim(-1, 1), Is.False, "ein Nebenriss hat den Index -1");
        });
    }
}
