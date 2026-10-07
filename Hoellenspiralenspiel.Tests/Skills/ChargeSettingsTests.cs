using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Die Werte hat der User am 07.10.2026 vorgegeben: 20 % je Sekunde, Schuss ab einem Drittel, 300 % Waffenschaden bei voll, bis 150 %,
//darüber durchstoßend, eine halbe Sekunde über dem Maximum verpufft der Schuss mit 5 s Abklingzeit
[TestFixture]
public class ChargeSettingsTests
{
    private const float Tolerance = 0.001f;

    private static readonly ChargeSettings ChargedShot = new(20f, 100f / 3f, 150f, 0.5f, 5);

    private static readonly AttackDefinition FullShot = new("Charged Shot", 300f);

    [Test]
    public void DieLadungWaechstMitDemTempo_UndEndetAmMaximum()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChargedShot.Advance(0f, 1), Is.EqualTo(20f).Within(Tolerance));
            Assert.That(ChargedShot.Advance(20f, 0.5), Is.EqualTo(30f).Within(Tolerance));
            Assert.That(ChargedShot.Advance(140f, 1), Is.EqualTo(150f), "mehr als das Maximum gibt es nicht");
            Assert.That(ChargedShot.Advance(150f, 1), Is.EqualTo(150f));
            Assert.That(ChargedShot.Advance(10f, -1), Is.EqualTo(10f), "rückwärts lädt nichts");
        });
    }

    //Nachtrag des Users vom 07.10.2026: Angriffstempo verkürzt die Ladezeit proportional
    [Test]
    public void ErhoehtesAngriffstempo_LaedtImSelbenVerhaeltnisSchneller()
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.Attackspeed, 1.2f);

        var plain = ChargeSettings.GetRateFactor(sheet);

        sheet.AddModifier(new CombatStatModifier(CombatStat.Attackspeed, ModificationType.Percentage, 0.5f, "Item:Gloves"));

        var faster = ChargeSettings.GetRateFactor(sheet);

        Assert.Multiple(() =>
        {
            Assert.That(faster / plain, Is.EqualTo(1.5f).Within(Tolerance), "50 % mehr Angriffstempo");
            Assert.That(ChargedShot.Advance(0f, 1, faster), Is.EqualTo(20f * faster).Within(Tolerance));
            Assert.That(ChargedShot.GetSecToReach(100f, 2f), Is.EqualTo(2.5).Within(Tolerance), "doppeltes Tempo, halbe Zeit");
            Assert.That(ChargedShot.GetSecToReach(100f, plain), Is.EqualTo(5 / plain).Within(Tolerance));
            Assert.That(ChargedShot.Advance(0f, 1, 0f), Is.Zero, "ohne Faktor lädt nichts, der Held hält den Faktor über 0,1");
        });
    }

    [Test]
    public void DerFaktor_FaelltNieUnterEinZehntel()
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.Attackspeed, 1.2f);
        sheet.AddModifier(new CombatStatModifier(CombatStat.Attackspeed, ModificationType.Percentage, -5f, "Item:Curse"));

        Assert.That(ChargeSettings.GetRateFactor(sheet), Is.EqualTo(0.1f));
    }

    [Test]
    public void OhneStatBlatt_WirdGeworfen()
        => Assert.That(() => ChargeSettings.GetRateFactor(null), Throws.ArgumentNullException);

    [Test]
    public void UnterEinemDrittel_GehtKeinSchussLos()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChargedShot.CanFire(33f), Is.False);
            Assert.That(ChargedShot.CanFire(100f / 3f), Is.True);
            Assert.That(ChargedShot.CanFire(150f), Is.True);
        });
    }

    [Test]
    public void DerSchadenWaechstLinearMitDerLadung()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChargedShot.GetAttack(FullShot, 100f / 3f).WeaponDamagePercent, Is.EqualTo(100f).Within(Tolerance), "ein Drittel geladen gibt 100 %");
            Assert.That(ChargedShot.GetAttack(FullShot, 100f).WeaponDamagePercent, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(ChargedShot.GetAttack(FullShot, 150f).WeaponDamagePercent, Is.EqualTo(450f).Within(Tolerance));
            Assert.That(ChargedShot.GetAttack(FullShot, 50f).WeaponDamagePercent, Is.EqualTo(150f).Within(Tolerance));
            Assert.That(ChargedShot.GetAttack(FullShot, 100f).Name, Is.EqualTo("Charged Shot"), "nur der Anteil ändert sich");
        });
    }

    [Test]
    public void DerSchadenWirdNieNegativ()
        => Assert.That(ChargedShot.GetDamageFactor(-10f), Is.Zero);

    [Test]
    public void OhneAngriff_WirdGeworfen()
        => Assert.That(() => ChargedShot.GetAttack(null, 100f), Throws.ArgumentNullException);

    [Test]
    public void ErstUeberVollerLadung_DurchstoesstDerPfeil()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChargedShot.Pierces(100f), Is.False, "genau voll durchstößt noch nicht");
            Assert.That(ChargedShot.Pierces(100.01f), Is.True);
            Assert.That(ChargedShot.Pierces(150f), Is.True);
        });
    }

    [Test]
    public void DasMaximumDarfEineHalbeSekundeGehaltenWerden()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChargedShot.IsAtMax(149.9f), Is.False);
            Assert.That(ChargedShot.IsAtMax(150f), Is.True);
            Assert.That(ChargedShot.IsOverheld(150f, 0.49), Is.False);
            Assert.That(ChargedShot.IsOverheld(150f, 0.5), Is.True);
            Assert.That(ChargedShot.IsOverheld(100f, 10), Is.False, "unter dem Maximum läuft keine Frist");
        });
    }

    [Test]
    public void DieZeitenErgebenSichAusDemTempo()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChargedShot.GetSecToReach(100f / 3f), Is.EqualTo(5.0 / 3).Within(Tolerance), "ein Drittel nach 1,67 s");
            Assert.That(ChargedShot.GetSecToReach(ChargeSettings.FullPercent), Is.EqualTo(5).Within(Tolerance));
            Assert.That(ChargedShot.GetSecToReach(150f), Is.EqualTo(7.5).Within(Tolerance));
            Assert.That((ChargedShot with { RatePerSec = 0f }).GetSecToReach(100f), Is.EqualTo(double.PositiveInfinity), "ohne Tempo lädt nichts");
        });
    }

    [Test]
    public void DieDarstellungBeginntBeiDerMindestladung_UndIstAmMaximumVoll()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChargedShot.GetShownShare(0f), Is.Zero);
            Assert.That(ChargedShot.GetShownShare(33f), Is.Zero, "knapp unter dem Drittel noch nichts");
            Assert.That(ChargedShot.GetShownShare(100f / 3f), Is.Zero.Within(Tolerance));
            Assert.That(ChargedShot.GetShownShare(100f), Is.EqualTo((100f - 100f / 3f) / (150f - 100f / 3f)).Within(Tolerance));
            Assert.That(ChargedShot.GetShownShare(150f), Is.EqualTo(1f));
            Assert.That(ChargedShot.GetShownShare(200f), Is.EqualTo(1f));
        });
    }

    [Test]
    public void EinSchussMitGleicherMindestUndHoechstladung_IstSofortVoll()
    {
        var instant = ChargedShot with { MinPercent = 100f, MaxPercent = 100f };

        Assert.That(instant.GetShownShare(100f), Is.EqualTo(1f));
    }

    [Test]
    public void EineLadungVonSechzigTicks_ErreichtDasDrittelNachGutAnderthalbSekunden()
    {
        const double tickSec = 1.0 / 60;

        var percent = 0f;
        var ticks   = 0;

        while (!ChargedShot.CanFire(percent))
        {
            percent = ChargedShot.Advance(percent, tickSec);
            ticks++;
        }

        Assert.That(ticks, Is.EqualTo(100).Within(1), "20 % je Sekunde: das Drittel liegt bei 100 Ticks zu einer Sechzigstelsekunde, Rundung hin oder her");
    }
}
