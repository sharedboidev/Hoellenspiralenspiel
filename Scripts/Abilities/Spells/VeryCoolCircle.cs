using System;
using Godot;
using Hoellenspiralenspiel.Scripts.Abilities;
using Hoellenspiralenspiel.Scripts.Abilities.Spells;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Units.Enemies;

public partial class VeryCoolCircle
        : Area2D,
          ISpell
{
    private bool                 executed;
    private bool                 isPositionFixed;
    private PackedScene          LightningScene = ResourceLoader.Load<PackedScene>("res://Scenes/Spells/lightning_strike.tscn");
    private LightningStrikeSkill lightningStrikeSkill;
    private LightningStrike      lightningStrikeVfx;

    public void Init(BaseSkill skill,
                     Vector2   globalPlayerPosition,
                     Vector2   globalMousePosition)
    {
        TopLevel             = true;
        GlobalPosition       = globalMousePosition;
        lightningStrikeSkill = (LightningStrikeSkill)skill;
    }

    public override void _Ready()
    {
        // Signale verbinden
        BodyEntered += OnBodyEntered;
        BodyExited  += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (!isPositionFixed)
            GlobalPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();

        if (!Input.IsActionJustPressed("mouse_left") || executed)
            return;

        BeFixed();
        BeCool();
        executed = true;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is BaseEnemy enemy)
            enemy.SetHighlight(true);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is BaseEnemy enemy)
            enemy.SetHighlight(false);
    }

    public void BeFixed()
        => isPositionFixed = true;

    public void BeCool()
    {
        var animation = GetNode<AnimatedSprite2D>("Animation");
        animation.Play();

        animation.AnimationFinished += SpawnCenterLightning;
    }

    public void SpawnCenterLightning()
    {
        if (LightningScene == null)
            return;

        lightningStrikeVfx          =  LightningScene.Instantiate<LightningStrike>();
        lightningStrikeVfx.Started  += (_, _) => DoDamage();
        lightningStrikeVfx.Finished += LightningStrikeVFXOnFinished;

        lightningStrikeVfx.Position    = Vector2.Zero;
        lightningStrikeVfx.GlobalScale = Vector2.One;

        var scale = 8.0f;
        lightningStrikeVfx.Scale       = new Vector2(scale, scale);
        lightningStrikeVfx.GlobalScale = new Vector2(scale, scale);
        AddChild(lightningStrikeVfx);
        lightningStrikeVfx.PlaySound();
    }

    private void DoDamage()
    {
        foreach (var body in GetOverlappingBodies())
        {
            if (body is not BaseEnemy enemy)
                continue;

            var damageResult = lightningStrikeSkill.MakeRealDamage(enemy);
            enemy.LifeCurrent -= (int)damageResult.Value;
            enemy.InstatiateFloatingCombatText(damageResult, GetTree().CurrentScene, new Vector2(0, -60));
        }
    }

    private void LightningStrikeVFXOnFinished(object sender, EventArgs e)
    {
        lightningStrikeVfx.QueueFree();
        lightningStrikeVfx = null;
        QueueFree();
    }

    // public void SpawnIsometricLightnings(int amount)
    // {
    //     var colShape = GetNodeOrNull<CollisionShape2D>("Collision");
    //     var radius   = ((CircleShape2D)colShape.Shape).Radius;
    //
    //     for (var i = 0; i < amount; i++)
    //     {
    //         // Zufalls-Winkel und Radius (Wurzel für Gleichmäßigkeit)
    //         var angle = (float)GD.RandRange(0, Math.PI * 2);
    //         var r     = Mathf.Sqrt(GD.Randf()) * radius;
    //
    //         // 2. DIE FORMEL FÜR DEN BODEN
    //         // Wir nehmen r * cos für die Breite (X)
    //         // Wir nehmen r * sin * 0.5f für die Tiefe (Y)
    //         var spawnPos = new Vector2(
    //                                    Mathf.Cos(angle) * r,
    //                                    Mathf.Sin(angle) * r * 0.5f
    //                                   );
    //
    //         var strike = LightningScene.Instantiate<Node2D>();
    //
    //         // 3. SEHR WICHTIG: DIE SKALIERUNG
    //         // Falls dein AOE-Circle Node im Editor auf Y skaliert wurde (z.B. 0.5),
    //         // werden die Blitze auch "platt" gedrückt. Das korrigieren wir:
    //         AddChild(strike);
    //         strike.Position = spawnPos;
    //
    //         // Wir setzen die Skalierung des Blitzes zurück, damit er aufrecht steht
    //         strike.GlobalScale = Vector2.One;
    //     }
    // }
}