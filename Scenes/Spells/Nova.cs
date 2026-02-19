using Godot;
using System;

public partial class Nova : Node2D
{ 
	
	 [Export] public float MaxRadius = 300f;
	[Export] public float ExpansionSpeed = 900f;
	[Export] public float SlowDuration = 2f;
	[Export] public float RingThickness = 40f;

	private float _currentRadius = 0f;
	private float _previousRadius = 0f;

	private ShaderMaterial _material;

	public override void _Ready()
	{
		_material = (ShaderMaterial)GetNode<Sprite2D>("Sprite2D").Material;
		_material.SetShaderParameter("max_radius", MaxRadius);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		_previousRadius = _currentRadius;
		_currentRadius += ExpansionSpeed * dt;

		UpdateVisual();

		if (_currentRadius >= MaxRadius)
			QueueFree();
	}

	private void UpdateVisual()
	{
		_material.SetShaderParameter("outer_radius", _currentRadius);
		_material.SetShaderParameter("inner_radius", _currentRadius - RingThickness);
	}
}
