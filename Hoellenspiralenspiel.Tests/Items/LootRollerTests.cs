using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Tests.Combat;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class LootRollerTests
{
    private static LootRoller CreateRoller()
        => new(TestItems.Catalog, new AffixRoller([]));

    private static LootEntryDefinition ItemEntry(string itemId, float weight = 1f, int quantityMin = 1, int quantityMax = 1)
        => new() { Kind = LootEntryKind.Item, ItemId = itemId, Weight = weight, QuantityMin = quantityMin, QuantityMax = quantityMax };

    private static LootTableDefinition Table(int rolls, params LootEntryDefinition[] entries)
    {
        var table = new LootTableDefinition { Id = "test", Rolls = rolls };

        table.Entries.AddRange(entries);

        return table;
    }

    [Test]
    public void ItemEintrag_LaesstDasItemFallen()
    {
        var drops = CreateRoller().Roll(Table(1, ItemEntry("sword")), new SeededRandom(1));

        Assert.That(drops.Select(item => item.Definition.Id), Is.EqualTo(new[] { "sword" }));
    }

    [Test]
    public void JederWurf_LaesstEtwasFallen()
    {
        var drops = CreateRoller().Roll(Table(4, ItemEntry("sword"), ItemEntry("helmet")), new SeededRandom(1));

        Assert.That(drops, Has.Count.EqualTo(4));
    }

    [Test]
    public void Nichts_LaesstNichtsFallen()
    {
        var table = Table(3, new LootEntryDefinition { Kind = LootEntryKind.Nothing });

        Assert.That(CreateRoller().Roll(table, new SeededRandom(1)), Is.Empty);
    }

    [Test]
    public void LeereTabelle_LaesstNichtsFallen()
        => Assert.That(CreateRoller().Roll(Table(3), new SeededRandom(1)), Is.Empty);

    [Test]
    public void UnbekannteItemBasis_WirdUebersprungen()
        => Assert.That(CreateRoller().Roll(Table(2, ItemEntry("unknown")), new SeededRandom(1)), Is.Empty);

    [Test]
    public void Gewicht_BestimmtDieHaeufigkeit()
    {
        var table = Table(1000, ItemEntry("sword", 3), ItemEntry("helmet"));
        var drops = CreateRoller().Roll(table, new SeededRandom(7));

        Assert.That(drops.Count(item => item.Definition.Id == "sword"), Is.InRange(700, 800));
    }

    [Test]
    public void EintragOhneGewicht_FaelltNie()
    {
        var table = Table(200, ItemEntry("sword", 0), ItemEntry("helmet"));
        var drops = CreateRoller().Roll(table, new SeededRandom(7));

        Assert.That(drops.Select(item => item.Definition.Id), Is.All.EqualTo("helmet"));
    }

    [Test]
    public void VerschachtelteTabelle_WuerfeltIhreEigenenEintraege()
    {
        var inner = Table(2, ItemEntry("potion"));
        var outer = Table(3, new LootEntryDefinition { Kind = LootEntryKind.NestedTable, NestedTable = inner });

        var drops = CreateRoller().Roll(outer, new SeededRandom(1));

        Assert.Multiple(() =>
        {
            Assert.That(drops, Has.Count.EqualTo(6));
            Assert.That(drops.Select(item => item.Definition.Id), Is.All.EqualTo("potion"));
        });
    }

    [Test]
    public void TabelleDieSichSelbstEnthaelt_KommtZumEnde()
    {
        var table = Table(1);

        table.Entries.Add(new LootEntryDefinition { Kind = LootEntryKind.NestedTable, NestedTable = table });

        Assert.That(CreateRoller().Roll(table, new SeededRandom(1)), Is.Empty);
    }

    [Test]
    public void Menge_LiegtInDerSpanneDesEintrags()
    {
        var table = Table(300, ItemEntry("potion", quantityMin: 2, quantityMax: 4));
        var sizes = CreateRoller().Roll(table, new SeededRandom(3)).Select(item => item.StackSize).Distinct().ToArray();

        Assert.That(sizes, Is.EquivalentTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void Menge_UebersteigtNieDenStapel()
    {
        var table = Table(100, ItemEntry("potion", quantityMin: 4, quantityMax: 12));
        var drops = CreateRoller().Roll(table, new SeededRandom(3));

        Assert.That(drops.Select(item => item.StackSize), Is.All.InRange(4, 5));
    }

    [Test]
    public void NichtStapelbareItems_FallenEinzeln()
    {
        var table = Table(20, ItemEntry("sword", quantityMin: 3, quantityMax: 5));
        var drops = CreateRoller().Roll(table, new SeededRandom(3));

        Assert.That(drops.Select(item => item.StackSize), Is.All.EqualTo(1));
    }

    [Test]
    public void Itemlevel_GehtAnDasItem()
    {
        var drops = CreateRoller().Roll(Table(1, ItemEntry("sword")), new SeededRandom(1), 37);

        Assert.That(drops.Single().ItemLevel, Is.EqualTo(37));
    }

    [Test]
    public void Beute_BekommtAffixe()
    {
        var affix = new AffixDefinition(AffixType.Prefix, Enums.CombatStat.Life, Enums.ModificationType.Flat)
        {
            AllowedSlots = [Enums.ItemSlot.Helmet],
            Tiers        = [new AffixTierDefinition(1, 1, 100, 3, 9, "Hearty")]
        };

        var roller = new LootRoller(TestItems.Catalog, new AffixRoller([affix]));

        //Würfe: Eintrag, Anzahl der Affixe 3, Beginn mit Prefix, Stufe, Wert
        var drops = roller.Roll(Table(1, ItemEntry("helmet")), new FixedRandom(0.5f, 0f, 0.99f, 0f));

        Assert.That(drops.Single().AffixedName, Is.EqualTo("Hearty Helmet"));
    }

    [Test]
    public void GleicherSeed_ErgibtGleicheBeute()
    {
        var affix = new AffixDefinition(AffixType.Prefix, Enums.CombatStat.Life, Enums.ModificationType.Flat)
        {
            AllowedSlots = [Enums.ItemSlot.Helmet, Enums.ItemSlot.PhysicalWeapon],
            Tiers        = [new AffixTierDefinition(1, 1, 100, 3, 90, "Hearty")]
        };

        var roller = new LootRoller(TestItems.Catalog, new AffixRoller([affix]));
        var table  = Table(30, ItemEntry("sword"), ItemEntry("helmet"), ItemEntry("potion", quantityMax: 5), new LootEntryDefinition { Kind = LootEntryKind.Nothing });

        string Describe(ItemInstance item)
            => $"{item.Definition.Id} x{item.StackSize} {string.Join(",", item.Affixes.Select(itemAffix => itemAffix.Value))}";

        var first  = roller.Roll(table, new SeededRandom(99)).Select(Describe).ToArray();
        var second = roller.Roll(table, new SeededRandom(99)).Select(Describe).ToArray();
        var other  = roller.Roll(table, new SeededRandom(100)).Select(Describe).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(other, Is.Not.EqualTo(first));
        });
    }
}
