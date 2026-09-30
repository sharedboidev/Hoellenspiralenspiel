using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Saving;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillBarView : HBoxContainer
{
    private const int SlotGapPx = 8;

    private static readonly PackedScene SlotScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/skill_slot.tscn");

    private readonly List<SkillSlotView> slots = new();
    private          IHero               hero;
    private          SkillPicker         picker;
    [Export] private Node                player;

    public override void _Ready()
    {
        if (player is IHero heroOfScene)
            Bind(heroOfScene);
    }

    public void Bind(IHero boundHero)
    {
        hero = boundHero;

        AddThemeConstantOverride("separation", SlotGapPx);

        for (var slot = 0; slot < hero.Loadout.SlotCount; slot++)
            AddSlot(slot);

        picker = new SkillPicker();

        AddChild(picker);

        picker.SkillChosen          += OnSkillChosen;
        picker.ConsumableChosen     += OnConsumableChosen;
        hero.Loadout.SlotChanged    += ShowSlot;
        hero.SkillCooldowns.Started += OnCooldownStarted;
        hero.Items.Changed          += ShowConsumableCounts;

        //Nach einer Neubelegung in den Einstellungen stehen die neuen Tasten auf den Plätzen
        if (UserSettings.Instance is { } settings)
            settings.Changed += ShowKeys;
    }

    public override void _ExitTree()
    {
        if (UserSettings.Instance is { } settings)
            settings.Changed -= ShowKeys;

        if (hero is null)
            return;

        hero.Loadout.SlotChanged    -= ShowSlot;
        hero.SkillCooldowns.Started -= OnCooldownStarted;
        hero.Items.Changed          -= ShowConsumableCounts;
    }

    private void AddSlot(int slot)
    {
        var slotView = SlotScene.Instantiate<SkillSlotView>();

        AddChild(slotView);

        slotView.Init(slot, InputActions.GetKeyLabel(InputActions.SkillSlots[slot]), hero);

        slotView.PickerRequested += OpenPicker;

        slots.Add(slotView);

        ShowSlot(slot);
    }

    private void ShowSlot(int slot)
    {
        var consumableId = hero.Loadout.GetConsumableId(slot);

        if (consumableId is null)
            slots[slot].ShowSkill(SkillLibrary.Find(hero.Loadout.GetSkillId(slot)));
        else
            slots[slot].ShowConsumable(ItemLibrary.Find(consumableId) as ConsumableBaseResource, hero.Items.CountInInventory(consumableId));
    }

    private void ShowConsumableCounts()
    {
        for (var slot = 0; slot < slots.Count; slot++)
        {
            if (hero.Loadout.GetConsumableId(slot) is not null)
                ShowSlot(slot);
        }
    }

    private void ShowKeys()
    {
        foreach (var slotView in slots)
            slotView.ShowKey(InputActions.GetKeyLabel(InputActions.SkillSlots[slotView.Slot]));
    }

    private void OpenPicker(SkillSlotView slotView)
        => picker.Open(slotView.Slot, hero.KnownSkills, GetConsumables(), slotView.GetGlobalRect(), hero);

    private static List<ConsumableBaseResource> GetConsumables()
        => ItemLibrary.All
                      .OfType<ConsumableBaseResource>()
                      .OrderBy(consumable => consumable.Definition.Name)
                      .ThenBy(consumable => consumable.Id)
                      .ToList();

    private void OnSkillChosen(int slot, SkillResource skill)
        => hero.Loadout.Assign(slot, skill?.Id);

    private void OnConsumableChosen(int slot, ConsumableBaseResource consumable)
        => hero.Loadout.AssignConsumable(slot, consumable?.Id);

    private void OnCooldownStarted(string skillId)
    {
        foreach (var slotView in slots)
        {
            if (slotView.Shows(skillId))
                slotView.RefreshCooldown();
        }
    }
}
