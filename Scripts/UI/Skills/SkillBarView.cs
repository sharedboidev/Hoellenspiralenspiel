using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Skills;
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
        hero.Loadout.SlotChanged    += OnSlotChanged;
        hero.SkillCooldowns.Started += OnCooldownStarted;
    }

    public override void _ExitTree()
    {
        if (hero is null)
            return;

        hero.Loadout.SlotChanged    -= OnSlotChanged;
        hero.SkillCooldowns.Started -= OnCooldownStarted;
    }

    private void AddSlot(int slot)
    {
        var slotView = SlotScene.Instantiate<SkillSlotView>();

        AddChild(slotView);

        slotView.Init(slot, InputActions.GetKeyLabel(InputActions.SkillSlots[slot]), hero);
        slotView.ShowSkill(SkillLibrary.Find(hero.Loadout.GetSkillId(slot)));

        slotView.PickerRequested += OpenPicker;

        slots.Add(slotView);
    }

    private void OpenPicker(SkillSlotView slotView)
        => picker.Open(slotView.Slot, hero.KnownSkills, slotView.GetGlobalRect(), hero);

    private void OnSkillChosen(int slot, SkillResource skill)
        => hero.Loadout.Assign(slot, skill?.Id);

    private void OnSlotChanged(int slot)
        => slots[slot].ShowSkill(SkillLibrary.Find(hero.Loadout.GetSkillId(slot)));

    private void OnCooldownStarted(string skillId)
    {
        foreach (var slotView in slots)
        {
            if (slotView.Shows(skillId))
                slotView.RefreshCooldown();
        }
    }
}
