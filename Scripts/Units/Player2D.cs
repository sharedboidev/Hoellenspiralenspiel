using Godot;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player2D : BaseUnit
{
    private AnimationTree AnimationTree { get; set; }
    private PackedScene   fireballScene = ResourceLoader.Load<PackedScene>("res://Scenes/Spells/fireball.tscn");

    public override void _Ready()
    {
        base._Ready();

        AnimationTree = GetNode<AnimationTree>(nameof(AnimationTree));
        Movementspeed = 300;
    }

    public override void _PhysicsProcess(double delta)
    {
        MovementDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity    = MovementDirection * Movementspeed;

        if(MovementDirection != Vector2.Zero)
        {
            AnimationTree.Set("parameters/StateMachine/MoveState/RunState/blend_position", MovementDirection * new Vector2(1, -1));
            AnimationTree.Set("parameters/StateMachine/MoveState/IdleState/blend_position", MovementDirection * new Vector2(1, -1));
        }

        MoveAndSlide();

        if (Input.IsActionJustPressed("F"))
        {
            var fireball = (Fireball)fireballScene.Instantiate();
            var container = GetNode<Node2D>("Fireballs");
            
            GetTree().CurrentScene.AddChild(fireball);
            
            fireball.Init(GlobalPosition, GetGlobalMousePosition());
        }
    }
}