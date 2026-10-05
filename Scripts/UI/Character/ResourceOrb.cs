using Godot;
using Hoellenspiralenspiel.Interfaces;

namespace Hoellenspiralenspiel.Scripts.UI.Character;

public enum ResourceType
{
	Life = 0,
	Mana = 1
}

public partial class ResourceOrb : Control
{
	private         float          current;
	private         IHero          hero;
	private         Color          lifeColor    = new(0.65f, 0.08f, 0.10f);
	private         Color          manaColor    = new(0.10f, 0.30f, 0.85f);
	[Export] public float          MaxRessource = 100f;
	private         ShaderMaterial orbShader;
	[Export] public TextureRect    OrbTexture;
	[Export] private Node          player;
	[Export] public Label          ResourceText;
	private         int            shownCurrent    = int.MinValue;
	private         float          shownFillAmount = float.NaN;
	private         int            shownMaximum    = int.MinValue;
	private         float          shownPending    = float.NaN;
	[Export] private ResourceType  type;

	public override void _Ready()
	{
		current = MaxRessource;

		if (player is IHero heroOfScene)
			Init(heroOfScene, type);
	}

	public void Init(IHero adherentHero, ResourceType resourceType)
	{
		hero = adherentHero;
		type = resourceType;

		ConfigureOrbColors();

		hero.ResourcesChanged += Refresh;

		ApplyColor();
		Refresh();
	}

	private void Refresh()
	{
		MaxRessource = type == ResourceType.Life ? hero.LifeMaximum : hero.ManaMaximum;

		SetRessource(type == ResourceType.Life ? hero.LifeCurrent : hero.ManaCurrent,
		             type == ResourceType.Life ? hero.LifePending : hero.ManaPending);
	}

	private void ConfigureOrbColors()
	{
		var original = OrbTexture.Material as ShaderMaterial;

		orbShader        = new ShaderMaterial();
		orbShader.Shader = original?.Shader;

		OrbTexture.Material = orbShader;
		OrbTexture.Modulate = Colors.White;
	}

	public override void _ExitTree()
	{
		if (hero is not null)
			hero.ResourcesChanged -= Refresh;
	}

	private void ApplyColor()
	{
		if (orbShader is null)
			GD.Print("orbShader is null");

		var c = type == ResourceType.Life ? lifeColor : manaColor;
		orbShader?.SetShaderParameter("liquid_color", c);
	}

	//pending ist, was ein Leech noch heilt. Es steht halb durchsichtig über dem Stand und füllt sich nach und nach auf
	public void SetRessource(float newValue, float pending = 0f)
	{
		current = Mathf.Clamp(newValue, 0f, MaxRessource);

		var fillAmount    = MaxRessource > 0 ? current / MaxRessource : 0f;
		var pendingAmount = MaxRessource > 0 ? Mathf.Clamp(pending / MaxRessource, 0f, 1f - fillAmount) : 0f;

		if (!fillAmount.Equals(shownFillAmount))
		{
			shownFillAmount = fillAmount;

			orbShader.SetShaderParameter("fill_amount", fillAmount);
		}

		if (!pendingAmount.Equals(shownPending))
		{
			shownPending = pendingAmount;

			orbShader.SetShaderParameter("pending_amount", pendingAmount);
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
