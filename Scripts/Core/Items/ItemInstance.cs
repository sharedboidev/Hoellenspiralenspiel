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
    public int    MaxDamage         => (int)GetLocalValue(Definition.Weapon?.MaxDamage ?? 0f, CombatStat.PhysicalDamage, true);
    public double AttacksPerSecond  => Math.Round(GetLocalValue(Definition.Weapon?.AttacksPerSecond ?? 0f, CombatStat.Attackspeed), 2);
    public double CriticalHitChance => Math.Round(GetLocalValue(Definition.Weapon?.CriticalHitChance ?? 0f, CombatStat.CriticalHitChance), 2);

    //Zusatzschaden der Elemente aus lokalen Affixen. Eine Waffe bringt selbst keinen mit
    public PerElement<DamageRange> AddedDamage
        => Definition.Weapon is null
                ? default
                : PerElement<DamageRange>.From(element => new DamageRange((int)GetLocalValue(0f, element.GetElementStat()),
                                                                          (int)GetLocalValue(0f, element.GetElementStat(), true)));

    //"Bow Attacks fire an additional Arrow" gilt nur für die Angriffe mit dieser Waffe
    public int ExtraProjectiles => Definition.Weapon is null ? 0 : (int)GetLocalValue(0f, CombatStat.ProjectileCount);

    //Block und Parry der Basis, ein Schild erhöht seinen Block über "% increased Chance to Block"
    public GuardStats Guard
        => new(GetLocalValue(Definition.Guard.MeleeBlock, CombatStat.MeleeBlock),
               GetLocalValue(Definition.Guard.SpellBlock, CombatStat.SpellBlock),
               GetLocalValue(Definition.Guard.MeleeParry, CombatStat.MeleeParry),
               GetLocalValue(Definition.Guard.SpellParry, CombatStat.SpellParry));

    //Lokale Affixe senken die Anforderungen an Attribute, die an das Level nie. Gerundet wird zur nächsten ganzen Zahl
    public IReadOnlyDictionary<Requirement, int> Requirements
    {
        get
        {
            if (!affixes.Any(affix => affix.IsLocal && affix.Stat == CombatStat.AttributeRequirements))
                return Definition.Requirements;

            return Definition.Requirements.ToDictionary(pair => pair.Key,
                                                        pair => pair.Key == Requirement.CharacterLevel
                                                                ? pair.Value
                                                                : (int)MathF.Round(GetLocalValue(pair.Value, CombatStat.AttributeRequirements), MidpointRounding.AwayFromZero));
        }
    }

    public void AddAffix(ItemAffix affix)
    {
        ArgumentNullException.ThrowIfNull(affix);

        affixes.Add(affix);
    }

    //Ein hybrider Affix gehört zu einer eigenen Familie: "+# to Armour" und "+# to Armour, +# to maximum Life" passen auf dasselbe Item
    public bool HasAffixLike(AffixType type, CombatStat stat, ModificationType modification, CombatStat? hybridStat = null)
        => affixes.Any(affix => affix.Type == type && affix.Stat == stat && affix.Modification == modification && affix.Hybrid?.Stat == hybridStat);

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
                                 weapon.ProjectileSpeed)
        {
            AddedDamage      = AddedDamage,
            ExtraProjectiles = ExtraProjectiles
        };
    }

    public IReadOnlyList<CombatStatModifier> GetEquipModifiers()
    {
        var modifiers = new List<CombatStatModifier>();
        var guard     = Guard;

        if (Definition.Kind == ItemKind.Armor)
            AddFlat(modifiers, CombatStat.Armor, ArmorValue);

        AddFlat(modifiers, CombatStat.MeleeBlock, guard.MeleeBlock);
        AddFlat(modifiers, CombatStat.SpellBlock, guard.SpellBlock);
        AddFlat(modifiers, CombatStat.MeleeParry, guard.MeleeParry);
        AddFlat(modifiers, CombatStat.SpellParry, guard.SpellParry);

        foreach (var affix in affixes)
        {
            if (!affix.IsLocal)
                AddGlobal(modifiers, affix);

            if (affix.Hybrid is { IsLocal: false } hybrid)
                modifiers.Add(new CombatStatModifier(hybrid.Stat, hybrid.Modification, hybrid.Value, InstanceId));
        }

        return modifiers;
    }

    //Ein globales "Adds X to Y" legt X in den Stat des Affixes und Y in dessen Gegenstück mit Max
    private void AddGlobal(List<CombatStatModifier> modifiers, ItemAffix affix)
    {
        modifiers.Add(new CombatStatModifier(affix.Stat, affix.Modification, affix.Value, InstanceId));

        if (affix.HasRange && CombatStatGroups.TryGetRangeMaximum(affix.Stat, out var maximum))
            modifiers.Add(new CombatStatModifier(maximum, affix.Modification, affix.ValueTo, InstanceId));
    }

    private void AddFlat(List<CombatStatModifier> modifiers, CombatStat stat, float value)
    {
        if (value != 0f)
            modifiers.Add(new CombatStatModifier(stat, ModificationType.Flat, value, InstanceId));
    }

    //Ein Affix "Adds X to Y" gibt dem unteren Wert X und dem oberen Y, jeder andere flache Affix beiden dasselbe.
    //Der zweite Stat eines hybriden Affixes zählt mit, wenn er lokal ist
    private float GetLocalValue(float baseValue, CombatStat stat, bool isUpperValue = false)
    {
        var addedFlat = 0f;
        var increased = 0f;
        var more      = 1f;

        foreach (var affix in affixes)
        {
            if (affix.IsLocal && affix.Stat == stat)
                Accumulate(affix.Modification, isUpperValue && affix.HasRange ? affix.ValueTo : affix.Value, ref addedFlat, ref increased, ref more);

            if (affix.Hybrid is { IsLocal: true } hybrid && hybrid.Stat == stat)
                Accumulate(hybrid.Modification, hybrid.Value, ref addedFlat, ref increased, ref more);
        }

        return StatFormulas.Combine(baseValue, addedFlat, increased, more);
    }

    private static void Accumulate(ModificationType modification, float value, ref float addedFlat, ref float increased, ref float more)
    {
        switch (modification)
        {
            case ModificationType.Flat:
                addedFlat += value;

                break;
            case ModificationType.Percentage:
                increased += value;

                break;
            case ModificationType.More:
                more *= 1 + value;

                break;
        }
    }
}
