using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Scripts.Core.Items;

public sealed class ItemInstance
{
    private static long lastInstanceNumber;

    private readonly List<ItemAffix> affixes = new();
    private          int             stackSize;

    public ItemInstance(ItemDefinition definition, int itemLevel = 1, int stackSize = 1)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
        ItemLevel  = Math.Max(1, itemLevel);
        StackSize  = stackSize;
    }

    //Herkunft der Modifier im Stat-Blatt, gilt nur für die laufende Sitzung
    public string InstanceId { get; } = $"item-{Interlocked.Increment(ref lastInstanceNumber)}";

    public ItemDefinition Definition { get; }

    public int ItemLevel { get; }

    public string RareName { get; set; }

    public IReadOnlyList<ItemAffix> Affixes => affixes;

    public int StackSize
    {
        get => stackSize;
        set => stackSize = Math.Clamp(value, 0, Definition.MaxStackSize);
    }

    public int  FreeStackSpace => Definition.MaxStackSize - StackSize;
    public bool IsStackFull    => FreeStackSpace <= 0;

    public ItemRarity Rarity
    {
        get
        {
            var prefixCount = affixes.Count(affix => affix.Type == AffixType.Prefix);
            var suffixCount = affixes.Count(affix => affix.Type == AffixType.Suffix);

            if (prefixCount + suffixCount == 0)
                return ItemRarity.Normal;

            return prefixCount > 1 || suffixCount > 1 ? ItemRarity.Rare : ItemRarity.Magic;
        }
    }

    public string AffixedName
    {
        get
        {
            var prefix = affixes.FirstOrDefault(affix => affix.Type == AffixType.Prefix)?.NameAddition;
            var suffix = affixes.FirstOrDefault(affix => affix.Type == AffixType.Suffix)?.NameAddition;

            return $"{prefix?.Trim()} {Definition.Name} {suffix?.Trim()}".Trim();
        }
    }

    public int    ArmorValue        => (int)GetLocalValue(Definition.Armor, CombatStat.Armor);
    public int    MinDamage         => (int)GetLocalValue(Definition.Weapon?.MinDamage ?? 0f, CombatStat.PhysicalDamage);
    public int    MaxDamage         => (int)GetLocalValue(Definition.Weapon?.MaxDamage ?? 0f, CombatStat.PhysicalDamage);
    public double AttacksPerSecond  => Math.Round(GetLocalValue(Definition.Weapon?.AttacksPerSecond ?? 0f, CombatStat.Attackspeed), 2);
    public double CriticalHitChance => Math.Round(GetLocalValue(Definition.Weapon?.CriticalHitChance ?? 0f, CombatStat.CriticalHitChance), 2);

    public void AddAffix(ItemAffix affix)
    {
        ArgumentNullException.ThrowIfNull(affix);

        affixes.Add(affix);
    }

    public bool HasAffixLike(AffixType type, CombatStat stat, ModificationType modification)
        => affixes.Any(affix => affix.Type == type && affix.Stat == stat && affix.Modification == modification);

    public bool CanStackWith(ItemInstance other)
        => other is not null && other != this && Definition.IsStackable && other.Definition.Id == Definition.Id;

    public WeaponProfile ToWeaponProfile()
    {
        var weapon = Definition.Weapon;

        if (weapon is null)
            return null;

        return new WeaponProfile(MinDamage,
                                 MaxDamage,
                                 (float)AttacksPerSecond,
                                 (float)CriticalHitChance,
                                 weapon.DamageType,
                                 weapon.Range,
                                 weapon.IsRanged,
                                 weapon.ProjectileSpeed);
    }

    public IReadOnlyList<CombatStatModifier> GetEquipModifiers()
    {
        var modifiers = new List<CombatStatModifier>();

        if (Definition.Kind == ItemKind.Armor)
            AddFlat(modifiers, CombatStat.Armor, ArmorValue);

        AddFlat(modifiers, CombatStat.MeleeBlock, Definition.Guard.MeleeBlock);
        AddFlat(modifiers, CombatStat.SpellBlock, Definition.Guard.SpellBlock);
        AddFlat(modifiers, CombatStat.MeleeParry, Definition.Guard.MeleeParry);
        AddFlat(modifiers, CombatStat.SpellParry, Definition.Guard.SpellParry);

        foreach (var affix in affixes.Where(affix => !affix.IsLocal))
            modifiers.Add(new CombatStatModifier(affix.Stat, affix.Modification, affix.Value, InstanceId));

        return modifiers;
    }

    private void AddFlat(List<CombatStatModifier> modifiers, CombatStat stat, float value)
    {
        if (value != 0f)
            modifiers.Add(new CombatStatModifier(stat, ModificationType.Flat, value, InstanceId));
    }

    private float GetLocalValue(float baseValue, CombatStat stat)
    {
        var addedFlat = 0f;
        var increased = 0f;
        var more      = 1f;

        foreach (var affix in affixes)
        {
            if (!affix.IsLocal || affix.Stat != stat)
                continue;

            switch (affix.Modification)
            {
                case ModificationType.Flat:
                    addedFlat += affix.Value;

                    break;
                case ModificationType.Percentage:
                    increased += affix.Value;

                    break;
                case ModificationType.More:
                    more *= 1 + affix.Value;

                    break;
            }
        }

        return StatFormulas.Combine(baseValue, addedFlat, increased, more);
    }
}
