using Godot;

namespace Hoellenspiralenspiel.Scripts.Units;

public partial class Player2D : CharacterBody2D
{
    [Export]
    public Vector2 InputVector { get; set; } = Vector2.Zero;

    [Export]
    public float Movementspeed { get; set; } = 300;

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsKeyPressed(Key.A) && Input.IsKeyPressed(Key.W))
        {
            InputVector = Vector2.Left + Vector2.Up;
        }
        else if (Input.IsKeyPressed(Key.S) && Input.IsKeyPressed(Key.D))
        {
            InputVector = Vector2.Down + Vector2.Right;
        }
        else if (Input.IsKeyPressed(Key.W) && Input.IsKeyPressed(Key.D))
        {
            InputVector = Vector2.Up + Vector2.Right;
        }
        else if (Input.IsKeyPressed(Key.A) && Input.IsKeyPressed(Key.S))
        {
            InputVector = Vector2.Down + Vector2.Left;
        }
        else if (Input.IsKeyPressed(Key.A))
        {
            InputVector = Vector2.Left;
        }
        else if (Input.IsKeyPressed(Key.D))
        {
            InputVector = Vector2.Right;
        }
        else if (Input.IsKeyPressed(Key.W))
        {
            InputVector = Vector2.Up;
        }
        else if (Input.IsKeyPressed(Key.S))
        {
            InputVector = Vector2.Down;
        }
        else
        {
            InputVector = Vector2.Zero;
        }

        Velocity = InputVector * Movementspeed;

        MoveAndSlide();
    }
}