using System;
using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;

namespace Hoellenspiralenspiel.Scripts.Skills;

//Alle Skills, die der Held kennen kann. Jede Skill-Resource im Ordner gehört dazu, ein neuer Skill braucht hier keinen Eintrag
public static class SkillLibrary
{
    public const string PlayerSkillsPath    = "res://Resources/Skills/Player";
    public const string StartingLoadoutPath = "res://Resources/Skills/starting_loadout.tres";

    private static readonly Dictionary<string, SkillResource> SkillsById = new();
    private static readonly List<SkillResource>               Skills     = new();
    private static          bool                              isLoaded;

    //Attacks zuerst, danach Spells, jeweils nach Namen sortiert
    public static IReadOnlyList<SkillResource> PlayerSkills
    {
        get
        {
            EnsureLoaded();

            return Skills;
        }
    }

    public static SkillResource Find(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return null;

        EnsureLoaded();

        return SkillsById.GetValueOrDefault(skillId);
    }

    public static SkillLoadoutResource LoadStartingLoadout()
        => ResourceLoader.Exists(StartingLoadoutPath) ? ResourceLoader.Load<SkillLoadoutResource>(StartingLoadoutPath) : null;

    private static void EnsureLoaded()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        foreach (var fileName in ResourceLoader.ListDirectory(PlayerSkillsPath))
        {
            if (fileName.EndsWith('/'))
                continue;

            var path = $"{PlayerSkillsPath}/{fileName}";

            if (ResourceLoader.Load(path) is SkillResource skill)
                Add(skill, path);
        }

        Skills.Sort(CompareForDisplay);
    }

    private static void Add(SkillResource skill, string path)
    {
        if (string.IsNullOrWhiteSpace(skill.Id))
        {
            GD.PushWarning($"Der Skill {path} hat keine Id und wird übersprungen.");

            return;
        }

        if (!SkillsById.TryAdd(skill.Id, skill))
        {
            GD.PushWarning($"Die Skill-Id {skill.Id} ist doppelt vergeben, {path} wird übersprungen.");

            return;
        }

        Skills.Add(skill);
    }

    private static int CompareForDisplay(SkillResource left, SkillResource right)
    {
        if (left.Kind != right.Kind)
            return left.Kind == SkillKind.Attack ? -1 : 1;

        return string.Compare(left.Definition.Name, right.Definition.Name, StringComparison.OrdinalIgnoreCase);
    }
}
