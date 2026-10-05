using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.Core.Saving;

public static class SaveGameMapper
{
    public static ItemSave ToSave(ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new ItemSave
        {
            BaseId    = item.Definition.Id,
            ItemLevel = item.ItemLevel,
            StackSize = item.StackSize,
            RareName  = item.RareName,
            Affixes = item.Affixes.Select(affix => new AffixSave
                          {
                              Type         = affix.Type,
                              Stat         = affix.Stat,
                              Modification = affix.Modification,
                              Value        = affix.Value,
                              NameAddition = affix.NameAddition,
                              IsLocal      = affix.IsLocal
                          })
                          .ToList()
        };
    }

    public static ItemInstance ToItem(ItemSave save, IItemCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var definition = save is null ? null : catalog.Find(save.BaseId);

        if (definition is null)
            return null;

        var item = new ItemInstance(definition, save.ItemLevel, Math.Max(1, save.StackSize)) { RareName = save.RareName };

        foreach (var affix in save.Affixes ?? [])
            item.AddAffix(new ItemAffix(affix.Type, affix.Stat, affix.Modification, affix.Value, affix.NameAddition, affix.IsLocal));

        return item;
    }

    public static void CaptureItems(CharacterItems items, SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(save);

        save.Inventory = ToSave(items.Inventory);
        save.Stash     = ToSave(items.Stash);

        save.Equipment = items.Equipment.Items
                              .OrderBy(entry => entry.Key)
                              .Select(entry => new EquippedItemSave { Place = entry.Key, Item = ToSave(entry.Value) })
                              .ToList();

        save.Unplaced = items.HeldItem is null ? [] : [ToSave(items.HeldItem)];
    }

    private static List<PlacedItemSave> ToSave(InventoryGrid grid)
        => grid.GetItemsInReadingOrder()
               .Select(item =>
               {
                   var cell = grid.GetPositionOf(item);

                   return new PlacedItemSave { X = cell.X, Y = cell.Y, Item = ToSave(item) };
               })
               .ToList();

    //Liefert die Ids der Item-Basen, die es nicht mehr gibt. Ihre Items fehlen nach dem Laden.
    public static IReadOnlyList<string> RestoreItems(SaveGame save, CharacterItems items, IItemCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(items);

        var missingBaseIds = new List<string>();

        ItemInstance Load(ItemSave itemSave)
        {
            var item = ToItem(itemSave, catalog);

            if (item is null)
                missingBaseIds.Add(itemSave?.BaseId ?? string.Empty);

            return item;
        }

        List<(ItemInstance Item, GridCell Cell)> LoadPlaced(List<PlacedItemSave> placedItems)
            => (placedItems ?? [])
              .Where(placed => placed is not null)
              .Select(placed => (Item: Load(placed.Item), Cell: new GridCell(placed.X, placed.Y)))
              .Where(placed => placed.Item is not null)
              .ToList();

        var inventory = LoadPlaced(save.Inventory);
        var stash     = LoadPlaced(save.Stash);
        var equipment = (save.Equipment ?? []).Select(equipped => Load(equipped.Item)).Where(item => item is not null).ToList();
        var unplaced  = (save.Unplaced ?? []).Select(Load).Where(item => item is not null).ToList();

        items.Restore(inventory, equipment, unplaced, stash);

        return missingBaseIds;
    }

    public static void CaptureVendor(Vendor vendor, SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentNullException.ThrowIfNull(save);

        save.Vendor = vendor.IsStocked ? new VendorSave { ItemLevel = vendor.ItemLevel, Stock = ToSave(vendor.Stock) } : null;
    }

    //Ohne gespeicherten Bestand bleibt der Händler leer, und wer ihn führt, würfelt neu
    public static bool RestoreVendor(SaveGame save, Vendor vendor, IItemCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(vendor);

        if (save.Vendor is null)
            return false;

        var stock = (save.Vendor.Stock ?? [])
                   .Where(placed => placed is not null)
                   .Select(placed => (Item: ToItem(placed.Item, catalog), Cell: new GridCell(placed.X, placed.Y)))
                   .Where(placed => placed.Item is not null)
                   .ToList();

        //Bis Version 3 lag der Bestand ungeordnet. Er wird einmal nach Itemtyp neu ausgelegt
        if (save.Version < 4)
            vendor.Restock(stock.Select(placed => placed.Item), save.Vendor.ItemLevel);
        else
            vendor.RestoreStock(stock, save.Vendor.ItemLevel);

        return true;
    }

    public static void CaptureLoadout(SkillLoadout loadout, SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        ArgumentNullException.ThrowIfNull(save);

        save.Loadout            = Enumerable.Range(0, loadout.SlotCount).Select(loadout.GetSkillId).ToList();
        save.LoadoutConsumables = Enumerable.Range(0, loadout.SlotCount).Select(loadout.GetConsumableId).ToList();
    }

    //Ein Trank zählt nur, wenn der Katalog seine Item-Basis als Verbrauchsgut kennt. Ohne Katalog bleiben nur die Skills
    public static void RestoreLoadout(SaveGame save, SkillLoadout loadout, IItemCatalog catalog = null)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(loadout);

        var savedSkills      = save.Loadout ?? [];
        var savedConsumables = save.LoadoutConsumables ?? [];

        for (var slot = 0; slot < loadout.SlotCount; slot++)
        {
            var consumableId = slot < savedConsumables.Count ? savedConsumables[slot] : null;

            if (IsConsumable(consumableId, catalog))
                loadout.AssignConsumable(slot, consumableId);
            else
                loadout.Assign(slot, slot < savedSkills.Count ? savedSkills[slot] : null);
        }
    }

    private static bool IsConsumable(string itemBaseId, IItemCatalog catalog)
        => !string.IsNullOrWhiteSpace(itemBaseId) && catalog?.Find(itemBaseId)?.Consumable is not null;

    public static void CaptureJourney(JourneyState journey, SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(save);

        save.Descent = null;
        save.Journey = new JourneySave
        {
            UnlockedCircles = journey.UnlockedCircles,
            Circles = journey.Descents
                             .Where(circle => circle.Value.HasBegun)
                             .OrderBy(circle => circle.Key, StringComparer.Ordinal)
                             .Select(circle => ToSave(circle.Key, circle.Value))
                             .ToList(),
            TownPortal = journey.TownPortal is { } spot
                                 ? new TownPortalSave { CircleId = spot.CircleId, Depth = spot.Depth, X = spot.X, Z = spot.Z }
                                 : null
        };
    }

    private static CircleSave ToSave(string circleId, DescentState descent)
        => new()
        {
            CircleId       = circleId,
            Seed           = descent.Seed,
            DeepestDepth   = descent.DeepestDepth,
            ContentVersion = descent.ContentVersion,
            Levels = descent.RevealedByLocation.Keys
                            .Union(descent.LocationsWithKills)
                            .OrderBy(location => location.Field)
                            .ThenBy(location => location.Dungeon)
                            .ThenBy(location => location.DungeonLevel)
                            .Select(location => new ExploredLevelSave
                            {
                                Location = location.ToString(),
                                Depth    = location.Field,
                                Revealed = descent.GetRevealed(location) ?? string.Empty,
                                Killed   = descent.GetKilled(location).ToList()
                            })
                            .ToList()
        };

    //Ein Spielstand der Version 1 kennt nur einen Abstieg ohne Kreis, er zählt für legacyCircleId
    public static bool RestoreJourney(SaveGame save, JourneyState journey, string legacyCircleId)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(journey);

        if (save.Journey is null)
            return RestoreLegacyDescent(save, journey, legacyCircleId);

        journey.Reset(save.Journey.UnlockedCircles);

        foreach (var circle in save.Journey.Circles ?? [])
        {
            if (!string.IsNullOrEmpty(circle?.CircleId))
                Restore(journey.GetDescent(circle.CircleId), circle.Seed, circle.DeepestDepth, circle.Levels, circle.ContentVersion);
        }

        if (save.Journey.TownPortal is { } portal)
            journey.OpenTownPortal(new TownPortalSpot(portal.CircleId, portal.Depth, portal.X, portal.Z));

        return true;
    }

    private static bool RestoreLegacyDescent(SaveGame save, JourneyState journey, string legacyCircleId)
    {
        if (save.Descent is null || string.IsNullOrEmpty(legacyCircleId))
            return false;

        journey.Reset();

        Restore(journey.GetDescent(legacyCircleId), save.Descent.Seed, save.Descent.Depth, save.Descent.Levels);

        return true;
    }

    private static void Restore(DescentState descent, int seed, int deepestDepth, List<ExploredLevelSave> levels, int contentVersion = 0)
    {
        var saved = (levels ?? []).Where(level => level is not null).Select(level => (Location: LocationOf(level), Level: level)).ToList();

        descent.Restore(seed,
                        deepestDepth,
                        saved.Select(entry => new KeyValuePair<LocationKey, string>(entry.Location, entry.Level.Revealed)),
                        saved.Select(entry => new KeyValuePair<LocationKey, IEnumerable<int>>(entry.Location, entry.Level.Killed)),
                        contentVersion);
    }

    //Bis Version 5 stand nur die Tiefe im Spielstand
    private static LocationKey LocationOf(ExploredLevelSave level)
        => LocationKey.TryParse(level.Location, out var location) ? location : LocationKey.Of(level.Depth);
}
