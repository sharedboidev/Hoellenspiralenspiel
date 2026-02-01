using Godot;

public enum RessourceType
{
	Life = 0,
	Mana = 1
}

public partial class RessourceOrb : Control
{
	private         float          current;
	private         Color          lifeColor    = new(0.65f, 0.08f, 0.10f);
	private         Color          manaColor    = new(0.10f, 0.30f, 0.85f);
	[Export] public float          maxRessource = 100f;
	private         ShaderMaterial orbShader;
	[Export] public TextureRect    OrbTexture;
	private         RessourceType  type;

	public override void _Ready()
	{
		current = maxRessource;
	}

	public void Init(float max, RessourceType type)
	{
		var original = OrbTexture.Material as ShaderMaterial;

		orbShader        = new ShaderMaterial();
		orbShader.Shader = original.Shader;

		OrbTexture.Material = orbShader;

		current   = maxRessource = max;
		this.type = type;

		ApplyColor();
		SetRessource(current);
	}

	private void ApplyColor()
	{
		if (orbShader is null)
			GD.Print("orbShader is null");
		var c = type == RessourceType.Life ? lifeColor : manaColor;
		orbShader.SetShaderParameter("liquid_color", c);
	}

	public void SetRessource(float c)
	{
		current = Mathf.Clamp(c, 0f, maxRessource);
		var fillAmount = current / maxRessource;

		orbShader.SetShaderParameter("fill_amount", fillAmount);
	}
}
