using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

//"Adds X to Y" auf der Waffe, Zusatzschaden der Elemente und gesenkte Anforderungen
[TestFixture]
public class SwordAffixMechanicsTests
{
    private static readonly ItemDefinition HeavySword = TestItems.Sword with
    {
        Requirements = new Dictionary<Requirement, int> { [Requirement.Strength] = 100, [Requirement.CharacterLevel] = 10 }
    };

    private static ItemAffix AddsLocal(CombatStat stat, float from, float to)
        => new(AffixType.Prefix, stat, ModificationType.Flat, from, "Adds", true, to);

    private static ItemAffix ReducedRequirements(float percent)
        => new(AffixType.Suffix, CombatStat.AttributeRequirements, ModificationType.Percentage, -percent / 100f, "of the Deserving", true);

    [Test]
    public void AddsXtoY_GibtDemUnterenUndDemOberenWertVerschiedeneZahlen()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(AddsLocal(CombatStat.PhysicalDamage, 2, 5));

        Assert.Multiple(() =>
        {
            Assert.That(sword.MinDamage, Is.EqualTo(4 + 2));
            Assert.That(sword.MaxDamage, Is.EqualTo(9 + 5));
        });
    }

    [Test]
    public void FlacherAffixOhneY_GibtBeidenDenselbenWert()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(new ItemAffix(AffixType.Prefix, CombatStat.PhysicalDamage, ModificationType.Flat, 3, "Rough", true));

        Assert.Multiple(() =>
        {
            Assert.That(sword.MinDamage, Is.EqualTo(4 + 3));
            Assert.That(sword.MaxDamage, Is.EqualTo(9 + 3));
        });
    }

    [Test]
    public void Elementaffix_GibtDerWaffeZusatzschaden()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(AddsLocal(CombatStat.FireDamage, 1, 3));
        sword.AddAffix(AddsLocal(CombatStat.LightningDamage, 1, 6));

        var profile = sword.ToWeaponProfile();

        Assert.Multiple(() =>
        {
            Assert.That(sword.AddedDamage.Fire, Is.EqualTo(new DamageRange(1, 3)));
            Assert.That(sword.AddedDamage.Frost.IsEmpty, Is.True);
            Assert.That(profile.AddedDamage, Is.EqualTo(sword.AddedDamage));
            Assert.That(profile.MinDamage, Is.EqualTo(4), "der physische Teil bleibt");
            Assert.That(sword.GetEquipModifiers().Select(modifier => modifier.AffectedStat), Has.None.EqualTo(CombatStat.FireDamage), "lokal, nicht im Stat-Blatt");
        });
    }

    [Test]
    public void Ruestung_HatKeinenZusatzschaden()
        => Assert.That(TestItems.Create(TestItems.Helmet).AddedDamage, Is.EqualTo(default(PerElement<DamageRange>)));

    [Test]
    public void ReducedAttributeRequirements_SenktNurDieAttribute()
    {
        var sword = TestItems.Create(HeavySword);

        sword.AddAffix(ReducedRequirements(18));

        Assert.Multiple(() =>
        {
            Assert.That(sword.Requirements[Requirement.Strength], Is.EqualTo(82));
            Assert.That(sword.Requirements[Requirement.CharacterLevel], Is.EqualTo(10), "das Level bleibt");
            Assert.That(TestItems.Create(HeavySword).Requirements, Is.SameAs(HeavySword.Requirements), "ohne Affix die der Basis");
        });
    }

    [Test]
    public void GesenkteAnforderung_EntscheidetUeberDasAnlegen()
    {
        var items   = new CharacterItems(6, 4, requirement => requirement == Requirement.Strength ? 90 : 10);
        var plain   = TestItems.Create(HeavySword);
        var reduced = TestItems.Create(HeavySword);

        reduced.AddAffix(ReducedRequirements(18));

        Assert.Multiple(() =>
        {
            Assert.That(items.CanEquip(plain), Is.False);
            Assert.That(items.GetUnmetRequirements(plain), Is.EqualTo(new[] { Requirement.Strength }));
            Assert.That(items.CanEquip(reduced), Is.True);
            Assert.That(items.GetUnmetRequirements(reduced), Is.Empty);
        });
    }

    [Test]
    public void StufeMitY_WuerfeltBeideWerteInIhrerSpanne()
    {
        var addsFire = new AffixDefinition(AffixType.Prefix, CombatStat.FireDamage, ModificationType.Flat)
        {
            AllowedSlots       = [ItemSlot.PhysicalWeapon],
            AllowedWeaponTypes = [WeaponType.Sword],
            IsLocal            = true,
            Tiers              = [new AffixTierDefinition(1, 1, 100, 1, 2, "Warmed", 3, 4)]
        };

        var roller = new AffixRoller([addsFire]);

        for (var seed = 0; seed < 100; seed++)
        {
            var sword = TestItems.Create(TestItems.Sword);

            roller.RollAffixesFor(sword, new SeededRandom(seed), 2, 2);

            var affix = sword.Affixes.Single();

            Assert.That(affix.Value, Is.InRange(1f, 2f), $"Seed {seed}");
            Assert.That(affix.ValueTo, Is.InRange(3f, 4f), $"Seed {seed}");
        }
    }

    [Test]
    public void StufeOhneY_WuerfeltKeinZweitesMal()
    {
        var strength = new AffixDefinition(AffixType.Suffix, CombatStat.Strength, ModificationType.Flat)
        {
            AllowedSlots = [ItemSlot.PhysicalWeapon],
            Tiers        = [new AffixTierDefinition(1, 1, 100, 8, 12, "of the Thug")]
        };

        var sword = TestItems.Create(TestItems.Sword);

        new AffixRoller([strength]).RollAffixesFor(sword, new SeededRandom(3), 2, 2);

        Assert.That(sword.Affixes.Single().ValueTo, Is.Zero);
    }

    [Test]
    public void AddsXtoY_UebersteheDasSpeichern()
    {
        var sword = TestItems.Create(TestItems.Sword);

        sword.AddAffix(AddsLocal(CombatStat.FireDamage, 2, 4));

        var json = SaveGameSerializer.Serialize(new SaveGame { Unplaced = [SaveGameMapper.ToSave(sword)] });

        Assert.That(SaveGameSerializer.TryDeserialize(json, out var loaded), Is.True);

        var restored = SaveGameMapper.ToItem(loaded.Unplaced.Single(), TestItems.Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(restored.Affixes.Single().ValueTo, Is.EqualTo(4f));
            Assert.That(restored.AddedDamage.Fire, Is.EqualTo(new DamageRange(2, 4)));
        });
    }

    [Test]
    public void AlterAffixOhneY_LaedtMitNull()
    {
        const string json = "{\"Version\":6,\"Unplaced\":[{\"BaseId\":\"sword\",\"Affixes\":[{\"Type\":\"Prefix\",\"Stat\":\"PhysicalDamage\",\"Modification\":\"Flat\",\"Value\":3,\"NameAddition\":\"Rough\",\"IsLocal\":true}]}]}";

        Assert.That(SaveGameSerializer.TryDeserialize(json, out var loaded), Is.True);

        var restored = SaveGameMapper.ToItem(loaded.Unplaced.Single(), TestItems.Catalog);

        Assert.Multiple(() =>
        {
            Assert.That(restored.Affixes.Single().HasRange, Is.False);
            Assert.That(restored.MaxDamage, Is.EqualTo(9 + 3));
        });
    }
}
