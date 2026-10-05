using System;
using System.Collections.Generic;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Combat.StatusEffects;
using Hoellenspiralenspiel.Scripts.Core.Economy;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;

namespace Hoellenspiralenspiel.Interfaces;

public interface IHero
{
    StatSheet Stats { get; }

    StatusEffectTracker StatusEffects { get; }

    CharacterItems Items { get; }

    Purse Gold { get; }

    Purse StashGold { get; }

    WeaponProfile Weapon { get; }

    SkillLoadout Loadout { get; }

    SkillCooldowns SkillCooldowns { get; }

    IReadOnlyList<SkillResource> KnownSkills { get; }

    float LifeCurrent { get; }

    float LifeMaximum { get; }

    float ManaCurrent { get; }

    float ManaMaximum { get; }

    //Was ein Leech noch heilt
    float LifePending { get; }

    float ManaPending { get; }

    int Level { get; }

    long XpTotal { get; }

    long XpFloorCurrentLevel { get; }

    long XpForNextLevel { get; }

    int AttributePoints { get; }

    event Action SheetChanged;

    event Action ResourcesChanged;

    event Action XpChanged;

    void Consume(ItemInstance item);

    bool RaiseAttribute(Attributes attribute);
}
