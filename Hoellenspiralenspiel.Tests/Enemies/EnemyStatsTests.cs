using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using Hoellenspiralenspiel.Tests.Items;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Enemies;

//Die Erwartungen stammen aus dem laufenden Spiel: Die Laufzeitprüfung der Etappe 2 von M8 las sie an gespawnten Gegnern ab
[TestFixture]
public class EnemyStatsTests
{
    private static readonly EnemyDefinition Skeleton = new("skeleton", "Skeleton")
    {
        Strength      = new AttributeGrowth(1, 1f),
        Dexterity     = new AttributeGrowth(1, 0.5f),
        Constitution  = new AttributeGrowth(1, 1f),
        LifeBonus     = 40,
        Movementspeed = 90f,
        DamageMin     = 3f,
        DamageMax     = 5f,
        DamageType    = DamageType.Slash,
        AttackRange   = 40f
    };

    private static readonly EnemyDefinition BlueBlob = new("blue_blob", "Blue Blob")
    {
        Strength        = new AttributeGrowth(1, 1f),
        Constitution    = new AttributeGrowth(1, 1f),
        Movementspeed   = 25f,
        FrostResistance = 75,
        DamageType      = DamageType.Frost,
        AttackRange     = 30f
    };

    private static readonly MonsterModDefinition Stalwart = new("stalwart", "Stalwart")
    {
        Modifiers = [new CombatStatModifier(CombatStat.Life, ModificationType.More, 0.6f)]
    };

    private static StatSheet SheetOf(EnemyDefinition definition, int level, params MonsterModDefinition[] mods)
    {
        var sheet = new StatSheet();

        UnitBaseValues.Apply(sheet);
        EnemyStats.Apply(sheet, definition, level, mods);

        return sheet;
    }

    [Test]
    public void Skelett_HatAufLevel1_50Leben()
    {
        var sheet = SheetOf(Skeleton, 1);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinalWhole(CombatStat.Life), Is.EqualTo(50), "5 + 1 + 3 aus den Attributen, 40 Zuschlag, 2 % mehr aus Konstitution 1");
            Assert.That(sheet.GetFinal(CombatStat.Movementspeed), Is.EqualTo(90f));
            Assert.That(sheet.GetFinal(CombatStat.Dodge), Is.GreaterThan(6f), "Geschick verstärkt den Grundwert 6");
        });
    }

    [Test]
    public void Skelett_WaechstBisLevel6_AufStaerke6_Geschick3_Konstitution6_Und71Leben()
    {
        var sheet = SheetOf(Skeleton, 6);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinalWhole(CombatStat.Strength), Is.EqualTo(6));
            Assert.That(sheet.GetFinalWhole(CombatStat.Dexterity), Is.EqualTo(3), "Ein halber Punkt je Level wird abgerundet");
            Assert.That(sheet.GetFinalWhole(CombatStat.Constitution), Is.EqualTo(6));
            Assert.That(sheet.GetFinalWhole(CombatStat.Life), Is.EqualTo(71));
            Assert.That(sheet.GetFinalWhole(CombatStat.Liferegeneration), Is.EqualTo(3), "Stärke 6 / 5 + Konstitution 6 / 3, abgerundet");
        });
    }

    [Test]
    public void BlueBlob_HatFrostresistenz75_Und9Leben()
    {
        var sheet = SheetOf(BlueBlob, 1);

        Assert.Multiple(() =>
        {
            Assert.That(sheet.GetFinal(CombatStat.FrostResistance), Is.EqualTo(75f));
            Assert.That(sheet.GetFinal(CombatStat.FireResistance), Is.Zero);
            Assert.That(sheet.GetFinalWhole(CombatStat.Life), Is.EqualTo(9));
        });
    }

    [Test]
    public void Mods_LegenIhreModifierMitHerkunftAn()
    {
        var plain    = SheetOf(Skeleton, 3);
        var modified = SheetOf(Skeleton, 3, Stalwart);

        Assert.Multiple(() =>
        {
            Assert.That(modified.GetFinal(CombatStat.Life), Is.EqualTo(plain.GetFinal(CombatStat.Life) * 1.6f).Within(0.001f));
            Assert.That(modified.Modifiers.Single().OriginId, Is.EqualTo("monster-mod:stalwart"));
        });
    }

    [Test]
    public void Ausruestung_LegtIhreWerteAn_UndDieWaffeErsetztDieNatuerliche()
    {
        var armed = Skeleton with { Equipment = [TestItems.Shield, TestItems.Sword] };
        var plain = SheetOf(Skeleton, 2);
        var sheet = SheetOf(armed, 2);

        Assert.Multiple(() =>
        {
            Assert.That(armed.WieldedWeapon, Is.EqualTo(TestItems.Sword), "Der Schild steht vorn, geführt wird die erste Waffe");
            Assert.That(sheet.GetAddedFlat(CombatStat.Armor) - plain.GetAddedFlat(CombatStat.Armor), Is.EqualTo(8f), "Rüstung des Schilds");
            Assert.That(sheet.GetFinal(CombatStat.MeleeParry), Is.GreaterThan(0f), "Parieren des Schwerts");
            Assert.That(EnemyStats.GetWeapon(armed, 2).MinDamage, Is.EqualTo(4f));
            Assert.That(EnemyStats.GetWeapon(armed, 2).DamageType, Is.EqualTo(DamageType.Slash));
        });
    }

    [Test]
    public void NatuerlicheWaffe_FolgtSchadenSchadensartUndReichweite()
    {
        var weapon = EnemyStats.GetWeapon(Skeleton, 4);

        Assert.That(weapon, Is.EqualTo(new WeaponProfile(3f, 5f, 1f, 5f, DamageType.Slash, 40f)));
    }

    [Test]
    public void Projektile_NutztWerProjektilSkillsOderOhneSkillsEinenBogenFuehrt()
    {
        var spit = SkillDefinition.ForSpell("spit", new SpellDefinition("Spit", 3, 6, DamageType.Fire, 5)) with { Delivery = SkillDelivery.Projectile };
        var nova = SkillDefinition.ForSpell("nova", new SpellDefinition("Nova", 3, 6, DamageType.Frost, 5)) with { Delivery = SkillDelivery.AreaAroundCaster };

        Assert.Multiple(() =>
        {
            Assert.That(Skeleton.UsesProjectiles, Is.False);
            Assert.That((Skeleton with { Skills = [nova, spit] }).UsesProjectiles, Is.True);
            Assert.That((Skeleton with { Skills = [nova] }).UsesProjectiles, Is.False);
            Assert.That((Skeleton with { Equipment = [TestItems.Bow] }).UsesProjectiles, Is.True);
            Assert.That((Skeleton with { Equipment = [TestItems.Bow], Skills = [nova] }).UsesProjectiles, Is.False);
        });
    }

    [Test]
    public void OhneAnzeigename_HeisstDerGegnerNachSeinerId()
        => Assert.That(new EnemyDefinition("ghoul", " ").Name, Is.EqualTo("ghoul"));
}
