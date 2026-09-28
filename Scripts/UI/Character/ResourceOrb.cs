using System.ComponentModel;
using Godot;
using Hoellenspiralenspiel.Scripts.Units;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

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
	private         int            shownCurrent    = int.MinValue;
	private         float          shownFillAmount = float.NaN;
	private         int            shownMaximum    = int.MinValue;
	private         ResourceType   type;

	public override void _Ready()
		=> current = MaxRessource;

	public void Init(Player2D adherentPlayer, ResourceType resourceType)
	{
		player = adherentPlayer;
		type   = resourceType;

		ConfigureOrbColors();
		SetPositionInViewport(resourceType);

		player.PropertyChanged += PlayerOnPropertyChanged;
		player.StatsChanged    += Refresh;

		ApplyColor();
		Refresh();
	}

	private void Refresh()
	{
		MaxRessource = type == ResourceType.Life ? player.LifeMaximum : player.ManaMaximum;

		SetRessource(type == ResourceType.Life ? player.LifeCurrent : player.ManaCurrent);
	}

	private void SetPositionInViewport(ResourceType resourceTypetype)
	{
		var viewportSize   = GetViewportRect().Size;
		var viewportWidth  = viewportSize.X;
		var viewportHeight = viewportSize.Y;
		var offsetPx       = 64;

		var orbPosition = resourceTypetype switch
		{
			ResourceType.Life => new Vector2(viewportWidth / 4 - Size.X / 2, viewportHeight - Size.Y - offsetPx),
			ResourceType.Mana => new Vector2(viewportWidth * 3 / 4 - Size.X / 2, viewportHeight - Size.Y - offsetPx),
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

	public override void _ExitTree()
	{
		if (player is null)
			return;

		player.PropertyChanged -= PlayerOnPropertyChanged;
		player.StatsChanged    -= Refresh;
	}

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

		var fillAmount = MaxRessource > 0 ? current / MaxRessource : 0f;

		if (!fillAmount.Equals(shownFillAmount))
		{
			shownFillAmount = fillAmount;

			orbShader.SetShaderParameter("fill_amount", fillAmount);
		}

		var currentToShow = (int)current;
		var maximumToShow = (int)MaxRessource;

		if (currentToShow == shownCurrent && maximumToShow == shownMaximum)
			return;

		shownCurrent = currentToShow;
		shownMaximum = maximumToShow;

		ResourceText.Text = $"{currentToShow} / {maximumToShow}";
	}
}
