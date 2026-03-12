using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Items;
using Hoellenspiralenspiel.Scripts.Items.Armors;
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
    private Dictionary<int, long> xpTable = new()
    {
        { 1, 0 },
        { 2, 525 },
        { 3, 1760 },
        { 4, 3781 },
        { 5, 7184 },
        { 6, 12186 },
        { 7, 19324 },
        { 8, 29377 },
        { 9, 43181 },
        { 10, 61693 },
        { 11, 85990 },
        { 12, 117506 },
        { 13, 157384 },
        { 14, 207736 },
        { 15, 269997 },
        { 16, 346462 },
        { 17, 439268 },
        { 18, 551295 },
        { 19, 685171 },
        { 20, 843709 },
        { 21, 1030734 },
        { 22, 1249629 },
        { 23, 1504995 },
        { 24, 1800847 },
        { 25, 2142652 },
        { 26, 2535122 },
        { 27, 2984677 },
        { 28, 3496798 },
        { 29, 4080655 },
        { 30, 4742836 },
        { 31, 5490247 },
        { 32, 6334393 },
        { 33, 7283446 },
        { 34, 8384398 },
        { 35, 9541110 },
        { 36, 10874351 },
        { 37, 12361842 },
        { 38, 14018289 },
        { 39, 15859432 },
        { 40, 17905634 },
        { 41, 20171471 },
        { 42, 22679999 },
        { 43, 25456123 },
        { 44, 28517857 },
        { 45, 31897771 },
        { 46, 35621447 },
        { 47, 39721017 },
        { 48, 44225461 },
        { 49, 49176560 },
        { 50, 54607467 },
        { 51, 60565335 },
        { 52, 67094245 },
        { 53, 74247659 },
        { 54, 82075627 },
        { 55, 90631041 },
        { 56, 99984974 },
        { 57, 110197515 },
        { 58, 121340161 },
        { 59, 133497202 },
        { 60, 146749362 },
        { 61, 161191120 },
        { 62, 176922628 },
        { 63, 194049893 },
        { 64, 212684946 },
        { 65, 232956711 },
        { 66, 255001620 },
        { 67, 278952403 },
        { 68, 304972236 },
        { 69, 333233648 },
        { 70, 363906163 },
        { 71, 397194041 },
        { 72, 433312945 },
        { 73, 472476370 },
        { 74, 514937180 },
        { 75, 560961898 },
        { 76, 610815862 },
        { 77, 664824416 },
        { 78, 723298169 },
        { 79, 786612664 },
        { 80, 855129128 },
        { 81, 929261318 },
        { 82, 1009443795 },
        { 83, 1096169525 },
        { 84, 1189918242 },
        { 85, 1291270350 },
        { 86, 1400795257 },
        { 87, 1519130326 },
        { 88, 1646943474 },
        { 89, 1784977296 },
        { 90, 1934009687 },
        { 91, 2094900291 },
        { 92, 2268549086 },
        { 93, 2455921256 },
        { 94, 2658074992 },
        { 95, 2876116901 },
        { 96, 3111280300 },
        { 97, 3364828162 },
        { 98, 3638186694 },
        { 99, 3932818530 },
        { 100, 4250334444 },
    };
    private AnimationTree AnimationTree { get; set; }

    [Export]
    public AudioStreamPlayer2D NoManaSound { get; set; }

    [Export]
    public int ManaBase { get; set; } = 60;

    private float ManaAddedFlat            => GetModifierSumOf(ModificationType.Flat, CombatStat.Mana);
    private float ManaPercentageMultiplier => 1 + GetModifierSumOf(ModificationType.Percentage, CombatStat.Mana);
    private float ManaMoreMultiplierTotal  => GetTotalMoreMultiplierOf(CombatStat.Mana);
    public  float ManaMaximum              => (int)((ManaBase + ManaAddedFlat) * ManaPercentageMultiplier * ManaMoreMultiplierTotal);

    public int XpCurrent      { get; set; }
    public int XpForNextLevel { get; set; }
    public int Level          { get; set; } = 1;

    [Export]
    public float ManaCurrent
    {
        get => manaCurrent;
        set => SetField(ref manaCurrent, Math.Min(value, ManaMaximum));
    }

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
                var damageTaken = new HitResult(10, HitType.Normal, LifeModificationMode.Damage, this, CombatStat.Armor);

                ReceiveDamage(damageTaken);

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
        if (item is BaseArmor armor)
        {
            var flatArmorMod = item.CreateCombatStatModifier(CombatStat.Armor, ModificationType.Flat, armor.ArmorvalueFinal);
            CombatStatModifiers.Add(flatArmorMod);
        }

        foreach (var modifier in item.GetExtrinsicModifiers())
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