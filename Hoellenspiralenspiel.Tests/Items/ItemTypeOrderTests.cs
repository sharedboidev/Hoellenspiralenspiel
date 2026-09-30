using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Tests.Economy;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Items;

[TestFixture]
public class ItemTypeOrderTests
{
    internal static readonly ItemDefinition Tunic = ItemDefinition.ForArmor("tunic", "Tunic", ItemSlot.Torso, 5) with
    {
        Width  = 2,
        Height = 3,
        Price  = 30
    };

    internal static readonly ItemDefinition Gloves = ItemDefinition.ForArmor("gloves", "Gloves", ItemSlot.Hands, 3) with
    {
        Width  = 2,
        Height = 2,
        Price  = 14
    };

    internal static readonly ItemDefinition ManaPotion = ItemDefinition.ForConsumable("mana_potion", "Mana Potion", new ConsumableEffect(ConsumableEffectKind.RestoreMana, 20), 5) with { Price = 12 };

    private static ItemDefinition Weapon(WeaponType type, string id = null, string name = null)
        => ItemDefinition.ForWeapon(id ?? $"{type}", name ?? $"{type}", ItemSlot.PhysicalWeapon, new WeaponStats(1, 2, 1f, 5, type, WieldStrategy.MainHand));

    private static ItemDefinition Armor(ItemSlot slot)
        => ItemDefinition.ForArmor($"{slot}", $"{slot}", slot, 1);

    private static ItemDefinition Consumable(ConsumableEffectKind kind)
        => ItemDefinition.ForConsumable($"{kind}", $"{kind}", new ConsumableEffect(kind, 10), 5);

    private static int[] RanksOf(params ItemDefinition[] definitions)
        => definitions.Select(ItemTypeOrder.GetRank).ToArray();

    [Test]
    public void DieBasenDesSpiels_StehenInDieserReihenfolge()
        => Assert.That(RanksOf(TestItems.Sword, TestItems.Bow, TestItems.Staff, TestItems.Shield, TestItems.Helmet, Tunic, Gloves, TestItems.Potion, ManaPotion),
                       Is.Ordered.Ascending.And.Unique);

    [Test]
    public void DieWaffen_FolgenIhremWaffentyp()
        => Assert.That(RanksOf(Weapon(WeaponType.Sword),
                               Weapon(WeaponType.Axe),
                               Weapon(WeaponType.Flail),
                               Weapon(WeaponType.Dagger),
                               Weapon(WeaponType.Bow),
                               Weapon(WeaponType.Staff),
                               Weapon(WeaponType.Wand),
                               Weapon(WeaponType.Undefined)),
                       Is.Ordered.Ascending.And.Unique);

    [Test]
    public void EinUnbekannterWaffentyp_StehtBeiDenUebrigenWaffen()
        => Assert.That(ItemTypeOrder.GetRank(Weapon((WeaponType)99)), Is.EqualTo(ItemTypeOrder.GetRank(Weapon(WeaponType.Undefined))));

    [Test]
    public void EinStab_StehtBeiDenWaffenVorDemSchild()
        => Assert.That(RanksOf(TestItems.Bow, TestItems.Staff, Weapon(WeaponType.Undefined), TestItems.Shield), Is.Ordered.Ascending.And.Unique);

    [Test]
    public void DieRuestung_FolgtVomKopfBisZuDenFuessen_DanachDerSchmuck()
    {
        ItemSlot[] slots =
        [
            ItemSlot.Helmet, ItemSlot.Shoulders, ItemSlot.Back, ItemSlot.Torso, ItemSlot.Wrists, ItemSlot.Hands, ItemSlot.Belt, ItemSlot.Legs, ItemSlot.Feet,
            ItemSlot.Neck, ItemSlot.Ring1, ItemSlot.Ring2, ItemSlot.Ring3, ItemSlot.Ring4
        ];

        Assert.That(RanksOf([TestItems.Shield, ..slots.Select(Armor)]), Is.Ordered.Ascending.And.Unique);
    }

    [Test]
    public void DieVerbrauchsgueter_KommenNachDemSchmuck()
        => Assert.That(RanksOf(Armor(ItemSlot.Ring4),
                               Consumable(ConsumableEffectKind.RestoreLife),
                               Consumable(ConsumableEffectKind.RestoreMana),
                               Consumable((ConsumableEffectKind)99)),
                       Is.Ordered.Ascending.And.Unique);

    [Test]
    public void Unbekanntes_StehtZuletzt()
    {
        var lastKnown = ItemTypeOrder.GetRank(Consumable((ConsumableEffectKind)99));

        Assert.Multiple(() =>
        {
            Assert.That(ItemTypeOrder.GetRank(Armor(ItemSlot.Undefined)), Is.GreaterThan(lastKnown));
            Assert.That(ItemTypeOrder.GetRank(Armor(ItemSlot.PhysicalWeapon)), Is.GreaterThan(lastKnown));
            Assert.That(ItemTypeOrder.GetRank(null), Is.GreaterThan(lastKnown));
        });
    }

    [Test]
    public void Sort_LegtGleicheBasenZusammen_RareVorMagicVorNormal()
    {
        var normalSword = TestItems.Create(TestItems.Sword);
        var helmet      = TestItems.Create(TestItems.Helmet);
        var magicSword  = PriceRuleTests.CreateMagic(TestItems.Sword);
        var normalStaff = TestItems.Create(TestItems.Staff);
        var rareSword   = PriceRuleTests.CreateRare(TestItems.Sword);
        var rareStaff   = PriceRuleTests.CreateRare(TestItems.Staff);

        var sorted = ItemTypeOrder.Sort([normalSword, helmet, magicSword, normalStaff, rareSword, rareStaff]);

        Assert.That(sorted, Is.EqualTo(new[] { rareSword, magicSword, normalSword, rareStaff, normalStaff, helmet }));
    }

    [Test]
    public void Sort_OrdnetBasenEinesTypsNachNameUndDannNachId()
    {
        var greatsword = TestItems.Create(Weapon(WeaponType.Sword, "a_greatsword", "Greatsword"));
        var arming     = TestItems.Create(Weapon(WeaponType.Sword, "z_arming", "Arming Sword"));
        var armingTwin = TestItems.Create(Weapon(WeaponType.Sword, "b_arming", "Arming Sword"));

        Assert.That(ItemTypeOrder.Sort([greatsword, arming, armingTwin]), Is.EqualTo(new[] { armingTwin, arming, greatsword }));
    }

    [Test]
    public void Sort_BeiGleichstand_BleibtDieReihenfolge()
    {
        var first  = TestItems.Create(TestItems.Sword);
        var second = TestItems.Create(TestItems.Sword);
        var third  = TestItems.Create(TestItems.Sword);

        Assert.That(ItemTypeOrder.Sort([second, third, first]), Is.EqualTo(new[] { second, third, first }));
    }

    [Test]
    public void Sort_UebergehtLeereEintraege()
    {
        var sword = TestItems.Create(TestItems.Sword);

        Assert.Multiple(() =>
        {
            Assert.That(ItemTypeOrder.Sort([null, sword, null]), Is.EqualTo(new[] { sword }));
            Assert.That(ItemTypeOrder.Sort(null), Is.Empty);
        });
    }
}
