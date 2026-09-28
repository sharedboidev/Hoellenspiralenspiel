using System;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Stats;

[TestFixture]
public class StatSheetTests
{
    private const string Helmet = "Item:Helmet";
    private const string Gloves = "Item:Gloves";

    private static StatSheet CreateSheetWithAttributes(int value = 1)
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            foreach (var attribute in StatSheet.Attributes)
                s.SetBase(attribute, value);
        });

        return sheet;
    }

    private static float DerivedValue(StatSheet sheet, CombatStat stat, ModificationType type)
        => sheet.DerivedModifiers.Single(m => m.AffectedStat == stat && m.ModificationType == type).Value;

    #region Grundrechnung

    [Test]
    public void NeuesBlatt_HatNurDieFormelwerte()
    {
        var sheet = new StatSheet();

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinal(CombatStat.Strength), Is.Zero);
            Assert.That(sheet.GetEffectiveBase(CombatStat.Life), Is.EqualTo(5f));
            Assert.That(sheet.GetEffectiveBase(CombatStat.Mana), Is.EqualTo(3f));
            Assert.That(sheet.GetFinal(CombatStat.Armor), Is.EqualTo(2f).Within(0.1f), "Konstitution 0 gibt 2 flache Rüstung");
        });
    }

    [Test]
    public void Grundwert_WirdZumEndwert()
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.FireResistance, 75);

        Assert.That(sheet.GetFinalWhole(CombatStat.FireResistance), Is.EqualTo(75));
    }

    [Test]
    public void Modifier_WerdenNachArtVerrechnet()
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.FireResistance, 10);
        sheet.AddModifiers(
        [
            new CombatStatModifier(CombatStat.FireResistance, ModificationType.Flat, 10, Helmet),
            new CombatStatModifier(CombatStat.FireResistance, ModificationType.Percentage, 0.25f, Helmet),
            new CombatStatModifier(CombatStat.FireResistance, ModificationType.Percentage, 0.25f, Gloves),
            new CombatStatModifier(CombatStat.FireResistance, ModificationType.More, 0.5f, Helmet),
            new CombatStatModifier(CombatStat.FireResistance, ModificationType.More, 0.5f, Gloves)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetAddedFlat(CombatStat.FireResistance), Is.EqualTo(10f));
            Assert.That(sheet.GetIncreasedMultiplier(CombatStat.FireResistance), Is.EqualTo(1.5f).Within(0.0001f), "increased wird addiert");
            Assert.That(sheet.GetMoreMultiplier(CombatStat.FireResistance), Is.EqualTo(2.25f).Within(0.0001f), "more wird multipliziert");
            Assert.That(sheet.GetFinal(CombatStat.FireResistance), Is.EqualTo(67.5f).Within(0.001f));
            Assert.That(sheet.GetFinalWhole(CombatStat.FireResistance), Is.EqualTo(67));
        });
    }

    [Test]
    public void Zauberschaden_EntsprichtDemBeispielAusDemSpiel()
    {
        var sheet = CreateSheetWithAttributes();

        sheet.SetBase(CombatStat.Intelligence, 4);
        sheet.AddModifiers(
        [
            new CombatStatModifier(CombatStat.SpellDamage, ModificationType.Percentage, 0.14f, Gloves),
            new CombatStatModifier(CombatStat.SpellDamage, ModificationType.Percentage, 0.22f, "Item:Staff")
        ]);

        Assert.That((sheet.GetTotalMultiplier(CombatStat.SpellDamage) - 1) * 100, Is.EqualTo(40.04f).Within(0.01f));
    }

    #endregion

    #region Attribute und abgeleitete Werte

    [Test]
    public void Attribute_SindGanzeZahlen()
    {
        var sheet = new StatSheet();

        sheet.SetBase(CombatStat.Strength, 10);
        sheet.AddModifier(new CombatStatModifier(CombatStat.Strength, ModificationType.Percentage, 0.15f, Helmet));

        Assert.That(sheet.GetFinal(CombatStat.Strength), Is.EqualTo(11f));
    }

    [Test]
    public void LebenUndMana_FolgenDenFormelnAusDemDesigndokument()
    {
        var sheet = CreateSheetWithAttributes();

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetEffectiveBase(CombatStat.Life), Is.EqualTo(9f), "5 + S + 3*C");
            Assert.That(sheet.GetEffectiveBase(CombatStat.Mana), Is.EqualTo(9f), "3 + A + 5*I");
            Assert.That(sheet.GetFinalWhole(CombatStat.Life), Is.EqualTo(9));
            Assert.That(sheet.GetFinalWhole(CombatStat.Mana), Is.EqualTo(9));
        });
    }

    [Test]
    public void GrundwertVonLeben_IstEinZuschlagAufDieFormel()
    {
        var sheet = CreateSheetWithAttributes();

        sheet.SetBase(CombatStat.Life, 66);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetEffectiveBase(CombatStat.Life), Is.EqualTo(75f));
            Assert.That(sheet.GetFinalWhole(CombatStat.Life), Is.EqualTo(76), "75 mal 2,18 % more aus Konstitution 1");
        });
    }

    [Test]
    public void AttributAendern_AendertAbgeleiteteWerte()
    {
        var sheet      = CreateSheetWithAttributes();
        var lifeBefore = sheet.GetFinal(CombatStat.Life);
        var manaBefore = sheet.GetFinal(CombatStat.Mana);

        sheet.SetBase(CombatStat.Constitution, 20);
        sheet.SetBase(CombatStat.Intelligence, 20);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinal(CombatStat.Life), Is.GreaterThan(lifeBefore));
            Assert.That(sheet.GetFinal(CombatStat.Mana), Is.GreaterThan(manaBefore));
            Assert.That(sheet.GetEffectiveBase(CombatStat.Life), Is.EqualTo(66f));
            Assert.That(sheet.GetEffectiveBase(CombatStat.Mana), Is.EqualTo(104f));
        });
    }

    [Test]
    public void RuestungsbonusVonStaerkeUndKonstitution_BleibenBeideErhalten()
    {
        //Regression F1: beide Attribute teilten sich eine Herkunft und überschrieben sich
        var sheet = CreateSheetWithAttributes();

        sheet.SetBase(CombatStat.Strength, 30);
        sheet.SetBase(CombatStat.Constitution, 30);
        sheet.SetBase(CombatStat.Strength, 31);

        var flatFromConstitution = DerivedValue(sheet, CombatStat.Armor, ModificationType.Flat);
        var moreFromStrength     = DerivedValue(sheet, CombatStat.Armor, ModificationType.More);

        Assert.Multiple(() =>
        {
            Assert.That(flatFromConstitution, Is.GreaterThan(2f));
            Assert.That(moreFromStrength, Is.GreaterThan(0.02f));
            Assert.That(sheet.GetFinal(CombatStat.Armor), Is.EqualTo(flatFromConstitution * (1 + moreFromStrength)).Within(0.001f));
        });
    }

    [Test]
    public void AttributVonAusruestung_WirktAufAbgeleiteteWerte()
    {
        //Regression F2: nur der Grundwert löste die Neuberechnung aus
        var sheet      = CreateSheetWithAttributes();
        var moreBefore = DerivedValue(sheet, CombatStat.PhysicalDamage, ModificationType.More);
        var lifeBefore = sheet.GetFinal(CombatStat.Life);

        sheet.AddModifier(new CombatStatModifier(CombatStat.Strength, ModificationType.Flat, 50, Helmet));

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinalWhole(CombatStat.Strength), Is.EqualTo(51));
            Assert.That(DerivedValue(sheet, CombatStat.PhysicalDamage, ModificationType.More), Is.GreaterThan(moreBefore));
            Assert.That(sheet.GetFinal(CombatStat.Life), Is.GreaterThan(lifeBefore));
        });

        sheet.RemoveModifiersOf(Helmet);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinalWhole(CombatStat.Strength), Is.EqualTo(1));
            Assert.That(DerivedValue(sheet, CombatStat.PhysicalDamage, ModificationType.More), Is.EqualTo(moreBefore));
            Assert.That(sheet.GetFinal(CombatStat.Life), Is.EqualTo(lifeBefore));
        });
    }

    [Test]
    public void AbgeleiteteModifier_EntstehenNichtDoppelt()
    {
        var sheet = CreateSheetWithAttributes();

        for (var i = 2; i < 20; i++)
            sheet.SetBase(CombatStat.Awareness, i);

        Assert.That(sheet.DerivedModifiers, Has.Count.EqualTo(12), "2 + 2 + 2 + 2 + 4 abgeleitete Modifier");
    }

    [Test]
    public void Lichtradius_WaechstMitAwareness()
    {
        var sheet = CreateSheetWithAttributes();

        sheet.SetBase(CombatStat.LightRadius, 100);

        var radiusBefore = sheet.GetFinal(CombatStat.LightRadius);

        sheet.SetBase(CombatStat.Awareness, 50);

        Assert.Multiple(() =>
        {
            Assert.That(radiusBefore, Is.EqualTo(102f).Within(0.5f));
            Assert.That(sheet.GetFinal(CombatStat.LightRadius), Is.GreaterThan(radiusBefore));
        });
    }

    #endregion

    #region Herkunft von Modifiern

    [Test]
    public void IdentischeModifier_ZaehlenBeide()
    {
        //Regression F11: gleiche Einträge wurden beim Entfernen anderer Modifier zusammengefasst
        var sheet = CreateSheetWithAttributes();
        var twin  = new CombatStatModifier(CombatStat.Life, ModificationType.Flat, 10, Helmet);

        sheet.AddModifiers([twin, twin]);
        sheet.AddModifier(new CombatStatModifier(CombatStat.FireResistance, ModificationType.Flat, 5, Gloves));

        Assert.That(sheet.GetAddedFlat(CombatStat.Life), Is.EqualTo(20f));

        sheet.RemoveModifiersOf(Gloves);

        Assert.That(sheet.GetAddedFlat(CombatStat.Life), Is.EqualTo(20f));
    }

    [Test]
    public void RemoveModifiersOf_EntferntNurDieEineHerkunft()
    {
        var sheet = CreateSheetWithAttributes();

        sheet.AddModifiers(
        [
            new CombatStatModifier(CombatStat.Life, ModificationType.Flat, 10, Helmet),
            new CombatStatModifier(CombatStat.Mana, ModificationType.Flat, 10, Helmet),
            new CombatStatModifier(CombatStat.Life, ModificationType.Flat, 7, Gloves)
        ]);

        var removed = sheet.RemoveModifiersOf(Helmet);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(sheet.GetAddedFlat(CombatStat.Life), Is.EqualTo(7f));
            Assert.That(sheet.GetAddedFlat(CombatStat.Mana), Is.Zero);
            Assert.That(sheet.Modifiers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AbgeleiteteModifier_LassenSichNichtVonAussenEntfernen()
    {
        var sheet       = CreateSheetWithAttributes();
        var armorBefore = sheet.GetFinal(CombatStat.Armor);

        var removed = sheet.RemoveModifiersOf(DerivedStatProvider.GetOriginIdFor(CombatStat.Constitution));

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.Zero);
            Assert.That(sheet.GetFinal(CombatStat.Armor), Is.EqualTo(armorBefore));
        });
    }

    #endregion

    #region Zwischenspeicher und Ereignisse

    [Test]
    public void Lesen_LoestKeineNeuberechnungAus()
    {
        var sheet = CreateSheetWithAttributes();
        var count = sheet.RecalculationCount;

        for (var i = 0; i < 1000; i++)
        {
            foreach (var stat in Enum.GetValues<CombatStat>())
                _ = sheet.GetFinal(stat);
        }

        Assert.That(sheet.RecalculationCount, Is.EqualTo(count));
    }

    [Test]
    public void GleicherGrundwert_LoestKeineNeuberechnungAus()
    {
        var sheet  = CreateSheetWithAttributes();
        var count  = sheet.RecalculationCount;
        var events = 0;

        sheet.Changed += () => events++;
        sheet.SetBase(CombatStat.Strength, 1);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.RecalculationCount, Is.EqualTo(count));
            Assert.That(events, Is.Zero);
        });
    }

    [Test]
    public void Update_RechnetUndMeldetEinmal()
    {
        var sheet  = CreateSheetWithAttributes();
        var count  = sheet.RecalculationCount;
        var events = 0;

        sheet.Changed += () => events++;

        sheet.Update(s =>
        {
            s.SetBase(CombatStat.Strength, 5);
            s.SetBase(CombatStat.Armor, 20);
            s.AddModifier(new CombatStatModifier(CombatStat.Life, ModificationType.Flat, 10, Helmet));
            s.RemoveModifiersOf(Helmet);
        });

        Assert.Multiple(() =>
        {
            Assert.That(sheet.RecalculationCount, Is.EqualTo(count + 1));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(sheet.GetFinalWhole(CombatStat.Strength), Is.EqualTo(5));
        });
    }

    [Test]
    public void Update_OhneAenderung_MeldetNichts()
    {
        var sheet  = CreateSheetWithAttributes();
        var events = 0;

        sheet.Changed += () => events++;
        sheet.Update(_ => { });

        Assert.That(events, Is.Zero);
    }

    [Test]
    public void Changed_FeuertErstWennDieWerteAktuellSind()
    {
        var sheet        = CreateSheetWithAttributes();
        var manaInsideEvent = 0f;

        sheet.Changed += () => manaInsideEvent = sheet.GetFinal(CombatStat.Mana);
        sheet.SetBase(CombatStat.Intelligence, 2);

        Assert.That(manaInsideEvent, Is.EqualTo(sheet.GetFinal(CombatStat.Mana)).And.GreaterThan(9f));
    }

    [Test]
    public void FehlerInUpdate_LaesstDasBlattBenutzbar()
    {
        var sheet = CreateSheetWithAttributes();

        Assert.Throws<InvalidOperationException>(() => sheet.Update(_ => throw new InvalidOperationException()));

        sheet.SetBase(CombatStat.Strength, 3);

        Assert.That(sheet.GetFinalWhole(CombatStat.Strength), Is.EqualTo(3));
    }

    [Test]
    public void NullAlsModifier_WirdAbgelehnt()
    {
        var sheet = new StatSheet();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => sheet.AddModifier(null));
            Assert.Throws<ArgumentNullException>(() => sheet.AddModifiers([null]));
        });
    }

    #endregion
}
