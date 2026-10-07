using Godot;
using Godot.Collections;
using HashSet = System.Collections.Generic.HashSet<string>;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Resources.Skills;

//Der Inspector zeigt je Gruppe nur die Felder, die zur Lieferung passen. Sonst landet ein Wert leicht in der falschen Resource
[GlobalClass]
public abstract partial class SkillResource : Resource
{
    private static readonly HashSet ProjectileFields = [nameof(ProjectileSpeed), nameof(ProjectileLifetimeSec), nameof(ForkCount), nameof(ForkGenerations), nameof(ForkRange)];
    private static readonly HashSet AreaFields       = [nameof(AreaRadius), nameof(AreaExpansionSec), nameof(AreaDelaySec)];
    private static readonly HashSet SweepFields      = [nameof(SweepArcDegrees), nameof(SweepRangeFactor)];

    private SkillDefinition definition;
    private SkillDelivery   delivery;

    [Export]
    public string Id { get; set; } = string.Empty;

    [Export]
    public string DisplayName { get; set; } = string.Empty;

    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Export]
    public Texture2D Icon { get; set; }

    [Export]
    public float ManaCost { get; set; }

    [Export]
    public double CooldownSec { get; set; }

    [Export]
    public SkillDelivery Delivery
    {
        get => delivery;
        set
        {
            delivery = value;

            NotifyPropertyListChanged();
        }
    }

    [Export]
    public PackedScene EffectScene { get; set; }

    [ExportGroup("Projectile")]
    [Export]
    public float ProjectileSpeed { get; set; } = 800f;

    [Export]
    public float ProjectileLifetimeSec { get; set; } = 2f;

    [Export]
    public int ForkCount { get; set; }

    [Export]
    public int ForkGenerations { get; set; }

    [Export]
    public float ForkRange { get; set; } = 600f;

    [ExportGroup("Area")]
    [Export]
    public float AreaRadius { get; set; } = 300f;

    [Export]
    public float AreaExpansionSec { get; set; }

    [Export]
    public float AreaDelaySec { get; set; }

    [ExportGroup("Sweep")]
    [Export(PropertyHint.Range, "1, 360, 1")]
    public float SweepArcDegrees { get; set; } = 180f;

    //Der Radius des Bogens in Vielfachen der Reichweite der Waffe
    [Export]
    public float SweepRangeFactor { get; set; } = 1f;

    //Bleibt leer, bis entschieden ist, wie der Held Skills bekommt
    [ExportGroup("Requirements")]
    [Export]
    public Dictionary<Requirement, int> Requirements { get; set; } = new();

    public abstract SkillKind Kind { get; }

    public SkillDefinition Definition => definition ??= CreateBaseDefinition() with
    {
        ManaCost = ManaCost,
        CooldownSec = CooldownSec,
        Delivery = Delivery,
        Projectile = Delivery == SkillDelivery.Projectile ? new ProjectileSettings(ProjectileSpeed, ProjectileLifetimeSec, ForkCount, ForkGenerations, ForkRange) : null,
        Area = Delivery is SkillDelivery.AreaAroundCaster or SkillDelivery.AreaAtPoint ? new AreaSettings(AreaRadius, AreaExpansionSec, AreaDelaySec) : null,
        Sweep = Delivery is SkillDelivery.WeaponSweep or SkillDelivery.WeaponWhirl ? new SweepSettings(SweepArcDegrees, SweepRangeFactor) : null
    };

    public string NameOrId => string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;

    protected abstract SkillDefinition CreateBaseDefinition();

    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsString();

        var fits = !ProjectileFields.Contains(name) && !AreaFields.Contains(name) && !SweepFields.Contains(name) ||
                   (ProjectileFields.Contains(name) && Delivery == SkillDelivery.Projectile) ||
                   (AreaFields.Contains(name) && Delivery is SkillDelivery.AreaAroundCaster or SkillDelivery.AreaAtPoint) ||
                   (SweepFields.Contains(name) && Delivery is SkillDelivery.WeaponSweep or SkillDelivery.WeaponWhirl);

        if (!fits)
            HideInInspector(property);
    }

    //Gespeichert wird das Feld weiter, nur der Inspector zeigt es nicht
    protected static void HideInInspector(Dictionary property)
        => property["usage"] = (int)PropertyUsageFlags.NoEditor;
}
