using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Die Werte hat der User am 08.10.2026 vorgegeben: 3 m Radius, der in 6 s um 25 % wächst, Brittle jede halbe Sekunde neu
[TestFixture]
public class CloudSettingsTests
{
    private const float Tolerance = 0.001f;

    private static readonly CloudSettings BrittleMist = new(300f, 6f, 25f, 0.5f, StatusEffectKind.Brittle);

    [Test]
    public void Radius_WaechstGleichmaessigUmEinViertel()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BrittleMist.GetRadiusAfter(0), Is.EqualTo(300f).Within(Tolerance));
            Assert.That(BrittleMist.GetRadiusAfter(3), Is.EqualTo(337.5f).Within(Tolerance));
            Assert.That(BrittleMist.GetRadiusAfter(6), Is.EqualTo(375f).Within(Tolerance));
            Assert.That(BrittleMist.GetRadiusAfter(9), Is.EqualTo(375f).Within(Tolerance), "nach der Dauer wächst nichts mehr");
            Assert.That(BrittleMist.GetRadiusAfter(-1), Is.EqualTo(300f).Within(Tolerance));
        });
    }

    [Test]
    public void Radius_OhneWachstumOderDauer()
    {
        Assert.Multiple(() =>
        {
            Assert.That((BrittleMist with { GrowthPercent = 0f }).GetRadiusAfter(4), Is.EqualTo(300f).Within(Tolerance));
            Assert.That((BrittleMist with { GrowthPercent = -50f }).GetRadiusAfter(4), Is.EqualTo(300f).Within(Tolerance), "ein Nebel schrumpft nicht");
            Assert.That((BrittleMist with { DurationSec = 0f }).GetRadiusAfter(0), Is.EqualTo(375f).Within(Tolerance));
        });
    }

    [Test]
    public void Nebel_EndetNachSeinerDauer()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BrittleMist.IsOver(5.99), Is.False);
            Assert.That(BrittleMist.IsOver(6), Is.True);
        });
    }

    [Test]
    public void Puls_KommtJedeHalbeSekunde()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BrittleMist.IsDue(1.2, 1.0), Is.False);
            Assert.That(BrittleMist.IsDue(1.5, 1.0), Is.True);
            Assert.That(BrittleMist.IsDue(1.4999999, 1.0), Is.True, "Rundung der Frames verschiebt den Puls nicht um einen Frame");
            Assert.That(BrittleMist.IsDue(2.0, 1.0), Is.True);
        });
    }
}
