using Godot;

public enum RessourceType
{
	Life = 0,
	Mana = 1
}

public partial class RessourceOrb : Control
{
	private          float          current;
	private          Color          lifeColor    = new(0.65f, 0.08f, 0.10f);
	private          Color          manaColor    = new(0.10f, 0.30f, 0.85f);
	[Export] public  float          maxRessource = 100f;
	private          ShaderMaterial orbShader;
	[Export] private TextureRect    OrbTexture;
	[Export] private RessourceType  Type;

	public override void _Ready()
	{
		current             = maxRessource;
		OrbTexture.Material = (Material)OrbTexture.Material.Duplicate(true);
		orbShader           = OrbTexture.Material as ShaderMaterial;

		ApplyColor();
		SetRessource(current);
	}

	private void ApplyColor()
	{
		var c = Type == RessourceType.Life ? lifeColor : manaColor;
		orbShader.SetShaderParameter("liquid_color", c);
	}

	public void SetRessource(float c)
	{
		current = Mathf.Clamp(c, 0f, maxRessource);
		var fillAmount = current / maxRessource;

		orbShader.SetShaderParameter("fill_amount", fillAmount);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_accept"))
			SetRessource(current - 10);

		if (@event.IsActionPressed("ui_cancel"))
			SetRessource(current + 10);
	}
}
