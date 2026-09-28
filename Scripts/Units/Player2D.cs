using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Armors;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using Hoellenspiralenspiel.Scripts.Utils;
using ResourceOrb = Hoellenspiralenspiel.Scripts.UI.Character.ResourceOrb;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player2D : BaseUnit
{
    public delegate void EquipmentChangedEventHandler();

    public delegate void LeveledUpEventHandler(Player2D player);

    //Lichter des Spielers mit der Größe, die in der Szene eingestellt ist
    private readonly Dictionary<PointLight2D, float> lightBaseScales = new();
    private readonly PackedScene                     skillBarIcon    = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
    private readonly List<BaseSkill>                 skills          = new();
    private          double                          contactDamageCooldownLeftSec;
    private          LevelUpEffect                   levelUpEffect;
    [Export] private ResourceOrb                     lifeOrb;
    private          int                             lightRadiusBase = 100;
    private          float                           manaCurrent;
    [Export] private ResourceOrb                     manaOrb;
    private          float                           manaregenerationBase = .5f;
    [Export] public  HBoxContainer                   SkillBar;
    private          long                            xpTotal;

    public Player2D()
        => Stats.Update(sheet =>
        {
            sheet.SetBase(CombatStat.Manaregeneration, manaregenerationBase);
            sheet.SetBase(CombatStat.LightRadius, lightRadiusBase);
        });

    private AnimationTree AnimationTree { get; set; }

    [Export]
    public AudioStreamPlayer2D NoManaSound { get; set; }

    [Export]
    public float ContactDamageCooldownSec { get; set; } = 1f;

    //Mana pro Sekunde
    [Export]
    public float ManaregenerationBase
    {
        get => manaregenerationBase;
        set => SetBaseStat(ref manaregenerationBase, value, CombatStat.Manaregeneration);
    }

    //Lichtradius in Prozent. 100 entspricht der Größe, die in der Szene eingestellt ist
    [Export]
    public int LightRadiusBase
    {
        get => lightRadiusBase;
        set => SetBaseStat(ref lightRadiusBase, value, CombatStat.LightRadius);
    }

    public int   ManaBase              => (int)Stats.GetEffectiveBase(CombatStat.Mana);
    public float ManaMaximum           => Stats.GetFinalWhole(CombatStat.Mana);
    public float ManaregenerationFinal => Stats.GetFinal(CombatStat.Manaregeneration);
    public float LightRadiusFinal      => Stats.GetFinal(CombatStat.LightRadius);

    public long XpTotal
    {
        get => xpTotal;
        private set => SetField(ref xpTotal, value);
    }

    public long XpForNextLevel                { get; private set; }
    public int  Level                         { get; private set; } = 1;
    public long XpDelta                       => XpTotal - XpFloorCurrentLevel;
    public long XpFloorCurrentLevel           => XpTable.GetTotalXpNeededForLevel(Level);
    public int  AttributePointsAllowedToSpend { get; set; }

    [Export]
    public float ManaCurrent
    {
        get => manaCurrent;
        set => SetField(ref manaCurrent, Math.Min(value, ManaMaximum));
    }

    public event EquipmentChangedEventHandler EquipmentChanged;
    public event LeveledUpEventHandler        LeveledUp;

    public override void _Ready()
    {
        lifeOrb.Init(this, ResourceType.Life);
        manaOrb.Init(this, ResourceType.Mana);

        XpForNextLevel  =  XpTable.GetTotalXpNeededForLevel(Level + 1);
        PropertyChanged += OnPropertyChanged;

        base._Ready();

        ManaCurrent = ManaMaximum;

        LoadLights();
        ConfigureSkillbar();

        AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
        levelUpEffect = GetNode<LevelUpEffect>(nameof(LevelUpEffect));
        //AnimationTree = GetNode<AnimationTree>("AnimationTreeNEW");
    }

    protected override void OnStatsRecalculated()
    {
        if (ManaCurrent > ManaMaximum)
            ManaCurrent = ManaMaximum;

        ApplyLightRadius();

        OnPropertyChanged(nameof(ManaMaximum));
    }

    private void LoadLights()
    {
        foreach (var light in this.GetAllChildren<PointLight2D>())
            lightBaseScales[light] = light.TextureScale;

        ApplyLightRadius();
    }

    //Der Lichtradius skaliert die Lichter relativ zu ihrer Größe aus der Szene
    private void ApplyLightRadius()
    {
        var factor = LightRadiusFinal / 100f;

        foreach (var (light, baseScale) in lightBaseScales)
            light.TextureScale = baseScale * factor;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(XpTotal) && Level < XpTable.MaxLevel && XpTotal >= XpForNextLevel)
            LevelUp();
    }

    public void GainExperience(int experienceGained)
        => XpTotal += experienceGained;

    public void LoseExperience(int experienceLost)
    {
        var xpNeededForCurrentLevel = XpTable.GetTotalXpNeededForLevel(Level);
        var totalXpDelta            = XpTotal - experienceLost;

        XpTotal = Math.Max(xpNeededForCurrentLevel, totalXpDelta);
    }

    public void LevelUp()
    {
        Level++;
        AttributePointsAllowedToSpend++;

        XpForNextLevel = XpTable.GetTotalXpNeededForLevel(Level + 1);

        levelUpEffect.Emit();

        LeveledUp?.Invoke(this);
    }

    public int GetRequiredAttributevalue(Requirement requirement)
        => requirement switch
        {
            Requirement.Strength       => StrengthFinal,
            Requirement.Dexterity      => DexterityFinal,
            Requirement.Intelligence   => IntelligenceFinal,
            Requirement.Constitution   => ConstitutionFinal,
            Requirement.Awareness      => AwarenessFinal,
            Requirement.CharacterLevel => Level,
            _                          => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, null)
        };

    private void ConfigureSkillbar()
    {
        AddSkillsToBar();
        SetSkillbarposition();
    }

    private void SetSkillbarposition()
    {
        var viewportSize     = GetViewportRect().Size;
        var skillbarSize     = SkillBar.Size;
        var skillbarPosition = new Vector2((viewportSize.X - skillbarSize.X) / 2, viewportSize.Y - 2 * skillbarSize.Y);

        SkillBar.Position = skillbarPosition;
    }

    private void AddSkillsToBar()
    {
        skills.Add(new FireballSkill(this));
        skills.Add(new FrostNovaSkill(this));
        skills.Add(new LightningStrikeSkill(this));

        var fireballActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        fireballActionBarItem.Init(skills.ElementAt(0), "res://Scenes/Spells/fireball.tscn", Key.F);

        var frostNovaActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        frostNovaActionBarItem.Init(skills.ElementAt(1), "res://Scenes/Spells/frost_nova.tscn", Key.E);

        var lightningStrikeActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        lightningStrikeActionBarItem.Init(skills.ElementAt(2), "res://Scenes/Spells/very_cool_circle.tscn", Key.R);

        SkillBar.AddChild(fireballActionBarItem);
        SkillBar.AddChild(frostNovaActionBarItem);
        SkillBar.AddChild(lightningStrikeActionBarItem);
    }

    public bool IsInAggroRangeOf(BaseEnemy enemy)
    {
        var distanceToEnemy = Math.Sqrt(GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition));

        return distanceToEnemy <= enemy.AggroRange;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        ResolveManareg(delta);
        HandleMovementInputs();
        HandleCollision(delta);
    }

    //Die Orbs hören auf die Änderung von ManaCurrent und LifeCurrent und müssen nicht eigens angestoßen werden
    private void ResolveManareg(double delta)
    {
        if (ManaCurrent < ManaMaximum)
        {
            ManaCurrent += ManaregenerationFinal * (float)delta;
            ManaCurrent =  Mathf.Clamp(ManaCurrent, 0, ManaMaximum);
        }
    }

    private void HandleCollision(double delta)
    {
        contactDamageCooldownLeftSec = Math.Max(0, contactDamageCooldownLeftSec - delta);

        if (contactDamageCooldownLeftSec > 0)
            return;

        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            var collider  = collision.GetCollider() as Node;

            if (collider == null || !collider.IsInGroup("monsters"))
                continue;

            var hit = new HitResult(10, HitType.Normal, LifeModificationMode.Damage, this, CombatStat.Armor);

            ReceiveDamage(hit);

            //Höchstens ein Kontakttreffer pro Abklingzeit, egal wie viele Gegner berührt werden
            contactDamageCooldownLeftSec = ContactDamageCooldownSec;

            return;
        }
    }

    private void HandleMovementInputs()
    {
        MovementDirection = Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown);
        Velocity          = MovementDirection * MovementspeedFinal;

        if (MovementDirection != Vector2.Zero)
        {
            AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", MovementDirection * new Vector2(1, -1));
            AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", MovementDirection * new Vector2(1, -1));
        }

        MoveAndSlide();
    }

    public bool CanUseAbility(float manaCost)
        => ManaCurrent >= manaCost;

    public void PlayOutOfMana()
    {
        if (!NoManaSound.IsPlaying())
            NoManaSound.Play();
    }

    public void ReduceMana(float mana)
        => ManaCurrent -= mana;

    //Alle Modifier eines Items tragen dessen Herkunft, damit sie beim Ablegen gemeinsam entfernt werden
    public void EquipItem(BaseItem item)
    {
        Stats.Update(sheet =>
        {
            if (item is BaseArmor armor)
                sheet.AddModifier(item.CreateCombatStatModifier(CombatStat.Armor, ModificationType.Flat, armor.ArmorvalueFinal));

            foreach (var modifier in item.GetExtrinsicModifiers())
                sheet.AddModifier(item.CreateCombatStatModifier(modifier));
        });

        EquipmentChanged?.Invoke();
    }

    public void UnequipItem(BaseItem item)
    {
        Stats.RemoveModifiersOf(item.ToString());

        EquipmentChanged?.Invoke();
    }
}
