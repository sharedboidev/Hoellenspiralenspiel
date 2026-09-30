using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.World.Levels;

namespace Hoellenspiralenspiel.Scripts.Controllers;

//Führt den Händler: würfelt seinen Bestand, kennt die Preise und wickelt den Handel mit dem Helden ab
public partial class VendorController : Node
{
    private VendorStockRoller roller;

    [Export]
    public Hero Hero { get; set; }

    [Export]
    public Descent Descent { get; set; }

    [Export]
    public Lootsystem Lootsystem { get; set; }

    [ExportGroup("Bestand")]
    [Export]
    public int StockSize { get; set; } = 20;

    //Jedes wievielte Stück ein bis zwei Affixe trägt. 0 schaltet das ab
    [Export]
    public int MagicOneIn { get; set; } = 20;

    //Jedes wievielte Stück Rare ist. 0 schaltet das ab
    [Export]
    public int RareOneIn { get; set; } = 30;

    [ExportGroup("Preise")]
    [Export]
    public float MagicPriceFactor { get; set; } = 3f;

    [Export]
    public float RarePriceFactor { get; set; } = 8f;

    //So viel vom Kaufpreis zahlt der Händler beim Ankauf
    [Export(PropertyHint.Range, "0,100,1")]
    public float SellSharePercent { get; set; } = 25f;

    public Vendor Vendor { get; } = new();

    public Trade Trade { get; private set; }

    public override void _Ready()
    {
        Trade = new Trade(Hero.Items, Hero.Gold, Vendor, new PriceRule(MagicPriceFactor, RarePriceFactor, SellSharePercent / 100f));

        Vendor.SetWares(ItemLibrary.All
                                   .Select(item => item.Definition)
                                   .Where(definition => definition.Consumable is not null && definition.Price > 0)
                                   .OrderBy(definition => definition.Id, StringComparer.Ordinal)
                                   .Select(definition => ItemLibrary.Create(definition.Id)));

        //Bei einem Stufenaufstieg hat die neue Ware das Level der zuletzt erreichten Ebene
        Hero.LeveledUp += () => Restock(Vendor.ItemLevel);

        if (Descent is null)
            return;

        Descent.LevelReached += Restock;

        //Der Rückkauf gilt, bis der Held den Hub verlässt
        Descent.LevelEntered += Vendor.ClearBuyback;
        Descent.PlaceEntered += Vendor.ClearBuyback;
    }

    public void EnsureStocked()
    {
        if (!Vendor.IsStocked)
            Restock(GetReachedAreaLevel());
    }

    //Der Bestand steht im Spielstand. Er hängt deshalb nicht am Seed der Ebenen und würfelt mit einer eigenen Quelle
    public void Restock(int itemLevel)
    {
        if (Lootsystem?.AffixRoller is null)
            return;

        roller ??= new VendorStockRoller(Lootsystem.AffixRoller);

        var bases = ItemLibrary.All
                               .Select(item => item.Definition)
                               .Where(definition => definition.IsEquippable && definition.Price > 0)
                               .OrderBy(definition => definition.Id, StringComparer.Ordinal)
                               .ToList();

        var stock = roller.Roll(bases,
                                StockSize,
                                itemLevel,
                                VendorStockRoller.ChanceOfOneIn(MagicOneIn),
                                VendorStockRoller.ChanceOfOneIn(RareOneIn),
                                new SeededRandom((int)GD.Randi()));

        Vendor.Restock(stock, itemLevel);
    }

    //Spielstände aus der Zeit vor dem Händler kennen nur die Reise, daraus folgt das Level der ersten Ware
    private int GetReachedAreaLevel()
    {
        if (Descent is null)
            return 1;

        var reached = Descent.Circles
                             .Where(circle => circle is not null)
                             .Select(circle => (Circle: circle, Deepest: Descent.Journey.Descents.GetValueOrDefault(circle.Id)?.DeepestDepth ?? 0))
                             .Where(entry => entry.Deepest > 0)
                             .Select(entry => entry.Circle.FirstAreaLevel + entry.Deepest - 1)
                             .DefaultIfEmpty(Descent.FirstCircle?.FirstAreaLevel ?? 1)
                             .Max();

        return Math.Max(1, reached);
    }
}
