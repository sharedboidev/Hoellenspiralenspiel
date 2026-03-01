using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class Statdisplay : PanelContainer
{
    [Export]
    private EquipmentPanel equipmentPanel;
    private RichTextLabel  armorLabel;
    private RichTextLabel  attackspeedLabel;
    private RichTextLabel  awarenessLabel;
    private RichTextLabel  constiLabel;
    private RichTextLabel  dexLabel;
    private RichTextLabel  dodgeLabel;
    private RichTextLabel  intLabel;
    private RichTextLabel  lifeLabel;
    private RichTextLabel  meleeCritChanceLabel;
    private RichTextLabel  critDamageLabel;
    private RichTextLabel  movementspeedLabel;
    private RichTextLabel  strengthLabel;

    public override void _Ready()
    {
        FindAttributes();
        FindOffences();
        FindDefences();
        FindUtilities();
    }

    public void Render(Player2D player)
    {
        strengthLabel.Text  = player.StrengthFinal.ToString("N0");
        dexLabel.Text       = player.DexterityFinal.ToString("N0");
        intLabel.Text       = player.IntelligenceFinal.ToString("N0");
        constiLabel.Text    = player.ConstitutionFinal.ToString("N0");
        awarenessLabel.Text = player.AwarenessFinal.ToString("N0");

        lifeLabel.Text  = player.LifeMaximum.ToString("N0");
        armorLabel.Text = equipmentPanel.GetTotalArmor().ToString("N0");
        dodgeLabel.Text = equipmentPanel.GetTotalDodge().ToString("0.##") + "%";

        meleeCritChanceLabel.Text = equipmentPanel.GetTotalMeleeCritChance().ToString("0.##") + "%";
        critDamageLabel.Text      = "+" + equipmentPanel.GetTotalCriticalDamage().ToString("N0") + "%";
        attackspeedLabel.Text     = "+"+ equipmentPanel.GetTotalIncreasedAttackspeed().ToString("N0")+"%";

        movementspeedLabel.Text = player.Movementspeed.ToString("N0");
    }
    
    private void FindUtilities()
        => movementspeedLabel = GetNode<RichTextLabel>("%Movementspeed");

    private void FindDefences()
    {
        lifeLabel  = GetNode<RichTextLabel>("%Life");
        armorLabel = GetNode<RichTextLabel>("%Armor");
        dodgeLabel = GetNode<RichTextLabel>("%Dodge");
    }

    private void FindOffences()
    {
        meleeCritChanceLabel = GetNode<RichTextLabel>("%MeleeCritChance");
        critDamageLabel = GetNode<RichTextLabel>("%MeleeCritDamage");
        attackspeedLabel     = GetNode<RichTextLabel>("%Attackspeed");
    }

    private void FindAttributes()
    {
        strengthLabel  = GetNode<RichTextLabel>("%Strength");
        dexLabel       = GetNode<RichTextLabel>("%Dexterity");
        intLabel       = GetNode<RichTextLabel>("%Intelligence");
        constiLabel    = GetNode<RichTextLabel>("%Constitution");
        awarenessLabel = GetNode<RichTextLabel>("%Awareness");
    }
}