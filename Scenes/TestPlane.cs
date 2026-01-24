using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Units;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class TestPlane : Node
{
	private readonly Random                isEliteRng          = new();
	private readonly Random                isRareRng           = new();
	private readonly RandomNumberGenerator rng                 = new();
	[Export] public  int                   MaxTries            = 20;
	[Export] public  float                 MinDistanceToPlayer = 220f;
	[Export] public  PackedScene           MonsterScene;
	[Export] public  Node2D                MonstersContainer;
	[Export] public  Player2D              PlayerPath;

	public override void _Ready()
		=> rng.Randomize();

	public void _on_monster_spawn_timer_timeout()
	{
		var spawnPos = GetRandomVisiblePointNotNearPlayer();

		if (spawnPos == null)
			return;

		var monster = (TestEnemy)MonsterScene.Instantiate();

		if (IsRare())
		{
			monster.Movementspeed =  250f;
			monster.LifeBase      =  350;
			monster.Scale         *= 1.3f;
		}
		else
		{
			if (IsElite())
			{
				monster.Movementspeed =  350f;
				monster.LifeBase      =  500;
				monster.Scale         *= 2f;
			}
			else
			{
				monster.Movementspeed = 150f;
				monster.LifeBase      = 200;
			}
		}

		MonstersContainer.AddChild(monster);
		monster.GlobalPosition = spawnPos.Value;
	}

	private bool IsRare()
		=> isRareRng.Next(1, 11) == 1;

	private bool IsElite()
		=> isEliteRng.Next(1, 16) == 1;

	private Vector2? GetRandomVisiblePointNotNearPlayer()
	{
		var rect    = GetViewport().GetVisibleRect();
		var padding = 64f;
		rect.Position += new Vector2(padding, padding);
		rect.Size     -= new Vector2(padding * 2f, padding * 2f);

		if (rect.Size.X <= 0 || rect.Size.Y <= 0)
			return null;

		var minDistSq = MinDistanceToPlayer * MinDistanceToPlayer;
		var playerPos = PlayerPath.GlobalPosition;

		for (var i = 0; i < MaxTries; i++)
		{
			var x = rng.RandfRange(rect.Position.X, rect.End.X);
			var y = rng.RandfRange(rect.Position.Y, rect.End.Y);
			var p = new Vector2(x, y);

			if (p.DistanceSquaredTo(playerPos) >= minDistSq)
				return p;
		}

		return null;
	}
}
