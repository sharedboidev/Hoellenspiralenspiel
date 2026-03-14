using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public partial class Statdisplay : PanelContainer
{
    private          RichTextLabel  armorLabel;
    private          RichTextLabel  attackspeedLabel;
    private          RichTextLabel  awarenessLabel;
    private          RichTextLabel  constiLabel;
    private          RichTextLabel  critDamageLabel;
    private          RichTextLabel  dexLabel;
    private          RichTextLabel  dodgeLabel;
    [Export] private EquipmentPanel equipmentPanel;
    private          RichTextLabel  fireResiLabel;
    private          RichTextLabel  frostResistance;
    private          RichTextLabel  intLabel;
    private          RichTextLabel  lifeLabel;
    private          RichTextLabel  liferegenerationLabel;
    private          RichTextLabel  lightningResiLabel;
    private          RichTextLabel  meleeCritChanceLabel;
    private          RichTextLabel  movementspeedLabel;
    private          RichTextLabel  spellDamageLabel;
    private          RichTextLabel  strengthLabel;
    private          RichTextLabel  manaLabel;
    private          RichTextLabel  manaregenerationLabel;

    public override void _Ready()
    {
        FindAttributes();
        FindRessources();
        FindOffences();
        FindDefences();
        FindUtilities();
    }

    public void Render(Player2D player)
    {
        RenderAttributes(player);
        RenderRessources(player);
        RenderDefences(player);
        RenderOffences(player);
        RenderUtilities(player);
    }


    private void RenderUtilities(Player2D player)
        => movementspeedLabel.Text = player.Movementspeed.ToString("N0");

    private void RenderOffences(Player2D player)
    {
        meleeCritChanceLabel.Text = equipmentPanel.GetTotalMeleeCritChance().ToString("0.##") + "%";
        critDamageLabel.Text      = "+" + equipmentPanel.GetTotalCriticalDamage().ToString("N0") + "%";
        attackspeedLabel.Text     = "+" + equipmentPanel.GetTotalIncreasedAttackspeed().ToString("N0") + "%";
        spellDamageLabel.Text     = "+" + ((player.SpellDamagePercentageMultiplier * player.SpellDamageMoreMultiplierTotal - 1) * 100).ToString("N0") + "%";
    }

    private void RenderDefences(Player2D player)
    {
        armorLabel.Text         = equipmentPanel.GetTotalArmor().ToString("N0");
        dodgeLabel.Text         = equipmentPanel.GetTotalDodge().ToString("0.##") + "%";
        fireResiLabel.Text      = player.FireResiFinal.ToString("N0") + "%";
        frostResistance.Text    = player.FrostResiFinal.ToString("N0") + "%";
        lightningResiLabel.Text = player.LightningResiFinal.ToString("N0") + "%";
    }

    private void RenderRessources(Player2D player)
    {
        lifeLabel.Text             = player.LifeMaximum.ToString("N0");
        liferegenerationLabel.Text = player.LiferegenerationFinal.ToString("N0");
        manaLabel.Text             = player.ManaMaximum.ToString("N0");
        manaregenerationLabel.Text = player.manaProSekunde.ToString("0.##");

    }
    private void RenderAttributes(Player2D player)
    {
        strengthLabel.Text  = player.StrengthFinal.ToString("N0");
        dexLabel.Text       = player.DexterityFinal.ToString("N0");
        intLabel.Text       = player.IntelligenceFinal.ToString("N0");
        constiLabel.Text    = player.ConstitutionFinal.ToString("N0");
        awarenessLabel.Text = player.AwarenessFinal.ToString("N0");
    }

    private void FindUtilities()
        => movementspeedLabel = GetNode<RichTextLabel>("%Movementspeed");

    private void FindDefences()
    {
        armorLabel         = GetNode<RichTextLabel>("%Armor");
        dodgeLabel         = GetNode<RichTextLabel>("%Dodge");
        fireResiLabel      = GetNode<RichTextLabel>("%FireResistance");
        frostResistance    = GetNode<RichTextLabel>("%FrostResistance");
        lightningResiLabel = GetNode<RichTextLabel>("%LightningResistance");
    }

    private void FindOffences()
    {
        meleeCritChanceLabel = GetNode<RichTextLabel>("%MeleeCritChance");
        critDamageLabel      = GetNode<RichTextLabel>("%MeleeCritDamage");
        attackspeedLabel     = GetNode<RichTextLabel>("%Attackspeed");
        spellDamageLabel     = GetNode<RichTextLabel>("%SpellDamage");
    }

    private void FindRessources()
    {
        lifeLabel             = GetNode<RichTextLabel>("%Life");
        liferegenerationLabel = GetNode<RichTextLabel>("%Liferegeneration");
        manaLabel             = GetNode<RichTextLabel>("%Mana");
        manaregenerationLabel = GetNode<RichTextLabel>("%Manaregeneration");
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