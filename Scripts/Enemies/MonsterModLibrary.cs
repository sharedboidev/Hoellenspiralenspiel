using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.MonsterMods;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.Enemies;

public static class MonsterModLibrary
{
    public const string ModsPath = "res://Resources/MonsterMods/Pool";

    private static readonly List<MonsterModDefinition>             Definitions = new();
    private static readonly Dictionary<string, MonsterModResource> ModsById    = new();
    private static          bool                                   isLoaded;

    public static IReadOnlyList<MonsterModDefinition> Pool
    {
        get
        {
            EnsureLoaded();

            return Definitions;
        }
    }

    public static MonsterModResource Find(string modId)
    {
        if (string.IsNullOrEmpty(modId))
            return null;

        EnsureLoaded();

        return ModsById.GetValueOrDefault(modId);
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        foreach (var path in ResourceFiles.ListIn(ModsPath))
        {
            if (ResourceLoader.Load(path) is MonsterModResource mod)
                Add(mod, path);
        }

        //Die Reihenfolge im Dateisystem darf den Wurf nicht beeinflussen
        Definitions.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));

        GD.Print($"Loaded {Definitions.Count} monster mods");
    }

    private static void Add(MonsterModResource mod, string path)
    {
        if (string.IsNullOrWhiteSpace(mod.Id))
        {
            GD.PushWarning($"Der Monster-Mod {path} hat keine Id und wird übersprungen.");

            return;
        }

        if (!ModsById.TryAdd(mod.Id, mod))
        {
            GD.PushWarning($"Die Mod-Id {mod.Id} ist doppelt vergeben, {path} wird übersprungen.");

            return;
        }

        Definitions.Add(mod.Definition);
    }
}
