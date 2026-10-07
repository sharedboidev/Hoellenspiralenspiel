using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

//Die Werte hat der User am 07.10.2026 vorgegeben: Mana je Sekunde, solange die Taste gehalten wird, Ticks im Takt des Angriffstempos
//mal einem Faktor aus dem Inspector, jeder Tick mit dem Waffenschaden des Skills auf alle in der 1,25-fachen Reichweite
[TestFixture]
public class ChannelSettingsTests
{
    private const float Tolerance = 0.0001f;

    private static readonly ChannelSettings Typhoon = new(3f);

    [Test]
    public void TicksFolgenDemAngriffstempo_MalDemFaktor()
    {
        var doubled = new ChannelSettings(3f, 2f);

        Assert.Multiple(() =>
        {
            Assert.That(Typhoon.GetTicksPerSec(1.4f), Is.EqualTo(1.4).Within(Tolerance), "ein Tick je Angriff");
            Assert.That(Typhoon.GetIntervalSec(1.4f), Is.EqualTo(1 / 1.4).Within(Tolerance));
            Assert.That(doubled.GetTicksPerSec(1.4f), Is.EqualTo(2.8).Within(Tolerance), "zwei Ticks je Angriff");
            Assert.That(Typhoon.GetTicksPerSec(0f), Is.EqualTo(CombatRules.MinAttacksPerSecond).Within(Tolerance), "langsamer als das Mindesttempo wird es nicht");
            Assert.That(new ChannelSettings(3f, 0f).GetTicksPerSec(1f), Is.EqualTo(0.1).Within(Tolerance), "ein Faktor von 0 fällt auf den kleinsten zurück");
        });
    }

    //Der erste Tick kommt nach der Hälfte eines Intervalls wie der Treffer eines Schlags nach der Hälfte seiner Dauer
    [Test]
    public void DerErsteTickKommtNachEinemHalbenIntervall()
        => Assert.That(Typhoon.GetFirstTickSec(2f), Is.EqualTo(0.25).Within(Tolerance));

    [Test]
    public void ManaFliesstJeSekunde_UndJeTickDerAnteil()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Typhoon.GetManaFor(1), Is.EqualTo(3f).Within(Tolerance));
            Assert.That(Typhoon.GetManaFor(1.0 / 60), Is.EqualTo(0.05f).Within(Tolerance), "ein Physik-Takt");
            Assert.That(Typhoon.GetManaFor(-1), Is.Zero, "rückwärts kostet nichts");
            Assert.That(Typhoon.GetManaPerTick(1.5f), Is.EqualTo(2f).Within(Tolerance), "bei 1,5 Ticks je Sekunde kostet ein Tick 2 Mana");
            Assert.That(new ChannelSettings(-3f).GetManaFor(1), Is.Zero, "negative Kosten gibt es nicht");
        });
    }

    //Zum Beginnen muss das Mana bis zum ersten Tick reichen, danach läuft der Wirbel, solange der Takt bezahlt ist.
    //Sonst hielte eine Regeneration von einem halben Mana je Sekunde den Wirbel bei null ewig am Leben, der Takt kostet mehr als er bringt
    [Test]
    public void BeginnenBrauchtDasManaBisZumErstenTick_WeiterGehtEsNurMitDemManaDesTakts()
    {
        const double tick = 1.0 / 60;

        Assert.Multiple(() =>
        {
            Assert.That(Typhoon.CanStart(0.75f, 2f), Is.True, "0,25 s bis zum ersten Tick kosten 0,75 Mana");
            Assert.That(Typhoon.CanStart(0.7f, 2f), Is.False);
            Assert.That(Typhoon.CanContinue(0.051f, tick), Is.True, "ein Takt kostet 0,05");
            Assert.That(Typhoon.CanContinue(0.04f, tick), Is.False);
            Assert.That(Typhoon.CanContinue(0f, tick), Is.False);
            Assert.That(new ChannelSettings(0f).CanStart(0f, 2f), Is.True, "ohne Kosten geht es immer");
            Assert.That(new ChannelSettings(0f).CanContinue(0f, tick), Is.True);
        });
    }

    //Eine Umdrehung je Tick: Die Waffe streicht zwischen zwei Treffern einmal an jedem vorbei
    [Test]
    public void DerHeldDrehtSichEinmalJeTick()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Typhoon.GetSpinDegreesPerSec(1f), Is.EqualTo(360).Within(Tolerance));
            Assert.That(new ChannelSettings(3f, 2f).GetSpinDegreesPerSec(1.5f), Is.EqualTo(1080).Within(Tolerance));
        });
    }

    //Das finale Angriffstempo aus dem Stat-Blatt, also samt Waffe und Modifikatoren
    [Test]
    public void DasAngriffstempoKommtAusDemStatBlatt()
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.Attackspeed, 1.4f);

        var plain = ChannelSettings.GetAttacksPerSec(sheet);

        sheet.AddModifier(new CombatStatModifier(CombatStat.Attackspeed, ModificationType.Percentage, 0.5f, "Item:Gear"));

        Assert.Multiple(() =>
        {
            Assert.That(plain, Is.EqualTo(sheet.GetFinal(CombatStat.Attackspeed) / 1.5f).Within(0.001f));
            Assert.That(ChannelSettings.GetAttacksPerSec(sheet), Is.EqualTo(sheet.GetFinal(CombatStat.Attackspeed)).Within(Tolerance));
            Assert.That(ChannelSettings.GetAttacksPerSec(new StatSheet()), Is.EqualTo(CombatRules.MinAttacksPerSecond).Within(Tolerance), "ohne Tempo gilt das Mindesttempo");
        });
    }

    [Test]
    public void DieUhrZaehltDenErstenTickNachDemVorlauf_DanachJeIntervall()
    {
        var clock = new ChannelClock();

        clock.Start(0.25);

        Assert.Multiple(() =>
        {
            Assert.That(clock.IsRunning, Is.True);
            Assert.That(clock.Advance(0.125, 0.5), Is.Zero, "noch vor dem ersten Tick");
            Assert.That(clock.Advance(0.125, 0.5), Is.EqualTo(1), "der erste Tick");
            Assert.That(clock.Advance(0.375, 0.5), Is.Zero);
            Assert.That(clock.Advance(0.125, 0.5), Is.EqualTo(1), "der zweite nach einem Intervall");
            Assert.That(clock.Advance(1.0, 0.5), Is.EqualTo(2), "ein großer Schritt holt zwei Ticks nach");
        });
    }

    [Test]
    public void DieUhrStehtNachDemStopp_UndHoltHoechstensZehnTicksNach()
    {
        var clock = new ChannelClock();

        clock.Start(0);

        Assert.Multiple(() =>
        {
            Assert.That(clock.Advance(0, 0.5), Is.EqualTo(1), "ohne Vorlauf tickt es sofort");
            Assert.That(clock.Advance(100, 0.5), Is.EqualTo(10), "mehr als zehn auf einmal gibt es nicht");
            Assert.That(clock.Advance(1, 0), Is.EqualTo(10), "ein Intervall von 0 läuft nicht ewig");
        });

        clock.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(clock.IsRunning, Is.False);
            Assert.That(clock.Advance(5, 0.5), Is.Zero);
        });
    }
}
