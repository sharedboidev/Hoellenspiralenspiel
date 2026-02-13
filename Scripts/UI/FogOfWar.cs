using Godot;
using Hoellenspiralenspiel.Scripts.Units;
using FogVisibilityController = Hoellenspiralenspiel.Scripts.Controllers.FogVisibilityController;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class FogOfWar : Node2D
{
    private          Image        fogImage;
    [Export] private int          fogScale = 16;
    private          Sprite2D     fogSprite;
    [Export] private TileMapLayer ground;
    private          bool         initialized;
    private          Texture2D    lastFogTexture;
    [Export] private Player2D     player;
    private          Image        visionImage;
    private          Rect2I       visionRect;
    private          Vector2I     worldPosition;

    public override void _Ready()
    {
        fogSprite = GetNode<Sprite2D>("%FogSprite");

        GenerateFog();
        UpdateFog();

        initialized = true;
    }

    public override void _Process(double delta)
    {
        if (player.Velocity != Vector2.Zero)
            UpdateFog();
    }

    public Texture2D GetFogTexture() => fogSprite.Texture;

    public Vector2 GetFogOffset() => worldPosition;

    public int GetFogScale() => fogScale;

    private void UpdateFog()
    {
        var playerPos = (Vector2I)player.GlobalPosition;
        var visionPos = (playerPos - worldPosition) / fogScale;

        var visionSize  = visionImage.GetSize();
        var centeredPos = visionPos - visionSize / 2;

        fogImage.BlendRect(visionImage, visionRect, centeredPos);

        AssignTextureFromImage();

        if (!initialized || fogSprite.Texture == lastFogTexture)
            return;

        UpdateEnemyVisibility();
        lastFogTexture = fogSprite.Texture;
    }

    private void UpdateEnemyVisibility()
    {
        var enemies = GetTree().GetNodesInGroup("enemies");

        foreach (var enemy in enemies)
        {
            if (enemy is { } node)
            {
                var controller = node.FindChild(nameof(FogVisibilityController), true, false) as FogVisibilityController;

                if (controller == null && node.HasNode(nameof(FogVisibilityController)))
                    controller = node.GetNode<FogVisibilityController>(nameof(FogVisibilityController));

                controller?.UpdateFogData(fogSprite.Texture, worldPosition, fogScale);
            }
        }
    }

    private void GenerateFog()
    {
        worldPosition = ground.GetUsedRect().Position * ground.TileSet.TileSize;
        var worldDimension = ground.GetUsedRect().Size * ground.TileSet.TileSize;
        var fogDimension   = worldDimension / fogScale;

        CreateFogImage(fogDimension);
        SetFogTexture();

        visionImage = player.VisionSprite.Texture.GetImage();
        visionImage.Convert(Image.Format.Rgba8);

        SetVisionRect();
    }

    private void SetVisionRect()
    {
        var scaledVisionSize = visionImage.GetSize() / fogScale;
        visionImage.Resize(scaledVisionSize.X, scaledVisionSize.Y);

        visionRect = new Rect2I(Vector2I.Zero, visionImage.GetSize());
    }

    private void SetFogTexture()
    {
        AssignTextureFromImage();

        fogSprite.Scale    = new Vector2(fogScale, fogScale);
        fogSprite.Position = worldPosition;
    }

    private void AssignTextureFromImage()
    {
        var fogTexture = ImageTexture.CreateFromImage(fogImage);
        fogSprite.Texture = fogTexture;
    }

    private void CreateFogImage(Vector2I fogDimension)
    {
        fogImage = Image.CreateEmpty(fogDimension.X, fogDimension.Y, false, Image.Format.Rgba8);
        fogImage.Fill(Colors.Black);
    }
}