using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Items;

public static class ItemLibrary
{
    public const string ItemsPath = "res://Resources/Items";

    private static readonly Dictionary<string, ItemBaseResource> ItemsById = new();
    private static          bool                                 isLoaded;

    public static IItemCatalog Catalog { get; } = new LibraryCatalog();

    public static IReadOnlyCollection<ItemBaseResource> All
    {
        get
        {
            EnsureLoaded();

            return ItemsById.Values;
        }
    }

    public static ItemBaseResource Find(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        EnsureLoaded();

        return ItemsById.GetValueOrDefault(itemId);
    }

    public static ItemInstance Create(string itemId, int itemLevel = 1, int stackSize = 1)
    {
        var definition = Catalog.Find(itemId);

        return definition is null ? null : new ItemInstance(definition, itemLevel, stackSize);
    }

    public static Texture2D GetIcon(ItemInstance item)
        => Find(item?.Definition.Id)?.Icon;

    public static PackedScene GetProjectileScene(ItemInstance item)
        => (Find(item?.Definition.Id) as WeaponBaseResource)?.ProjectileScene;

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        foreach (var path in ResourceFiles.ListIn(ItemsPath))
        {
            if (ResourceLoader.Load(path) is ItemBaseResource item)
                Add(item, path);
        }
    }

    private static void Add(ItemBaseResource item, string path)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            GD.PushWarning($"Die Item-Basis {path} hat keine Id und wird übersprungen.");

            return;
        }

        if (!ItemsById.TryAdd(item.Id, item))
            GD.PushWarning($"Die Item-Id {item.Id} ist doppelt vergeben, {path} wird übersprungen.");
    }

    private sealed class LibraryCatalog : IItemCatalog
    {
        public ItemDefinition Find(string itemId)
            => ItemLibrary.Find(itemId)?.Definition;
    }
}
