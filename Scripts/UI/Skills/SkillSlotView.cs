using Godot;
using Hoellenspiralenspiel.Interfaces;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

public partial class SkillSlotView : Control
{
    public delegate void PickerRequestedEventHandler(SkillSlotView slot);

    private TextureProgressBar cooldownOverlay;
    private SkillCooldowns     cooldowns;
    private TextureRect        icon;
    private Label              keyLabel;
    private IHero              owner;
    private SkillResource      skill;
    private Label              timeLabel;

    public int Slot { get; private set; }

    public event PickerRequestedEventHandler PickerRequested;

    //Die Leiste entsteht, während die Szene noch lädt. _Ready läuft dann erst später, deshalb holt Init die Knoten selbst
    public void Init(int slot, string keyText, IHero skillOwner)
    {
        icon            = GetNode<TextureRect>("%Icon");
        cooldownOverlay = GetNode<TextureProgressBar>("%CooldownOverlay");
        keyLabel        = GetNode<Label>("%KeyLabel");
        timeLabel       = GetNode<Label>("%TimeLabel");

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
        skill        = newSkill;
        icon.Texture = skill?.Icon;

        RefreshCooldown();
    }

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
        if (skill is null || owner is null)
            return SkillTooltip.BuildNote("Empty", "Right click to assign a skill");

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
