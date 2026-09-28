using System.Collections.Generic;
using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Skills;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Utils;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillBarView : HBoxContainer
{
    private const int SlotGapPx = 8;

    private static readonly PackedScene SlotScene = ResourceLoader.Load<PackedScene>("res://Scenes/UI/skill_slot.tscn");

    private readonly List<SkillSlotView> slots = new();
    private          SkillPicker         picker;
    private          Player2D            player;

    public void Bind(Player2D boundPlayer)
    {
        player = boundPlayer;

        AddThemeConstantOverride("separation", SlotGapPx);

        for (var slot = 0; slot < player.Loadout.SlotCount; slot++)
            AddSlot(slot);

        picker = new SkillPicker();

        AddChild(picker);

        picker.SkillChosen            += OnSkillChosen;
        player.Loadout.SlotChanged    += OnSlotChanged;
        player.SkillCooldowns.Started += OnCooldownStarted;
    }

    public override void _ExitTree()
    {
        if (player is null)
            return;

        player.Loadout.SlotChanged    -= OnSlotChanged;
        player.SkillCooldowns.Started -= OnCooldownStarted;
    }

    private void AddSlot(int slot)
    {
        var slotView = SlotScene.Instantiate<SkillSlotView>();

        AddChild(slotView);

        slotView.Init(slot, InputActions.GetKeyLabel(InputActions.SkillSlots[slot]), player);
        slotView.ShowSkill(SkillLibrary.Find(player.Loadout.GetSkillId(slot)));

        slotView.PickerRequested += OpenPicker;

        slots.Add(slotView);
    }

    private void OpenPicker(SkillSlotView slotView)
        => picker.Open(slotView.Slot, player.KnownSkills, slotView.GetGlobalRect(), player);

    private void OnSkillChosen(int slot, SkillResource skill)
        => player.Loadout.Assign(slot, skill?.Id);

    private void OnSlotChanged(int slot)
        => slots[slot].ShowSkill(SkillLibrary.Find(player.Loadout.GetSkillId(slot)));

    private void OnCooldownStarted(string skillId)
    {
        foreach (var slotView in slots)
        {
            if (slotView.Shows(skillId))
                slotView.RefreshCooldown();
        }
    }
}
