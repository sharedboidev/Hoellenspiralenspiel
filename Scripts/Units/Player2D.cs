using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Items.Weapons;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.UI;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

namespace Hoellenspiralenspiel.Scripts.Units;

public class FireballContainer { }

public partial class Player2D : BaseUnit
{
    private readonly PackedScene     skillBarIcon = ResourceLoader.Load<PackedScene>("res://Scenes/UI/cooldown_skill.tscn"); //.Instantiate<CooldownSkill>();
    private readonly List<BaseSkill> skills       = new();
    private          Node2D          Flamethrower;
    [Export] private PackedScene     FlamethrowerScene = ResourceLoader.Load<PackedScene>("res://Scenes/Spells/nova.tscn");
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

    public override void _Ready()
    {
        ManaCurrent = ManaMaximum;

        lifeOrb.Init(this, ResourceType.Life);
        manaOrb.Init(this, ResourceType.Mana);

        base._Ready();

        ConfigureSkillbar();

        AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
    }

    public int GetRequiredAttributevalue(Requirements requirement) => requirement switch
    {
        Requirements.Strength => 1,
        Requirements.Dexterity => 1,
        Requirements.Intelligence => 1,
        Requirements.Constitution => 1,
        Requirements.Awareness => 1,
        Requirements.CharacterLevel => Level,
        _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, null)
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

        var fireballActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        fireballActionBarItem.Init(skills.First(), "res://Scenes/Spells/fireball.tscn", Key.F);

        var frostNovaActionBarItem = skillBarIcon.Instantiate<CooldownSkill>();
        frostNovaActionBarItem.Init(skills.Last(), "res://Scenes/Spells/frost_nova.tscn", Key.E);

        SkillBar.AddChild(fireballActionBarItem);
        SkillBar.AddChild(frostNovaActionBarItem);
    }

    public bool IsInAggroRangeOf(BaseEnemy enemy)
    {
        var distanceToEnemy = Math.Sqrt(GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition));

        return distanceToEnemy <= enemy.AggroRange;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (ManaCurrent < ManaMaximum)
        {
            ManaCurrent += manaProSekunde * (float)delta;
            ManaCurrent =  Mathf.Clamp(ManaCurrent, 0, ManaMaximum);
            manaOrb.SetRessource(ManaCurrent);
        }

        //meaningful movement
        var shitParticles    = Input.IsMouseButtonPressed(MouseButton.Left);
        var tmpMovementspeed = shitParticles ? Movementspeed * .3f : Movementspeed;

        MovementDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity          = MovementDirection * tmpMovementspeed;

        if (MovementDirection != Vector2.Zero)
        {
            AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", MovementDirection * new Vector2(1, -1));
            AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", MovementDirection * new Vector2(1, -1));
        }

        MoveAndSlide();

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

        if (shitParticles)
        {
            Flamethrower                ??= FlamethrowerScene.Instantiate<Node2D>();
            Flamethrower.GlobalPosition =   GlobalPosition;

            var richtung = GlobalPosition - GetViewport().GetCamera2D().GetGlobalMousePosition();

            if (richtung.LengthSquared() < 0.0001f)
                richtung = Vector2.Right;
            else
                richtung = richtung.Normalized();

            Flamethrower.Rotation = (-richtung).Angle();

            if (GetNode("SpellsContainer").GetChildren().All(c => c.Name != Flamethrower.Name))
                GetNode("SpellsContainer").AddChild(Flamethrower);
        }
        else
        {
            Flamethrower?.QueueFree();
            Flamethrower = null;
        }
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
}