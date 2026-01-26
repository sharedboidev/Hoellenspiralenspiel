using System;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Controllers;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Models;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class Fireball : Area2D
{
	private readonly Random           critRng   = new();
	private readonly Random           damageRng = new();
	[Export] public  AnimatedSprite2D AnimationSprite;
	private          Vector2          richtung; 
    public           int     MaxForks      { get; set; } = 5;
    public           int     TimesForked   { get; set; }
    public           double  LebenszeitSec { get; set; } = 15;
    public           double  LebenszeitCurrentSec { get; set; }

	
	
	public override void _Ready()
	{
		AnimationSprite.Play("default");
		BodyEntered += OnBodyEntered;
	}

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("monsters"))
        {
            var damage  = damageRng.Next(50, 251);
            var isCrit  = critRng.Next(1, 11) == 10;
            var hitType = isCrit ? HitType.Critical : HitType.Normal;
            damage = isCrit ? (int)(damage * 1.3m) : damage;

            var enemy = body as BaseEnemy;
            enemy.LifeCurrent -= damage;

            var fakeHit = new HitResult(damage, hitType, LifeModificationMode.Damage);
            enemy.InstatiateFloatingCombatText(fakeHit, GetTree().CurrentScene, new Vector2(0, -60));

            var controller = GetTree().CurrentScene.GetNode<EnemyController>(nameof(EnemyController));

            if (controller.SpawnedEnemies.Count < 2)
            {
                QueueFree();
                return;
            }

            var twoRandomFriends = controller.SpawnedEnemies.Take(2);
            var fireballScene    = ResourceLoader.Load<PackedScene>("res://Scenes/Spells/fireball.tscn");
            var timesForked      = TimesForked + 1;

            foreach (var friend in twoRandomFriends)
            {
                if (TimesForked >= MaxForks)
                    continue;

                var fireball = (Fireball)fireballScene.Instantiate();
                fireball.TimesForked = timesForked;

                GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, fireball);

                fireball.Init(body.Position, friend.Position);
            }

            QueueFree();
        }
    }

    public void Init(Vector2 startGlobal, Vector2 destinationGlobal)
    {
        GlobalPosition = startGlobal;

        richtung = destinationGlobal - startGlobal;

        if (richtung.LengthSquared() < 0.0001f)
            richtung = Vector2.Right;
        else
            richtung = richtung.Normalized();

        Rotation = (-richtung).Angle();
    }

    public override void _Process(double d)
    {
        var delta = Convert.ToSingle(d);

        Position             += richtung * 800 * delta;
        LebenszeitCurrentSec += d;

        if(LebenszeitCurrentSec >= LebenszeitSec)
        {
            LebenszeitCurrentSec = 0;
            QueueFree();
        }
    }
}