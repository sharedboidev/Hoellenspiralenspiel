using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.Environment;

//So wie's implementiert ist, killts die Performance komplett :C
public partial class Fog : Sprite2D
{
    private          Image        fogImage;
    private          ImageTexture fogTexture;
    [Export] private int          gridSize = 16;
    [Export] private TileMapLayer groundTiles;
    private          Image        lightImage;
    private          Vector2      lightOffset;
    private          Texture2D    lightTexture = GD.Load<Texture2D>("res://Textures/Items/Environment/Light.png");
    [Export] private Player2D     player;
    private          int          viewportHeight;
    private          int          viewportWidth;
    private          Vector2I     worldpositionOffset;

    public override void _Ready()
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        viewportWidth  = (int)viewportSize.X;
        viewportHeight = (int)viewportSize.Y;

        fogImage    = new Image();
        fogTexture  = new ImageTexture();
        lightImage  = lightTexture.GetImage();
        lightOffset = new Vector2(lightTexture.GetWidth() / 2f, lightTexture.GetHeight() / 2f);

        var worldDimension = groundTiles is null ? new Vector2I(viewportWidth, viewportHeight) : groundTiles.GetUsedRect().Size * groundTiles.TileSet.TileSize / 5;
        // var worldPosition       = groundTiles is null ? new Vector2I(viewportWidth, viewportHeight) : groundTiles.GetUsedRect().Position * groundTiles.TileSet.TileSize / 5;

        //worldpositionOffset = worldPosition / gridSize;
        fogImage = Image.CreateEmpty(worldDimension.X, worldDimension.Y, false, Image.Format.Rgbah);
        fogImage.Fill(Colors.Black);
        fogTexture = ImageTexture.CreateFromImage(fogImage);

        lightImage.Convert(Image.Format.Rgbah);

        Scale    *= gridSize;
        Position -= new Vector2I(1000, 1000);
    }

    public override void _Process(double delta) => UpdateFog(player.Position / gridSize);

    public void UpdateFog(Vector2 newGridPosition)
    {
        var lightRect = new Rect2I(Vector2I.Zero, new Vector2I(lightImage.GetWidth(), lightImage.GetHeight()));

        fogImage.BlendRect(lightImage, lightRect, (Vector2I)(newGridPosition - lightOffset));

        UpdateFogImageTexture();
    }

    private void UpdateFogImageTexture()
    {
        var newFogTexture = ImageTexture.CreateFromImage(fogImage);
        Texture = newFogTexture;
    }
}