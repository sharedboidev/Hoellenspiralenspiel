using Godot;

namespace Hoellenspiralenspiel.Scripts.Environment;

public partial class CellarDoor : PanelContainer
{
    private CollisionShape2D collisionPolygon;
    private TextureRect      godRaysTexture;

    public override void _Ready()
    {
        godRaysTexture   = GetNode<TextureRect>("%GodRays");
        collisionPolygon = GetNode<CollisionShape2D>("%CollisionShape2D");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseEvent)
            return;

        GD.Print("Init Scene Transition!!");
    }

    public void _mouse_entered() => godRaysTexture.SetVisible(true);

    public void _mouse_exited() => godRaysTexture.SetVisible(false);
}