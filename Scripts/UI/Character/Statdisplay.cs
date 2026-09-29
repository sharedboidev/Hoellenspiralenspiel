using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Stats;

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
    private          RichTextLabel  meleeBlockLabel;
    private          RichTextLabel  meleeCritChanceLabel;
    private          RichTextLabel  meleeParryLabel;
    private          RichTextLabel  movementspeedLabel;
    private          RichTextLabel  spellBlockLabel;
    private          RichTextLabel  spellDamageLabel;
    private          RichTextLabel  spellParryLabel;
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

    public void Render(StatSheet stats)
    {
        RenderAttributes(stats);
        RenderRessources(stats);
        RenderDefences(stats);
        RenderOffences(stats);
        RenderUtilities(stats);
    }

    private static string AsBonusPercent(StatSheet stats, CombatStat stat)
        => "+" + ((stats.GetTotalMultiplier(stat) - 1) * 100).ToString("0.##") + "%";

    private static string AsChance(StatSheet stats, CombatStat stat)
        => CombatFormulas.ClampChance(stats.GetFinal(stat)).ToString("0.##") + "%";

    private void RenderUtilities(StatSheet stats)
    {
        movementspeedLabel.Text = stats.GetFinal(CombatStat.Movementspeed).ToString("N0");
        areaLabel.Text          = AsBonusPercent(stats, CombatStat.AreaOfEffect);
        lightRadiusLabel.Text   = stats.GetFinal(CombatStat.LightRadius).ToString("N0") + "%";
    }

    private void RenderOffences(StatSheet stats)
    {
        meleeCritChanceLabel.Text = stats.GetFinal(CombatStat.CriticalHitChance).ToString("0.##") + "%";
        critDamageLabel.Text      = "+" + stats.GetFinal(CombatStat.CriticalDamage).ToString("N0") + "%";
        attackspeedLabel.Text     = stats.GetFinal(CombatStat.Attackspeed).ToString("0.##") + "/s";
        spellDamageLabel.Text     = AsBonusPercent(stats, CombatStat.SpellDamage);
    }

    private void RenderDefences(StatSheet stats)
    {
        armorLabel.Text         = stats.GetFinalWhole(CombatStat.Armor).ToString("N0");
        dodgeLabel.Text         = stats.GetFinalWhole(CombatStat.Dodge).ToString("0.##") + "%";
        meleeBlockLabel.Text    = AsChance(stats, CombatStat.MeleeBlock);
        spellBlockLabel.Text    = AsChance(stats, CombatStat.SpellBlock);
        meleeParryLabel.Text    = AsChance(stats, CombatStat.MeleeParry);
        spellParryLabel.Text    = AsChance(stats, CombatStat.SpellParry);
        fireResiLabel.Text      = stats.GetFinalWhole(CombatStat.FireResistance).ToString("N0") + "%";
        frostResistance.Text    = stats.GetFinalWhole(CombatStat.FrostResistance).ToString("N0") + "%";
        lightningResiLabel.Text = stats.GetFinalWhole(CombatStat.LightningResistance).ToString("N0") + "%";
    }

    private void RenderRessources(StatSheet stats)
    {
        lifeLabel.Text             = stats.GetFinalWhole(CombatStat.Life).ToString("N0");
        liferegenerationLabel.Text = stats.GetFinalWhole(CombatStat.Liferegeneration).ToString("N0");
        manaLabel.Text             = stats.GetFinalWhole(CombatStat.Mana).ToString("N0");
        manaregenerationLabel.Text = stats.GetFinal(CombatStat.Manaregeneration).ToString("0.##");

    }
    private void RenderAttributes(StatSheet stats)
    {
        strengthLabel.Text  = stats.GetFinalWhole(CombatStat.Strength).ToString("N0");
        dexLabel.Text       = stats.GetFinalWhole(CombatStat.Dexterity).ToString("N0");
        intLabel.Text       = stats.GetFinalWhole(CombatStat.Intelligence).ToString("N0");
        constiLabel.Text    = stats.GetFinalWhole(CombatStat.Constitution).ToString("N0");
        awarenessLabel.Text = stats.GetFinalWhole(CombatStat.Awareness).ToString("N0");
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
        meleeBlockLabel    = GetNode<RichTextLabel>("%MeleeBlock");
        spellBlockLabel    = GetNode<RichTextLabel>("%SpellBlock");
        meleeParryLabel    = GetNode<RichTextLabel>("%MeleeParry");
        spellParryLabel    = GetNode<RichTextLabel>("%SpellParry");
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