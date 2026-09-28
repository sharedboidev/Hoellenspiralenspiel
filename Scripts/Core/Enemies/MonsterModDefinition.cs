using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Enemies;

//Neue Werte nur am Ende anhängen: Resources speichern die Auswahl als Zahl
public enum MonsterModFit
{
    Any,
    ProjectileUsersOnly,
    MeleeOnly
}

public readonly record struct MonsterTraits(int Level, bool UsesProjectiles);

public sealed record MonsterModDefinition
{
    public MonsterModDefinition(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ein Monster-Mod braucht eine Id.", nameof(id));

        Id   = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
    }

    public string Id   { get; }
    public string Name { get; }

    public float Weight   { get; init; } = 1f;
    public int   MinLevel { get; init; } = 1;

    //Von Mods derselben Gruppe bekommt ein Monster höchstens einen
    public string ExclusiveGroup { get; init; } = string.Empty;

    public MonsterModFit Fit { get; init; } = MonsterModFit.Any;

    public IReadOnlyList<CombatStatModifier> Modifiers { get; init; } = [];

    public string OriginId => $"monster-mod:{Id}";

    public bool Fits(MonsterTraits monster)
    {
        if (monster.Level < MinLevel)
            return false;

        return Fit switch
        {
            MonsterModFit.ProjectileUsersOnly => monster.UsesProjectiles,
            MonsterModFit.MeleeOnly           => !monster.UsesProjectiles,
            _                                 => true
        };
    }

    public IEnumerable<CombatStatModifier> GetStampedModifiers()
    {
        foreach (var modifier in Modifiers)
            yield return modifier with { OriginId = OriginId };
    }
}
