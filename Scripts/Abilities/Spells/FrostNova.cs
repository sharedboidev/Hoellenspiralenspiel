using System.Threading.Tasks;
using Godot;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Abilities.Spells;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class FrostNova : Area2D,
								 ISpell
{
	private CircleShape2D circleShape = ResourceLoader.Load<CircleShape2D>("res://Resources/CircleShape.tres");

	[Export] public CollisionShape2D CollisionShape;
	private         float            expansionTime = 0.2f;
	private         float            lifeTime;
	private         float            radius = 1000f;
	private         CircleShape2D    runtimeShape;
	private         FrostNovaSkill   skill;
	[Export] public Sprite2D         Sprite;
	private         float            spriteBaseRadius;

	public void Init(BaseSkill s, Vector2 globalPlayerPosition, Vector2 _)
	{
		skill          = s as FrostNovaSkill;
		GlobalPosition = globalPlayerPosition;
	}

	public override async void _Ready()
	{
		runtimeShape     =  (CircleShape2D)circleShape.Duplicate();
		BodyEntered      += OnBodyEntered;
		spriteBaseRadius =  Sprite.Texture.GetWidth() / 2f;
		_                =  ExpandAsync();
	}

	private async Task ExpandAsync()
	{
		const float tick       = 0.05f;
		var         radiusStep = radius / (expansionTime / tick);

		while (runtimeShape.Radius < radius)
		{
			runtimeShape.Radius = Mathf.Min(runtimeShape.Radius + radiusStep, radius);
			Sprite.Scale        = Vector2.One * (runtimeShape.Radius / spriteBaseRadius);
			CollisionShape.SetShape(runtimeShape);
			await ToSignal(GetTree().CreateTimer(tick), SceneTreeTimer.SignalName.Timeout);
		}

		QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("monsters") && body is BaseEnemy enemy)
		{
			var result = skill.MakeRealDamage(enemy);
			enemy.LifeCurrent -= (int)result.Value;
			enemy.InstatiateFloatingCombatText(result, GetTree().CurrentScene, new Vector2(0, -60));
		}
	}
}
