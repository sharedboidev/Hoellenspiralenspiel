using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Loading;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Loading;

[TestFixture]
public class LoadingTipsTests
{
    private static readonly Dictionary<string, string> Keys = new()
    {
        ["open_town_portal"]   = "T",
        ["toggle_loot_labels"] = "Alt",
        ["unbound"]            = string.Empty
    };

    private static string KeyOf(string action)
        => Keys.GetValueOrDefault(action);

    [Test]
    public void DieAktionWirdZurTaste()
        => Assert.That(LoadingTips.Format("Press {open_town_portal} to open a Town Portal.", KeyOf), Is.EqualTo("Press T to open a Town Portal."));

    [Test]
    public void MehrereAktionenInEinemTipp()
        => Assert.That(LoadingTips.Format("{toggle_loot_labels} and {open_town_portal}", KeyOf), Is.EqualTo("Alt and T"));

    [Test]
    public void OhneAktionBleibtDerText()
        => Assert.That(LoadingTips.Format("  Loot on the ground is lost.  ", KeyOf), Is.EqualTo("Loot on the ground is lost."));

    [TestCase("Press {unbound} now.")]
    [TestCase("Press {missing_action} now.")]
    [TestCase("Press {open_town_portal} or {unbound}.")]
    public void EineAktionOhneTasteVerwirftDenTipp(string tip)
        => Assert.That(LoadingTips.Format(tip, KeyOf), Is.Null);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void EinLeererTippIstKeiner(string tip)
        => Assert.That(LoadingTips.Format(tip, KeyOf), Is.Null);

    [Test]
    public void GewaehltWirdNurUnterBrauchbarenTipps()
    {
        string[] tips = ["Press {unbound}.", "", "Press {open_town_portal}."];

        for (var roll = 0f; roll < 1f; roll += 0.1f)
            Assert.That(LoadingTips.Pick(tips, KeyOf, null, new FixedRandom(roll)), Is.EqualTo("Press T."));
    }

    [Test]
    public void DerLetzteTippKommtNichtGleichWieder()
    {
        string[] tips = ["A", "B", "C"];

        for (var roll = 0f; roll < 1f; roll += 0.05f)
            Assert.That(LoadingTips.Pick(tips, KeyOf, "B", new FixedRandom(roll)), Is.Not.EqualTo("B"));
    }

    [Test]
    public void JederAndereTippKommtVor()
    {
        string[] tips = ["A", "B", "C"];

        Assert.That(LoadingTips.Pick(tips, KeyOf, "B", new FixedRandom(0f)), Is.EqualTo("A"));
        Assert.That(LoadingTips.Pick(tips, KeyOf, "B", new FixedRandom(0.99f)), Is.EqualTo("C"));
    }

    [Test]
    public void GibtEsNurEinenTippKommtErImmer()
        => Assert.That(LoadingTips.Pick(["A"], KeyOf, "A", new FixedRandom(0.5f)), Is.EqualTo("A"));

    [Test]
    public void DoppelteTippsZaehlenEinmal()
    {
        for (var roll = 0f; roll < 1f; roll += 0.1f)
            Assert.That(LoadingTips.Pick(["A", "A", "B"], KeyOf, "A", new FixedRandom(roll)), Is.EqualTo("B"));
    }

    [Test]
    public void OhneTippsGibtEsKeinen()
    {
        Assert.That(LoadingTips.Pick([], KeyOf, null, new SeededRandom(1)), Is.Null);
        Assert.That(LoadingTips.Pick(null, KeyOf, null, new SeededRandom(1)), Is.Null);
        Assert.That(LoadingTips.Pick(["{unbound}"], KeyOf, null, new SeededRandom(1)), Is.Null);
    }

    [TestCase(1.5, 0.0, 1.5)]
    [TestCase(1.5, 0.4, 1.1)]
    [TestCase(1.5, 1.5, 0.0)]
    [TestCase(1.5, 3.0, 0.0)]
    [TestCase(0.0, 0.2, 0.0)]
    [TestCase(1.0, -2.0, 1.0)]
    public void DerRestDerMindestdauer(double minimum, double shown, double expected)
        => Assert.That(LoadingTips.RemainingSec(minimum, shown), Is.EqualTo(expected).Within(1e-9));
}
