using Godot;
using Hoellenspiralenspiel.Resources.Skills;
using Hoellenspiralenspiel.Scripts.Core.Skills;

namespace Hoellenspiralenspiel.Scripts.Skills.Effects;

//Was der Spieler vom Laden sieht: der Pfeil im Bogen und die Aura unter dem Helden. Der Pfeil hängt am Haltepunkt der Waffe
//und hebt sich mit dem Arm, die Aura liegt am Boden. Beides verschwindet, sobald der Schuss losgeht oder verpufft
public sealed class ChargeLook
{
    //Die Sehne liegt so hoch über dem Haltepunkt des Bogens, siehe short_bow.tscn. Der Pfeil liegt dort entlang -Y: Am gehobenen Arm ist das vorn
    private const float StringHeightMeters = 0.055f;
    private const float AuraHeightMeters   = 0.05f;

    private readonly NockedArrow arrow;
    private readonly ChargeAura  aura;

    private ChargeLook(NockedArrow arrow, ChargeAura aura)
    {
        this.arrow = arrow;
        this.aura  = aura;
    }

    //owner trägt die Aura, bowPoint den Pfeil. Fehlt eine Szene oder der Bogen, fehlt nur dieser Teil
    public static ChargeLook Show(AttackSkillResource skill, Node3D owner, Node3D bowPoint)
    {
        NockedArrow arrow = null;
        ChargeAura  aura  = null;

        if (skill?.ChargeArrowScene is { } arrowScene && bowPoint is not null)
        {
            arrow = arrowScene.Instantiate<NockedArrow>();

            arrow.Transform = new Transform3D(new Basis(Vector3.Right, Mathf.DegToRad(-90f)), new Vector3(0f, StringHeightMeters - arrow.NockOffsetMeters, 0f));

            bowPoint.AddChild(arrow);
        }

        if (skill?.ChargeAuraScene is { } auraScene && owner is not null)
        {
            aura = auraScene.Instantiate<ChargeAura>();

            aura.Position = Vector3.Up * AuraHeightMeters;

            owner.AddChild(aura);
        }

        var look = new ChargeLook(arrow, aura);

        look.Update(null, 0f);

        return look;
    }

    public void Update(ChargeSettings charge, float percent)
    {
        var share   = charge?.GetShownShare(percent) ?? 0f;
        var canFire = charge?.CanFire(percent) ?? false;
        var pierces = charge?.Pierces(percent) ?? false;

        if (GodotObject.IsInstanceValid(arrow))
            arrow.Show(share, canFire, pierces);

        if (GodotObject.IsInstanceValid(aura))
            aura.Show(share, canFire);
    }

    public void Dismiss()
    {
        if (GodotObject.IsInstanceValid(arrow))
            arrow.QueueFree();

        if (GodotObject.IsInstanceValid(aura))
            aura.QueueFree();
    }
}
