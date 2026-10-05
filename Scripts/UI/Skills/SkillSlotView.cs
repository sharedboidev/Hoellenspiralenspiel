using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Items;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillSlotView : Control
{
    public delegate void PickerRequestedEventHandler(SkillSlotView slot);

    private ConsumableBaseResource consumable;
    private int                    consumableCount;
    private TextureProgressBar     cooldownOverlay;
    private SkillCooldowns         cooldowns;
    private Label                  countLabel;
    private TextureRect            icon;
    private Label                  keyLabel;
    private IHero                  owner;
    private SkillResource          skill;
    private Label                  timeLabel;
    private Control                wrongWeapon;

    [Export]
    public Color OutOfStockTint { get; set; } = new(0.35f, 0.35f, 0.35f);

    public int Slot { get; private set; }

    public event PickerRequestedEventHandler PickerRequested;

    //Die Leiste entsteht, während die Szene noch lädt. _Ready läuft dann erst später, deshalb holt Init die Knoten selbst
    public void Init(int slot, string keyText, IHero skillOwner)
    {
        icon            = GetNode<TextureRect>("%Icon");
        cooldownOverlay = GetNode<TextureProgressBar>("%CooldownOverlay");
        keyLabel        = GetNode<Label>("%KeyLabel");
        timeLabel       = GetNode<Label>("%TimeLabel");
        countLabel      = GetNode<Label>("%CountLabel");
        wrongWeapon     = GetNode<Control>("%WrongWeapon");

        Slot          = slot;
        owner         = skillOwner;
        cooldowns     = skillOwner.SkillCooldowns;
        keyLabel.Text = keyText;

        SetProcess(false);
    }

    public void ShowKey(string keyText)
        => keyLabel.Text = keyText;

    public void ShowSkill(SkillResource newSkill)
    {
        skill      = newSkill;
        consumable = null;

        icon.Texture       = skill?.Icon;
        icon.Modulate      = Colors.White;
        countLabel.Visible = false;

        RefreshCooldown();
        RefreshWeaponFit();
    }

    public void ShowConsumable(ConsumableBaseResource newConsumable, int countInInventory)
    {
        if (newConsumable is null)
        {
            ShowSkill(null);

            return;
        }

        skill           = null;
        consumable      = newConsumable;
        consumableCount = countInInventory;

        icon.Texture       = consumable.Icon;
        icon.Modulate      = countInInventory > 0 ? Colors.White : OutOfStockTint;
        countLabel.Text    = countInInventory.ToString("N0");
        countLabel.Visible = true;

        RefreshCooldown();
        RefreshWeaponFit();
    }

    //Ein Skill für Nahkampfwaffen liegt rot hinterlegt, solange der Held einen Bogen trägt. Einsetzen lässt er sich dann nicht
    public void RefreshWeaponFit()
        => wrongWeapon.Visible = skill is not null && owner is not null && !SkillGate.FitsWeapon(skill.Definition, owner.Weapon.IsRanged);

    public bool Shows(string skillId)
        => skill is not null && skill.Id == skillId;

    public void RefreshCooldown()
    {
        var isRunning = skill is not null && cooldowns is not null && !cooldowns.IsReady(skill.Id);

        SetProcess(isRunning);

        if (isRunning)
            ShowRemainingTime();
        else
            ClearRemainingTime();
    }

    public override void _Process(double delta)
    {
        if (skill is null || cooldowns.IsReady(skill.Id))
        {
            RefreshCooldown();

            return;
        }

        ShowRemainingTime();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
            return;

        PickerRequested?.Invoke(this);

        AcceptEvent();
    }

    public override string _GetTooltip(Vector2 atPosition)
        => BuildTooltip();

    public override Control _MakeCustomTooltip(string forText)
        => SkillTooltip.CreateContent(forText);

    public string BuildTooltip()
    {
        if (consumable is not null)
            return ConsumableTooltip.Build(consumable, consumableCount, "Right click to change");

        if (skill is null || owner is null)
            return SkillTooltip.BuildNote("Empty", "Right click to assign a skill or potion");

        return SkillTooltip.Build(skill, owner);
    }

    private void ShowRemainingTime()
    {
        var remainingSec = cooldowns.GetRemainingSec(skill.Id);

        cooldownOverlay.MaxValue = cooldowns.GetTotalSec(skill.Id);
        cooldownOverlay.Value    = remainingSec;
        timeLabel.Text           = remainingSec.ToString("0.0");
    }

    private void ClearRemainingTime()
    {
        cooldownOverlay.Value = 0;
        timeLabel.Text        = string.Empty;
    }
}
