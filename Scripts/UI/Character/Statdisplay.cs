using Godot;
using Hoellenspiralenspiel.Enums;
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
    private          RichTextLabel  areaLabel;
    private          RichTextLabel  lightRadiusLabel;

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

    private static string AsBonusPercent(Player2D player, CombatStat stat)
        => "+" + ((player.Stats.GetTotalMultiplier(stat) - 1) * 100).ToString("0.##") + "%";

    private void RenderUtilities(Player2D player)
    {
        movementspeedLabel.Text = player.MovementspeedFinal.ToString("N0");
        areaLabel.Text          = AsBonusPercent(player, CombatStat.AreaOfEffect);
        lightRadiusLabel.Text   = player.LightRadiusFinal.ToString("N0") + "%";
    }

    private void RenderOffences(Player2D player)
    {
        meleeCritChanceLabel.Text = player.Stats.GetFinal(CombatStat.CriticalHitChance).ToString("0.##") + "%";
        critDamageLabel.Text      = "+" + player.Stats.GetFinal(CombatStat.CriticalDamage).ToString("N0") + "%";
        attackspeedLabel.Text     = player.AttacksPerSecondFinal.ToString("0.##") + "/s";
        spellDamageLabel.Text     = AsBonusPercent(player, CombatStat.SpellDamage);
    }

    private void RenderDefences(Player2D player)
    {
        armorLabel.Text         = player.ArmorFinal.ToString("N0");
        dodgeLabel.Text         = player.DodgeFinal.ToString("0.##") + "%";
        fireResiLabel.Text      = player.FireResiFinal.ToString("N0") + "%";
        frostResistance.Text    = player.FrostResiFinal.ToString("N0") + "%";
        lightningResiLabel.Text = player.LightningResiFinal.ToString("N0") + "%";
    }

    private void RenderRessources(Player2D player)
    {
        lifeLabel.Text             = player.LifeMaximum.ToString("N0");
        liferegenerationLabel.Text = player.LiferegenerationFinal.ToString("N0");
        manaLabel.Text             = player.ManaMaximum.ToString("N0");
        manaregenerationLabel.Text = player.ManaregenerationFinal.ToString("0.##");

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
    {
        movementspeedLabel = GetNode<RichTextLabel>("%Movementspeed");
        areaLabel          = GetNode<RichTextLabel>("%Area");
        lightRadiusLabel   = GetNode<RichTextLabel>("%LightRadius");
    }

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