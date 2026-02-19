using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI;

public enum ResourceType
{
	Life = 0,
	Mana = 1
}

public partial class ResourceOrb : Control
{
	private         float          current;
	private         Color          lifeColor    = new(0.65f, 0.08f, 0.10f);
	private         Color          manaColor    = new(0.10f, 0.30f, 0.85f);
	[Export] public float          MaxRessource = 100f;
	private         ShaderMaterial orbShader;
	[Export] public TextureRect    OrbTexture;
	private         Player2D       player;
	[Export] public Label          ResourceText;
	private         string         resourceTextFormat = "{current} / {max}";
	private         ResourceType   type;

	public override void _Ready()
		=> current = MaxRessource;

	public void Init(Player2D adherentPlayer, ResourceType resourceTypetype)
	{
		player = adherentPlayer;

		ConfigureOrbColors();
		SetRessourceValues(resourceTypetype);
		SetPositionInViewport(resourceTypetype);

		player.PropertyChanged += PlayerOnPropertyChanged;

		ApplyColor();
		SetRessource(current);
	}

	private void SetRessourceValues(ResourceType resourceTypetype)
	{
		type         = resourceTypetype;
		MaxRessource = type == ResourceType.Life ? player.LifeMaximum : player.ManaMaximum;
		current      = type == ResourceType.Life ? player.LifeCurrent : player.ManaCurrent;
	}

	private void SetPositionInViewport(ResourceType resourceTypetype)
	{
		var viewportSize   = GetViewportRect().Size;
		var viewportWidth  = viewportSize.X;
		var viewportHeight = viewportSize.Y;

		var orbPosition = resourceTypetype switch
		{
			ResourceType.Life => new Vector2(viewportWidth / 4 - Size.X / 2, viewportHeight - Size.Y),
			ResourceType.Mana => new Vector2(viewportWidth * 3 / 4 - Size.X / 2, viewportHeight - Size.Y),
			_ => Vector2.Zero
		};

		Position = orbPosition;
	}
	private void ConfigureOrbColors()
	{
		var original = OrbTexture.Material as ShaderMaterial;

		orbShader        = new ShaderMaterial();
		orbShader.Shader = original?.Shader;

		OrbTexture.Material = orbShader;
		OrbTexture.Modulate = Colors.White;
	}

	private void PlayerOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (type)
		{
			case ResourceType.Life when e.PropertyName == nameof(BaseUnit.LifeCurrent):
				SetRessource(player.LifeCurrent);
				break;
			case ResourceType.Mana when e.PropertyName == nameof(Player2D.ManaCurrent):
				SetRessource(player.ManaCurrent);
				break;
		}
	}

	public override void _ExitTree() => player.PropertyChanged -= PlayerOnPropertyChanged;

	private void ApplyColor()
	{
		if (orbShader is null)
			GD.Print("orbShader is null");

		var c = type == ResourceType.Life ? lifeColor : manaColor;
		orbShader?.SetShaderParameter("liquid_color", c);
	}

	public void SetRessource(float newValue)
	{
		current = Mathf.Clamp(newValue, 0f, MaxRessource);
		var fillAmount = current / MaxRessource;

		ResourceText.Text = resourceTextFormat.Replace("{current}", ((int)current).ToString())
											  .Replace("{max}", MaxRessource.ToString());

		orbShader.SetShaderParameter("fill_amount", fillAmount);
	}
}
