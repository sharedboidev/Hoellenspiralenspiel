using System;
using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Items;
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

        save.Inventory = items.Inventory.GetItemsInReadingOrder()
                              .Select(item =>
                              {
                                  var cell = items.Inventory.GetPositionOf(item);

                                  return new PlacedItemSave { X = cell.X, Y = cell.Y, Item = ToSave(item) };
                              })
                              .ToList();

        save.Equipment = items.Equipment.Items
                              .OrderBy(entry => entry.Key)
                              .Select(entry => new EquippedItemSave { Place = entry.Key, Item = ToSave(entry.Value) })
                              .ToList();

        save.Unplaced = items.HeldItem is null ? [] : [ToSave(items.HeldItem)];
    }

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

        var inventory = (save.Inventory ?? [])
                       .Select(placed => (Item: Load(placed.Item), Cell: new GridCell(placed.X, placed.Y)))
                       .Where(placed => placed.Item is not null)
                       .ToList();

        var equipment = (save.Equipment ?? []).Select(equipped => Load(equipped.Item)).Where(item => item is not null).ToList();
        var unplaced  = (save.Unplaced ?? []).Select(Load).Where(item => item is not null).ToList();

        items.Restore(inventory, equipment, unplaced);

        return missingBaseIds;
    }

    public static void CaptureLoadout(SkillLoadout loadout, SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(loadout);
        ArgumentNullException.ThrowIfNull(save);

        save.Loadout = Enumerable.Range(0, loadout.SlotCount).Select(loadout.GetSkillId).ToList();
    }

    public static void RestoreLoadout(SaveGame save, SkillLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(loadout);

        var savedSlots = save.Loadout ?? [];

        for (var slot = 0; slot < loadout.SlotCount; slot++)
            loadout.Assign(slot, slot < savedSlots.Count ? savedSlots[slot] : null);
    }
}
