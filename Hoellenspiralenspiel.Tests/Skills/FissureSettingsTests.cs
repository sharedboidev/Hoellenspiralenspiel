using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Die Werte hat der User am 08.10.2026 vorgegeben: Einschlag 1 m, Risse 10 m lang, in 0,4 s gewachsen, 4 s liegend, 120 % Waffenschaden als Feuer je Sekunde,
//85 % als Treffer, wenn der Riss durch einen Gegner läuft
[TestFixture]
public class FissureSettingsTests
{
    private const float Tolerance = 0.001f;

    private static readonly FissureSettings LavaCrack = new(100f, 1000f, 50f, 0.4f, 4f, 120f, 30f, 85f, 5);

    [Test]
    public void Riss_RechnetMitEigenemAnteil_GanzInFeuer()
    {
        var slam = new AttackDefinition("Lava Crack", 100f) { IgnoresPierceHitPenalty = true };

        var crack = LavaCrack.GetAttack(slam);

        Assert.Multiple(() =>
        {
            Assert.That(crack.WeaponDamagePercent, Is.EqualTo(120f));
            Assert.That(crack.DealtAs, Is.EqualTo(DamageType.Fire));
            Assert.That(crack.Name, Is.EqualTo("Lava Crack"));
            Assert.That(slam.DealtAs, Is.Null, "der Schlag selbst bleibt physisch");
            Assert.That(LavaCrack.GetHitAttack(slam).WeaponDamagePercent, Is.EqualTo(85f), "der Treffer, wenn der Riss durch einen Gegner läuft");
            Assert.That(LavaCrack.GetHitAttack(slam).DealtAs, Is.EqualTo(DamageType.Fire));
        });
    }

    //Der Wunsch des Users vom 08.10.2026: höchstens fünf Hauptrisse zugleich, der sechste löst den ersten vorzeitig auf
    [Test]
    public void HoechstensFuenfHauptrisse_DerAeltesteWeicht()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LavaCrack.GetExcess(5), Is.Zero);
            Assert.That(LavaCrack.GetExcess(6), Is.EqualTo(1));
            Assert.That(LavaCrack.GetExcess(8), Is.EqualTo(3), "drei Risse auf einmal über der Grenze lösen die drei ältesten auf");
            Assert.That((LavaCrack with { MaxMainCracks = 0 }).GetExcess(50), Is.Zero, "0 heißt ohne Grenze");
        });
    }

    [Test]
    public void JedesZusaetzlicheProjektil_GibtEinenRiss()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LavaCrack.GetCount(0), Is.EqualTo(1));
            Assert.That(LavaCrack.GetCount(2), Is.EqualTo(3));
            Assert.That(LavaCrack.GetCount(-1), Is.EqualTo(1));
        });
    }

    [Test]
    public void Risse_FaechernMittigUmDieBlickrichtung()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LavaCrack.GetOffsetDegrees(0, 1), Is.Zero);
            Assert.That(LavaCrack.GetOffsetDegrees(0, 2), Is.EqualTo(-15f).Within(Tolerance));
            Assert.That(LavaCrack.GetOffsetDegrees(1, 2), Is.EqualTo(15f).Within(Tolerance));
            Assert.That(LavaCrack.GetOffsetDegrees(0, 3), Is.EqualTo(-30f).Within(Tolerance));
            Assert.That(LavaCrack.GetOffsetDegrees(1, 3), Is.Zero.Within(Tolerance));
            Assert.That(LavaCrack.GetOffsetDegrees(0, 11), Is.EqualTo(-90f).Within(Tolerance), "höchstens ein Halbkreis");
        });
    }

    [Test]
    public void Riss_WaechstIn0Komma4Sekunden_UndLiegt4Sekunden()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LavaCrack.GetGrownShare(0), Is.Zero);
            Assert.That(LavaCrack.GetGrownShare(0.2), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(LavaCrack.GetGrownShare(1), Is.EqualTo(1f));
            Assert.That((LavaCrack with { GrowSec = 0f }).GetGrownShare(0), Is.EqualTo(1f), "ohne Wachsen sofort ganz da");
            Assert.That(LavaCrack.IsOver(3.99), Is.False);
            Assert.That(LavaCrack.IsOver(4), Is.True);
            Assert.That(LavaCrack.HalfWidth, Is.EqualTo(25f));
        });
    }
}
