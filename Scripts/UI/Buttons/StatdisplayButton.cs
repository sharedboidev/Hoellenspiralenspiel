using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Buttons;

public partial class StatdisplayButton : TextureButton
{
    public delegate void PressedEventHandler(bool isToggledOpen);

    private bool      isToggledOpen = true;
    private Texture2D normalButtonTextureClosed  = GD.Load<Texture2D>("res://Textures/UI/Buttons/Mini_arrow_right2.png");
    private Texture2D normalButtonTextureOpen    = GD.Load<Texture2D>("res://Textures/UI/Buttons/Mini_arrow_left2.png");
    private Texture2D pressedButtonTextureClosed = GD.Load<Texture2D>("res://Textures/UI/Buttons/Mini_arrow_right2_t.png");
    private Texture2D pressedButtonTextureOpen   = GD.Load<Texture2D>("res://Textures/UI/Buttons/Mini_arrow_left2_t.png");

    public new event PressedEventHandler Pressed;

    public override void _Ready()
        => SetOpenTextures();

    public override void _Pressed()
    {
        if (isToggledOpen)
            SetClosedTextures();
        else
            SetOpenTextures();

        Pressed?.Invoke(isToggledOpen);
        isToggledOpen = !isToggledOpen;
    }

    private void SetClosedTextures()
    {
        TextureNormal  = normalButtonTextureClosed;
        TexturePressed = pressedButtonTextureClosed;
    }

    private void SetOpenTextures()
    {
        TextureNormal  = normalButtonTextureOpen;
        TexturePressed = pressedButtonTextureOpen;
    }
}