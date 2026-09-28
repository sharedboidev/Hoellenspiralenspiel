using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Items.Weapons;

namespace Hoellenspiralenspiel.Resources.Skills;

//Ein Skill als Daten. Ein neuer Skill besteht aus einer Resource dieser Art und bei Bedarf einer Szene für seine Wirkung
[GlobalClass]
public abstract partial class SkillResource : Resource
{
    private SkillDefinition definition;

    //Eindeutiger Schlüssel. Die Belegung der Leiste merkt sich den Skill darüber
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
    public SkillDelivery Delivery { get; set; }

    //Die Szene des Projektils oder der Fläche. Bei Delivery Weapon bleibt sie leer
    [Export]
    public PackedScene EffectScene { get; set; }

    [ExportGroup("Projectile")]
    [Export]
    public float ProjectileSpeed { get; set; } = 800f;

    [Export]
    public float ProjectileLifetimeSec { get; set; } = 2f;

    //So viele neue Projektile entstehen bei einem Treffer
    [Export]
    public int ForkCount { get; set; }

    //So oft wiederholt sich das Aufspalten
    [Export]
    public int ForkGenerations { get; set; }

    [Export]
    public float ForkRange { get; set; } = 600f;

    [ExportGroup("Area")]
    [Export]
    public float AreaRadius { get; set; } = 300f;

    //Zeit, in der die Fläche auf ihren Radius wächst. Bei 0 trifft sie alle im selben Augenblick
    [Export]
    public float AreaExpansionSec { get; set; }

    //Zeit zwischen dem Auslösen und dem Beginn der Wirkung
    [Export]
    public float AreaDelaySec { get; set; }

    //Bleibt leer, bis entschieden ist, wie der Held Skills bekommt
    [ExportGroup("Requirements")]
    [Export]
    public Dictionary<Requirement, int> Requirements { get; set; } = new();

    public abstract SkillKind Kind { get; }

    //Eine Zeile für Tooltips, die den Schaden des Skills beschreibt
    public abstract string DamageSummary { get; }

    //Der Skill, wie der Kern ihn kennt. Wird beim ersten Zugriff gebaut
    public SkillDefinition Definition => definition ??= CreateBaseDefinition() with
    {
        ManaCost = ManaCost,
        CooldownSec = CooldownSec,
        Delivery = Delivery,
        Projectile = Delivery == SkillDelivery.Projectile ? new ProjectileSettings(ProjectileSpeed, ProjectileLifetimeSec, ForkCount, ForkGenerations, ForkRange) : null,
        Area = Delivery is SkillDelivery.AreaAroundCaster or SkillDelivery.AreaAtPoint ? new AreaSettings(AreaRadius, AreaExpansionSec, AreaDelaySec) : null
    };

    public string NameOrId => string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;

    //Name, Art, Kosten und Beschreibung als Text für Tooltips
    public string GetTooltip()
    {
        var lines = new System.Text.StringBuilder();

        lines.AppendLine(NameOrId);
        lines.AppendLine(Kind == SkillKind.Attack ? "Attack" : "Spell");
        lines.AppendLine(DamageSummary);

        if (ManaCost > 0)
            lines.AppendLine($"Mana: {ManaCost:0.##}");

        if (CooldownSec > 0)
            lines.AppendLine($"Cooldown: {CooldownSec:0.##} s");

        if (!string.IsNullOrWhiteSpace(Description))
            lines.AppendLine().AppendLine(Description);

        return lines.ToString().TrimEnd();
    }

    protected abstract SkillDefinition CreateBaseDefinition();
}
