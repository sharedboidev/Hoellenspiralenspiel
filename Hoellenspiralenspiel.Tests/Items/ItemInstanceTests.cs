using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class ItemInstanceTests
{
    [Test]
    public void ItemOhneAffixe_IstNormal()
        => Assert.That(TestItems.Create(TestItems.Sword).Rarity, Is.EqualTo(ItemRarity.Normal));

    [Test]
    public void EinPrefixUndEinSuffix_SindMagic()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(TestItems.Local(CombatStat.PhysicalDamage, ModificationType.Flat, 3));
        sword.AddAffix(TestItems.Global(CombatStat.Strength, ModificationType.Flat, 2));

        Assert.That(sword.Rarity, Is.EqualTo(ItemRarity.Magic));
    }

    [Test]
    public void ZweiAffixeDerselbenArt_SindRare()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(TestItems.Global(CombatStat.Strength, ModificationType.Flat, 2));
        sword.AddAffix(TestItems.Global(CombatStat.Life, ModificationType.Flat, 9));

        Assert.That(sword.Rarity, Is.EqualTo(ItemRarity.Rare));
    }

    [Test]
    public void AffixedName_StelltPrefixUndSuffixUmDenNamen()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(new ItemAffix(AffixType.Suffix, CombatStat.Strength, ModificationType.Flat, 2, " of the Ape", false));
        sword.AddAffix(new ItemAffix(AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Flat, 3, "Rough", true));

        Assert.That(sword.AffixedName, Is.EqualTo("Rough Sword of the Ape"));
    }

    [Test]
    public void AffixedName_OhnePrefix_BeginntMitDemNamen()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(new ItemAffix(AffixType.Suffix, CombatStat.Strength, ModificationType.Flat, 2, "of the Ape", false));

        Assert.That(sword.AffixedName, Is.EqualTo("Sword of the Ape"));
    }

    [Test]
    public void LokaleAffixe_VeraendernDenWaffenschaden()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(TestItems.Local(CombatStat.PhysicalDamage, ModificationType.Flat, 2));
        sword.AddAffix(TestItems.Local(CombatStat.PhysicalDamage, ModificationType.Percentage, 0.5f));

        Assert.Multiple(() =>
        {
            Assert.That(sword.MinDamage, Is.EqualTo(9));
            Assert.That(sword.MaxDamage, Is.EqualTo(16));
        });
    }

    [Test]
    public void LokaleAffixe_VeraendernAngriffstempoUndKrit()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(TestItems.Local(CombatStat.Attackspeed, ModificationType.Flat, 0.1f));
        sword.AddAffix(TestItems.Local(CombatStat.CriticalHitChance, ModificationType.More, 1f));

        Assert.Multiple(() =>
        {
            Assert.That(sword.AttacksPerSecond, Is.EqualTo(1.5).Within(0.001));
            Assert.That(sword.CriticalHitChance, Is.EqualTo(10).Within(0.001));
        });
    }

    [Test]
    public void GlobaleAffixe_LassenDieWerteDesItemsUnberuehrt()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(TestItems.Global(CombatStat.PhysicalDamage, ModificationType.Flat, 50));

        Assert.That(sword.MaxDamage, Is.EqualTo(9));
    }

    [Test]
    public void LokaleAffixe_VeraendernDenRuestungswert()
    {
        var helmet = TestItems.Create(TestItems.Helmet);

        helmet.AddAffix(TestItems.Local(CombatStat.Armor, ModificationType.Flat, 6));
        helmet.AddAffix(TestItems.Local(CombatStat.Armor, ModificationType.Percentage, 0.25f));

        Assert.That(helmet.ArmorValue, Is.EqualTo(20));
    }

    [Test]
    public void ToWeaponProfile_UebernimmtDieWerteDerWaffe()
    {
        var profile = TestItems.Create(TestItems.Bow).ToWeaponProfile();

        Assert.Multiple(() =>
        {
            Assert.That(profile.MinDamage, Is.EqualTo(5));
            Assert.That(profile.MaxDamage, Is.EqualTo(11));
            Assert.That(profile.AttacksPerSecond, Is.EqualTo(1.2f));
            Assert.That(profile.CriticalHitChance, Is.EqualTo(6));
            Assert.That(profile.DamageType, Is.EqualTo(DamageType.Pierce));
            Assert.That(profile.Range, Is.EqualTo(700));
            Assert.That(profile.IsRanged, Is.True);
            Assert.That(profile.ProjectileSpeed, Is.EqualTo(1400));
        });
    }

    [TestCase(WeaponType.Sword, DamageType.Slash)]
    [TestCase(WeaponType.Axe, DamageType.Slash)]
    [TestCase(WeaponType.Bow, DamageType.Pierce)]
    [TestCase(WeaponType.Dagger, DamageType.Pierce)]
    [TestCase(WeaponType.Staff, DamageType.Crush)]
    [TestCase(WeaponType.Flail, DamageType.Crush)]
    public void Waffentyp_BestimmtDieSchadensart(WeaponType weaponType, DamageType expected)
        => Assert.That(new WeaponStats(1, 2, 1, 5, weaponType, WieldStrategy.OneHand).DamageType, Is.EqualTo(expected));

    [Test]
    public void ToWeaponProfile_OhneWaffe_LiefertNichts()
        => Assert.That(TestItems.Create(TestItems.Helmet).ToWeaponProfile(), Is.Null);

    [Test]
    public void Ruestung_GibtIhrenWertAnDasStatBlatt()
    {
        var helmet = TestItems.Create(TestItems.Helmet);

        helmet.AddAffix(TestItems.Local(CombatStat.Armor, ModificationType.Flat, 5));

        var expected = new CombatStatModifier(CombatStat.Armor, ModificationType.Flat, 15, helmet.InstanceId);

        Assert.That(helmet.GetEquipModifiers(), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public void Schild_GibtRuestungUndBlock()
    {
        var shield = TestItems.Create(TestItems.Shield);

        var modifiers = shield.GetEquipModifiers().ToDictionary(modifier => modifier.AffectedStat, modifier => modifier.Value);

        Assert.Multiple(() =>
        {
            Assert.That(modifiers[CombatStat.Armor], Is.EqualTo(8));
            Assert.That(modifiers[CombatStat.MeleeBlock], Is.EqualTo(15));
            Assert.That(modifiers[CombatStat.SpellBlock], Is.EqualTo(8));
            Assert.That(modifiers.ContainsKey(CombatStat.MeleeParry), Is.False);
        });
    }

    [Test]
    public void Schwert_GibtParryAberKeineRuestung()
    {
        var sword = TestItems.Create(TestItems.Sword);

        var expected = new CombatStatModifier(CombatStat.MeleeParry, ModificationType.Flat, 5, sword.InstanceId);

        Assert.That(sword.GetEquipModifiers(), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public void GlobaleAffixe_GehenAnDasStatBlatt_LokaleNicht()
    {
        var sword = TestItems.Create(TestItems.Bow);

        sword.AddAffix(TestItems.Local(CombatStat.PhysicalDamage, ModificationType.Flat, 3));
        sword.AddAffix(TestItems.Global(CombatStat.Strength, ModificationType.Flat, 2));

        var expected = new CombatStatModifier(CombatStat.Strength, ModificationType.Flat, 2, sword.InstanceId);

        Assert.That(sword.GetEquipModifiers(), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public void AngelegterSchild_ErhoehtDenBlockImStatBlatt()
    {
        var stats  = new StatSheet();
        var shield = TestItems.Create(TestItems.Shield);

        stats.AddModifiers(shield.GetEquipModifiers());

        var blockWithShield = stats.GetFinal(CombatStat.MeleeBlock);

        stats.RemoveModifiersOf(shield.InstanceId);

        Assert.Multiple(() =>
        {
            Assert.That(blockWithShield, Is.GreaterThanOrEqualTo(15));
            Assert.That(stats.GetFinal(CombatStat.MeleeBlock), Is.Zero);
        });
    }

    [Test]
    public void StackSize_BleibtInDenGrenzenDerBasis()
    {
        var potions = TestItems.Create(TestItems.Potion, 9);

        Assert.That(potions.StackSize, Is.EqualTo(5));

        potions.StackSize = -3;

        Assert.That(potions.StackSize, Is.Zero);
    }

    [Test]
    public void CanStackWith_NurGleicheStapelbareBasis()
    {
        var potions = TestItems.Create(TestItems.Potion, 2);

        Assert.Multiple(() =>
        {
            Assert.That(potions.CanStackWith(TestItems.Create(TestItems.Potion)), Is.True);
            Assert.That(potions.CanStackWith(potions), Is.False);
            Assert.That(potions.CanStackWith(TestItems.Create(TestItems.Sword)), Is.False);
            Assert.That(TestItems.Create(TestItems.Sword).CanStackWith(TestItems.Create(TestItems.Sword)), Is.False);
        });
    }

    [Test]
    public void JedeInstanz_HatEineEigeneId()
        => Assert.That(TestItems.Create(TestItems.Sword).InstanceId, Is.Not.EqualTo(TestItems.Create(TestItems.Sword).InstanceId));

    [Test]
    public void Anforderungen_NennenWasFehlt()
    {
        var unmet = ItemRequirements.GetUnmet(TestItems.Sword, _ => 1);

        Assert.Multiple(() =>
        {
            Assert.That(unmet, Is.EqualTo(new[] { Requirement.Strength }));
            Assert.That(ItemRequirements.AreMet(TestItems.Sword, _ => 2), Is.True);
            Assert.That(ItemRequirements.AreMet(TestItems.Helmet, _ => 0), Is.True);
        });
    }

    [Test]
    public void ItemBasisOhneId_IstNichtErlaubt()
        => Assert.That(() => ItemDefinition.ForArmor(" ", "Nothing", ItemSlot.Helmet, 1), Throws.ArgumentException);
}
