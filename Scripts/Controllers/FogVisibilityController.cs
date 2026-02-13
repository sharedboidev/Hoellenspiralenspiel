using Godot;
using Hoellenspiralenspiel.Scripts.UI;

namespace Hoellenspiralenspiel.Scripts.Controllers;

public partial class FogVisibilityController : Node
{
    private          FogOfWar       fogOfWar;
    private          ShaderMaterial shaderMaterial;
    [Export] private string         shaderPath = "res://Shaders/fog_visibility.gdshader";
    private          Sprite2D       sprite;

    public override void _Ready()
    {
        sprite = GetParent<Sprite2D>();

        if (sprite == null)
        {
            GD.PrintErr("FogVisibilityController: Parent is not a Sprite2D!");
            return;
        }

        // Find FogOfWar
        fogOfWar = GetTree().Root.FindChild("FogOfWar", true, false) as FogOfWar;

        if (fogOfWar == null)
        {
            var fogNodes = GetTree().GetNodesInGroup("fog_of_war");

            if (fogNodes.Count > 0)
                fogOfWar = fogNodes[0] as FogOfWar;
        }

        if (fogOfWar == null)
        {
            GD.PrintErr("FogVisibilityController: FogOfWar not found!");
            return;
        }

        // Load and apply shader
        var shader = GD.Load<Shader>(shaderPath);

        if (shader == null)
        {
            GD.PrintErr($"FogVisibilityController: Could not load shader at {shaderPath}");
            return;
        }

        shaderMaterial        = new ShaderMaterial();
        shaderMaterial.Shader = shader;
        sprite.Material       = shaderMaterial;

        // Initialize fog data immediately
        UpdateFogData(fogOfWar.GetFogTexture(), fogOfWar.GetFogOffset(), fogOfWar.GetFogScale());
    }

    public override void _Process(double delta)
    {
        if (shaderMaterial == null || sprite == null || fogOfWar == null) return;

        // Update sprite position every frame
        shaderMaterial.SetShaderParameter("sprite_global_position", sprite.GlobalPosition);
    }

    public void UpdateFogData(Texture2D fogTexture, Vector2 fogOffset, int fogScale)
    {
        if (shaderMaterial == null) return;

        shaderMaterial.SetShaderParameter("fog_texture", fogTexture);
        shaderMaterial.SetShaderParameter("fog_offset", fogOffset);
        shaderMaterial.SetShaderParameter("fog_scale", (float)fogScale);
    }
}