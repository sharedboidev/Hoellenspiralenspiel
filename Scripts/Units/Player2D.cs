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

public class FireballContainer { }

public partial class Player2D : BaseUnit
{
    public delegate void EquipmentChangedEventHandler();

    public delegate void LeveledUpEventHandler(Player2D player);

    private readonly PackedScene     skillBarIcon = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
    private readonly List<BaseSkill> skills       = new();
    private          LevelUpEffect   levelUpEffect;
    [Export] private ResourceOrb     lifeOrb;
    private          float           manaCurrent;
    [Export] private ResourceOrb     manaOrb;
    public          float           manaProSekunde = .5f;
    [Export] public  HBoxContainer   SkillBar;
    private          long            xpTotal;
    private          AnimationTree   AnimationTree { get; set; }

    [Export]
    public AudioStreamPlayer2D NoManaSound { get; set; }

    public  int   ManaBase                 => 3 + AwarenessFinal + 5 * IntelligenceFinal;
    private float ManaAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Mana);
    private float ManaPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Mana);
    public  float ManaMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Mana);
    public  float ManaMaximum              => (int)((ManaBase + ManaAddedFlat) * ManaPercentageMultiplier * ManaMoreMultiplierTotal);

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
        ManaCurrent = ManaMaximum;

        lifeOrb.Init(this, ResourceType.Life);
        manaOrb.Init(this, ResourceType.Mana);

        XpForNextLevel  =  XpTable.GetTotalXpNeededForLevel(Level + 1);
        PropertyChanged += OnPropertyChanged;

        base._Ready();

        ConfigureSkillbar();

        AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
        levelUpEffect = GetNode<LevelUpEffect>(nameof(LevelUpEffect));
        //AnimationTree = GetNode<AnimationTree>("AnimationTreeNEW");
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(XpTotal) && XpTotal >= XpForNextLevel)
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
        lightningStrikeActionBarItem.Init(skills.ElementAt(2), "res://very_cool_circle.tscn", Key.R);

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
        HandleCollision();
    }

    private void ResolveManareg(double delta)
    {
        if (ManaCurrent < ManaMaximum)
        {
            ManaCurrent += manaProSekunde * (float)delta;
            ManaCurrent =  Mathf.Clamp(ManaCurrent, 0, ManaMaximum);
            manaOrb.SetRessource(ManaCurrent);
        }
    }

    protected override void ResolveLifeReg(double delta)
    {
        base.ResolveLifeReg(delta);

        lifeOrb.SetRessource(LifeCurrent);
    }

    private void HandleCollision()
    {
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            var collider  = collision.GetCollider() as Node;

            if (collider != null && collider.IsInGroup("monsters"))
            {
                var hit = new HitResult(10, HitType.Normal, LifeModificationMode.Damage, this, CombatStat.Armor);

                if (hit.WasDodged)
                {
                    this.InstatiateFloatingCombatText(hit, GetTree().CurrentScene, new Vector2(0, -75));

                    continue;
                }

                ReceiveDamage(hit);

                lifeOrb.SetRessource(LifeCurrent);
            }
        }
    }

    private void HandleMovementInputs()
    {
        MovementDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity          = MovementDirection * Movementspeed;

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
    {
        ManaCurrent -= mana;
        manaOrb.SetRessource(ManaCurrent);
    }

    public void EquipItem(BaseItem item)
    {
        if (item is BaseArmor armor)
            ArmorBase += armor.ArmorvalueFinal;

        foreach (var modifier in item.GetExtrinsicModifiers())
        {
            var newModifier = item.CreateCombatStatModifier(modifier);
            CombatStatModifiers.Add(newModifier);
        }

        EquipmentChanged?.Invoke();
    }

    public void UnequipItem(BaseItem item)
    {
        if (item is BaseArmor armor)
            ArmorBase -= armor.ArmorvalueFinal;
        
        var itemId = item.ToString();

        RemoveModifiers(itemId);

        EquipmentChanged?.Invoke();
    }
}