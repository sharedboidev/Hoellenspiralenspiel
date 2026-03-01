using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.UI.Character;
using Hoellenspiralenspiel.Scripts.Units.Enemies;
using ResourceOrb = Hoellenspiralenspiel.Scripts.UI.Character.ResourceOrb;

namespace Hoellenspiralenspiel.Scripts.Units;

public class FireballContainer { }

public partial class Player2D : BaseUnit
{
    public delegate void EquipmentChangedEventHandler();

    private readonly PackedScene     skillBarIcon = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
    private readonly List<BaseSkill> skills       = new();
    [Export] private ResourceOrb     lifeOrb;
    private          float           manaCurrent;
    [Export] private ResourceOrb     manaOrb;
    private          float           manaProSekunde = 5f;
    [Export] public  HBoxContainer   SkillBar;
    private          AnimationTree   AnimationTree { get; set; }
    public           int             Level         { get; set; } = 1;

    [Export]
    public AudioStreamPlayer2D NoManaSound { get; set; }

    public float ManaCurrent
    {
        get => manaCurrent;
        set => SetField(ref manaCurrent, Math.Min(value, ManaMaximum));
    }

    [Export]
    public float ManaMaximum { get; set; } = 100;

    public event EquipmentChangedEventHandler EquipmentChanged;

    public override void _Ready()
    {
        ManaCurrent = ManaMaximum;

        lifeOrb.Init(this, ResourceType.Life);
        manaOrb.Init(this, ResourceType.Mana);

        base._Ready();

        ConfigureSkillbar();

        AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
        //AnimationTree = GetNode<AnimationTree>("AnimationTreeNEW");
    }

    public int GetRequiredAttributevalue(Requirement requirement)
        => requirement switch
        {
            Requirement.Strength       => 1,
            Requirement.Dexterity      => 1,
            Requirement.Intelligence   => 1,
            Requirement.Constitution   => 1,
            Requirement.Awareness      => 1,
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

    private void HandleCollision()
    {
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            var collider  = collision.GetCollider() as Node;

            if (collider != null && collider.IsInGroup("monsters"))
            {
                var monsters = collider as BaseUnit;

                var damageTaken = new HitResult(1, HitType.Normal, LifeModificationMode.Damage);
                this.InstatiateFloatingCombatText(damageTaken, GetTree().CurrentScene, new Vector2(0, -60));

                LifeCurrent -= (int)damageTaken.Value;
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
            //AnimationTree.Set("parameters/blend_position", MovementDirection * new Vector2(1, -1));
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
        foreach (var modifier in item.GetModifiers())
        {
            var newModifier = item.CreateCombatStatModifier(modifier);
            CombatStatModifiers.Add(newModifier);
        }

        EquipmentChanged?.Invoke();
    }

    public void UnequipItem(BaseItem item)
    {
        var modifierToRemove = CombatStatModifiers.Where(mod => mod.OriginId == item.ToString()).ToList();
        CombatStatModifiers = CombatStatModifiers.Except(modifierToRemove).ToList();

        EquipmentChanged?.Invoke();
    }
}