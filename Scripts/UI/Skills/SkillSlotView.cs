using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.UI.Skills;

//Ein Platz der Skill-Leiste. Zeigt Icon, Taste und Abklingzeit. Eingesetzt wird der Skill vom Spieler, nicht von hier
public partial class SkillSlotView : Control
{
    public delegate void PickerRequestedEventHandler(SkillSlotView slot);

    private TextureProgressBar cooldownOverlay;
    private SkillCooldowns     cooldowns;
    private TextureRect        icon;
    private Label              keyLabel;
    private SkillResource      skill;
    private Label              timeLabel;

    public int Slot { get; private set; }

    //Der Spieler will diesem Platz einen anderen Skill geben
    public event PickerRequestedEventHandler PickerRequested;

    //Die Leiste entsteht, während die Szene noch lädt. _Ready läuft dann erst später, deshalb holt Init die Knoten selbst
    public void Init(int slot, string keyText, SkillCooldowns unitCooldowns)
    {
        icon            = GetNode<TextureRect>("%Icon");
        cooldownOverlay = GetNode<TextureProgressBar>("%CooldownOverlay");
        keyLabel        = GetNode<Label>("%KeyLabel");
        timeLabel       = GetNode<Label>("%TimeLabel");

        Slot          = slot;
        cooldowns     = unitCooldowns;
        keyLabel.Text = keyText;

        SetProcess(false);
    }

    public void ShowSkill(SkillResource newSkill)
    {
        skill        = newSkill;
        icon.Texture = skill?.Icon;
        TooltipText  = skill is null ? "Empty\n\nRight click to assign a skill" : $"{skill.GetTooltip()}\n\nRight click to change";

        RefreshCooldown();
    }

    public bool Shows(string skillId)
        => skill is not null && skill.Id == skillId;

    //Die Anzeige läuft nur mit, solange eine Abklingzeit läuft
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
