using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

//Hybride Affixe, Bonusschaden für Angriffe und Zauber, Block des Schilds, Pfeile des Bogens, Zaubertempo und Krit für Zauber
[TestFixture]
public class HybridAndGlobalAffixTests
{
    private const string Gear      = "Item:Gear";
    private const float  Tolerance = 0.001f;

    private static ItemAffix ArmourAndLife(float armour, float life)
        => new(AffixType.Prefix, CombatStat.Armor, ModificationType.Flat, armour, "Clam's", true, 0f, new ItemAffixLine(CombatStat.Life, ModificationType.Flat, life, false));

    private static StatSheet Attacker(params CombatStatModifier[] modifiers)
    {
        var sheet = new StatSheet();

        sheet.Update(s =>
        {
            s.SetBase(CombatStat.HitChance, CombatRules.BaseHitChance);
            s.SetBase(CombatStat.CriticalDamage, CombatRules.BaseCriticalDamage);
        });

        sheet.AddModifiers(modifiers);

        return sheet;
    }

    private static CombatStatModifier Modifier(CombatStat stat, float value, ModificationType modification = ModificationType.Flat)
        => new(stat, modification, value, Gear);

    #region Hybride Affixe

    [Test]
    public void HybriderAffix_ErhoehtLokalDieRuestungUndGlobalDasLeben()
    {
        var helmet = TestItems.Create(TestItems.Helmet);

        helmet.AddAffix(ArmourAndLife(20, 18));

        var modifiers = helmet.GetEquipModifiers();

        Assert.Multiple(() =>
        {
            Assert.That(helmet.ArmorValue, Is.EqualTo(10 + 20));
            Assert.That(modifiers.Single(modifier => modifier.AffectedStat == CombatStat.Armor).Value, Is.EqualTo(30f));
            Assert.That(modifiers.Single(modifier => modifier.AffectedStat == CombatStat.Life).Value, Is.EqualTo(18f));
        });
    }

    [Test]
    public void HybriderAffix_WuerfeltBeideWerteInIhrerSpanne()
    {
        var hybrid = new AffixDefinition(AffixType.Prefix, CombatStat.Armor, ModificationType.Flat)
        {
            AllowedSlots = [ItemSlot.Helmet],
            IsLocal      = true,
            Hybrid       = new AffixHybridDefinition(CombatStat.Life, ModificationType.Flat, false),
            Tiers        = [new AffixTierDefinition(1, 1, 100, 20, 32, "Clam's", HybridMinValue: 18, HybridMaxValue: 23)]
        };

        var roller = new AffixRoller([hybrid]);

        for (var seed = 0; seed < 100; seed++)
        {
            var helmet = TestItems.Create(TestItems.Helmet);

            roller.RollAffixesFor(helmet, new SeededRandom(seed), 2, 2);

            var affix = helmet.Affixes.Single();

            Assert.That(affix.Value, Is.InRange(20f, 32f), $"Seed {seed}");
            Assert.That(affix.Hybrid.Value, Is.InRange(18f, 23f), $"Seed {seed}");
            Assert.That(affix.Hybrid.Stat, Is.EqualTo(CombatStat.Life), $"Seed {seed}");
        }
    }

    [Test]
    public void HybriderAffix_GehoertZuEinerEigenenFamilie()
    {
        var helmet = TestItems.Create(TestItems.Helmet);

        helmet.AddAffix(new ItemAffix(AffixType.Prefix, CombatStat.Armor, ModificationType.Flat, 10, "Varnished", true));

        Assert.Multiple(() =>
        {
            Assert.That(helmet.HasAffixLike(AffixType.Prefix, CombatStat.Armor, ModificationType.Flat), Is.True);
            Assert.That(helmet.HasAffixLike(AffixType.Prefix, CombatStat.Armor, ModificationType.Flat, CombatStat.Life), Is.False, "Rüstung mit Leben passt noch dazu");
        });
    }

    [Test]
    public void HybriderAffix_UebersteheDasSpeichern()
    {
        var helmet = TestItems.Create(TestItems.Helmet);

        helmet.AddAffix(ArmourAndLife(20, 18));

        Assert.That(SaveGameSerializer.TryDeserialize(SaveGameSerializer.Serialize(new SaveGame { Unplaced = [SaveGameMapper.ToSave(helmet)] }), out var loaded), Is.True);

        var restored = SaveGameMapper.ToItem(loaded.Unplaced.Single(), TestItems.Catalog);

        Assert.That(restored.Affixes.Single().Hybrid, Is.EqualTo(new ItemAffixLine(CombatStat.Life, ModificationType.Flat, 18, false)));
    }

    #endregion

    #region Bonusschaden für Angriffe und Zauber

    [Test]
    public void GlobalesAddsXtoY_LegtBeideWerteInsStatSheet()
    {
        var gloves = TestItems.Create(TestItems.Helmet);

        gloves.AddAffix(new ItemAffix(AffixType.Prefix, CombatStat.AddedFireToAttacks, ModificationType.Flat, 11, "Warmed", false, 27));

        var modifiers = gloves.GetEquipModifiers();

        Assert.Multiple(() =>
        {
            Assert.That(modifiers.Single(modifier => modifier.AffectedStat == CombatStat.AddedFireToAttacks).Value, Is.EqualTo(11f));
            Assert.That(modifiers.Single(modifier => modifier.AffectedStat == CombatStat.AddedFireToAttacksMax).Value, Is.EqualTo(27f));
        });
    }

    [Test]
    public void BonusSchadenFuerAngriffe_ZaehltZumGrundschadenDerAttack()
    {
        var weapon   = new WeaponProfile(10, 20, 1f, 0f, DamageType.Slash, WeaponProfile.DefaultMeleeRange);
        var cleave   = new AttackDefinition("Cleave", 150f);
        var plain    = Attacker();
        var attacker = Attacker(Modifier(CombatStat.AddedPhysicalToAttacks, 4), Modifier(CombatStat.AddedPhysicalToAttacksMax, 9),
                                Modifier(CombatStat.AddedFireToAttacks, 2), Modifier(CombatStat.AddedFireToAttacksMax, 6));

        var request    = HitRequests.ForAttack(attacker, weapon, cleave);
        var physical   = plain.GetTotalMultiplier(CombatStat.PhysicalDamage);
        var elemental  = plain.GetTotalMultiplier(CombatStat.ElementalDamage);

        Assert.Multiple(() =>
        {
            Assert.That(request.MinDamage, Is.EqualTo((10f + 4f) * 1.5f * physical).Within(Tolerance));
            Assert.That(request.MaxDamage, Is.EqualTo((20f + 9f) * 1.5f * physical).Within(Tolerance));
            Assert.That(request.AddedDamage.Fire, Is.EqualTo(new DamageRange(2f * 1.5f * elemental, 6f * 1.5f * elemental)));
        });
    }

    [Test]
    public void BonusSchadenFuerZauber_WaechstMitDemZauberschaden()
    {
        var attacker = Attacker(Modifier(CombatStat.AddedFireToSpells, 10), Modifier(CombatStat.AddedFireToSpellsMax, 20),
                                Modifier(CombatStat.SpellDamage, 0.5f, ModificationType.Percentage));

        var frost      = HitRequests.ForSpell(attacker, new SpellDefinition("Frost Nova", 30f, 40f, DamageType.Frost, 0f));
        var fireball   = HitRequests.ForSpell(attacker, new SpellDefinition("Fireball", 30f, 40f, DamageType.Fire, 0f));
        var spellScale = attacker.GetTotalMultiplier(CombatStat.SpellDamage) * attacker.GetTotalMultiplier(CombatStat.ElementalDamage);

        Assert.Multiple(() =>
        {
            Assert.That(frost.AddedDamage.Fire.Max, Is.EqualTo(20f * spellScale).Within(Tolerance), "eigener Feuerteil neben dem Frost");
            Assert.That(fireball.AddedDamage.Fire.IsEmpty, Is.True, "beim Feuerball zählt er zum Hauptteil");
            Assert.That(fireball.MaxDamage, Is.EqualTo((40f + 20f) * spellScale).Within(Tolerance));
            Assert.That(HitRequests.ForAttack(attacker, WeaponProfile.Unarmed, AttackDefinition.Standard).AddedDamage.Fire.IsEmpty, Is.True, "nicht für Angriffe");
        });
    }

    #endregion

    #region Schild, Bogen, Zauber

    [Test]
    public void IncreasedChanceToBlock_ErhoehtDenBlockDesSchilds()
    {
        var shield = TestItems.Create(TestItems.Shield);

        shield.AddAffix(new ItemAffix(AffixType.Prefix, CombatStat.MeleeBlock, ModificationType.Percentage, 0.5f, "Resolute", true));

        Assert.Multiple(() =>
        {
            Assert.That(shield.Guard.MeleeBlock, Is.EqualTo(15f * 1.5f).Within(Tolerance));
            Assert.That(shield.Guard.SpellBlock, Is.EqualTo(8f), "Zauberblock bleibt");
            Assert.That(shield.GetEquipModifiers().Single(modifier => modifier.AffectedStat == CombatStat.MeleeBlock).Value, Is.EqualTo(22.5f).Within(Tolerance));
        });
    }

    [Test]
    public void WeiterePfeile_GeltenNurFuerDenBogen()
    {
        var bow = TestItems.Create(TestItems.Bow);

        bow.AddAffix(new ItemAffix(AffixType.Suffix, CombatStat.ProjectileCount, ModificationType.Flat, 1, "of Fracturing", true));

        Assert.Multiple(() =>
        {
            Assert.That(bow.ToWeaponProfile().ExtraProjectiles, Is.EqualTo(1));
            Assert.That(bow.GetEquipModifiers().Select(modifier => modifier.AffectedStat), Has.None.EqualTo(CombatStat.ProjectileCount), "nicht für Zauber");
            Assert.That(TestItems.Create(TestItems.Bow).ToWeaponProfile().ExtraProjectiles, Is.Zero);
        });
    }

    [Test]
    public void KritFuerZauber_WirktNichtAufAngriffe()
    {
        var attacker = Attacker(Modifier(CombatStat.SpellCriticalHitChance, 1f, ModificationType.Percentage));
        var plain    = Attacker();
        var spell    = new SpellDefinition("Fireball", 30f, 40f, DamageType.Fire, 6f);
        var weapon   = new WeaponProfile(10, 20, 1f, 5f, DamageType.Slash, WeaponProfile.DefaultMeleeRange);

        Assert.Multiple(() =>
        {
            Assert.That(HitRequests.ForSpell(attacker, spell).CriticalHitChance, Is.EqualTo(6f * (plain.GetIncreasedMultiplier(CombatStat.CriticalHitChance) + 1f) * plain.GetMoreMultiplier(CombatStat.CriticalHitChance)).Within(Tolerance));
            Assert.That(HitRequests.ForAttack(attacker, weapon, AttackDefinition.Standard).CriticalHitChance, Is.EqualTo(HitRequests.ForAttack(plain, weapon, AttackDefinition.Standard).CriticalHitChance));
        });
    }

    [Test]
    public void Zaubertempo_KuerztDieWirkzeit()
    {
        var spell = SkillDefinition.ForSpell("spell", new SpellDefinition("Spell", 10f, 20f, DamageType.Fire, 0f)) with { CastSec = 0.6 };

        Assert.Multiple(() =>
        {
            Assert.That(spell.GetCastSec(Attacker()), Is.EqualTo(0.6).Within(Tolerance));
            Assert.That(spell.GetCastSec(Attacker(Modifier(CombatStat.CastSpeed, 0.5f, ModificationType.Percentage))), Is.EqualTo(0.4).Within(Tolerance));
        });
    }

    [Test]
    public void Zaubertempo_ZaehltInDerSchaetzungMit()
    {
        var spell = SkillDefinition.ForSpell("spell", new SpellDefinition("Spell", 10f, 20f, DamageType.Fire, 0f)) with { CastSec = 1.0 };

        Assert.That(SkillDamageEstimator.GetUsesPerSecond(Attacker(Modifier(CombatStat.CastSpeed, 1f, ModificationType.Percentage)), spell), Is.EqualTo(2.0).Within(Tolerance));
    }

    #endregion
}
