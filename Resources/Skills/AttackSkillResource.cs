using Godot;
using Godot.Collections;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using HashSet = System.Collections.Generic.HashSet<string>;

namespace Hoellenspiralenspiel.Resources.Skills;

[GlobalClass]
public partial class AttackSkillResource : SkillResource
{
    private static readonly HashSet ScatterFields = [nameof(ScatterCount), nameof(ScatterWeaponDamagePercent), nameof(ScatterImpactRadius), nameof(ScatterFlightSec), nameof(ScatterScene)];
    private static readonly HashSet RainFields    = [nameof(RainCount), nameof(RainRadius), nameof(RainImpactRadius), nameof(RainDelaySec), nameof(RainDurationSec), nameof(RainScene)];

    private static readonly HashSet ChargeFields =
    [
        nameof(ChargeRatePerSec), nameof(ChargeMinPercent), nameof(ChargeMaxPercent), nameof(ChargeOverholdSec), nameof(ChargeOverholdCooldownSec),
        nameof(ChargeArrowScene), nameof(ChargeAuraScene)
    ];

    private static readonly HashSet ChannelFields = [nameof(ChannelManaPerSec), nameof(ChannelTicksPerAttack)];

    [ExportGroup("Attack")]
    [Export]
    public float WeaponDamagePercent { get; set; } = 100f;

    [Export]
    public bool ConvertsDamageType { get; set; }

    [Export]
    public DamageType DealtAs { get; set; }

    //Pierce trifft nur halb so oft. Dieser Angriff trifft trotzdem mit der vollen Chance
    [Export]
    public bool IgnoresPierceHitPenalty { get; set; }

    //Kugeln, die nach einem gelandeten Treffer aus dem Ziel springen. 0 heißt keine
    [ExportGroup("Scatter")]
    [Export]
    public int ScatterCount { get; set; }

    [Export]
    public float ScatterWeaponDamagePercent { get; set; } = 50f;

    //In Pixeln. Die Kugeln landen so nah am Ziel, dass dieser Radius es erreicht
    [Export]
    public float ScatterImpactRadius { get; set; } = 75f;

    [Export]
    public float ScatterFlightSec { get; set; } = 0.6f;

    [Export]
    public PackedScene ScatterScene { get; set; }

    //Pfeile, die bei Delivery ArrowRain nach dem Schuss in den Himmel auf das Zielgebiet fallen. Jeder trifft mit WeaponDamagePercent
    [ExportGroup("Rain")]
    [Export]
    public int RainCount { get; set; } = 5;

    //In Pixeln. Die Pfeile fallen in diesem Umkreis um den Zielpunkt, dichter zur Mitte hin
    [Export]
    public float RainRadius { get; set; } = 200f;

    [Export]
    public float RainImpactRadius { get; set; } = 75f;

    //Vom Schuss bis zum ersten Pfeil, und so lange fallen sie danach
    [Export]
    public float RainDelaySec { get; set; } = 0.5f;

    [Export]
    public float RainDurationSec { get; set; } = 1f;

    //Ein Pfeil des Regens, sein Wurzelknoten ist eine FallingArea. Der Schuss in den Himmel ist EffectScene
    [Export]
    public PackedScene RainScene { get; set; }

    //Die Ladung bei Delivery ChargedShot, in Prozent. WeaponDamagePercent gilt bei 100 % und wächst linear mit der Ladung
    [ExportGroup("Charge")]
    [Export]
    public float ChargeRatePerSec { get; set; } = 20f;

    //Darunter verpufft der Schuss beim Loslassen
    [Export]
    public float ChargeMinPercent { get; set; } = 100f / 3f;

    //Über 100 % durchstößt der Pfeil alle Ziele auf seiner Bahn
    [Export]
    public float ChargeMaxPercent { get; set; } = 150f;

    //Wer das Maximum so lange hält, verschießt nichts und bekommt die Abklingzeit
    [Export]
    public float ChargeOverholdSec { get; set; } = 0.5f;

    [Export]
    public double ChargeOverholdCooldownSec { get; set; } = 5;

    //Während des Ladens: der Pfeil im Bogen (Wurzel NockedArrow) und die Aura unter dem Helden (Wurzel ChargeAura). EffectScene ist das Projektil
    [Export]
    public PackedScene ChargeArrowScene { get; set; }

    [Export]
    public PackedScene ChargeAuraScene { get; set; }

    //Der Wirbel bei Delivery WeaponWhirl: Mana je Sekunde statt je Einsatz, ManaCost bleibt dabei der Preis für den Beginn
    [ExportGroup("Channel")]
    [Export]
    public float ChannelManaPerSec { get; set; } = 3f;

    //Ticks je Angriff des Angriffstempos. Jeder Tick trifft jeden im Kreis mit WeaponDamagePercent
    [Export]
    public float ChannelTicksPerAttack { get; set; } = 1f;

    public override SkillKind Kind => SkillKind.Attack;

    //Kugeln springen nur aus einem Schlag, Pfeile regnen nur nach dem Schuss in den Himmel, geladen wird nur der geladene Schuss, der Wirbel kanalisiert als Einziger
    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);

        var name = property["name"].AsString();

        var fits = !ScatterFields.Contains(name) && !RainFields.Contains(name) && !ChargeFields.Contains(name) && !ChannelFields.Contains(name) ||
                   (ScatterFields.Contains(name) && Delivery is SkillDelivery.Weapon or SkillDelivery.MeleeStrike) ||
                   (RainFields.Contains(name) && Delivery == SkillDelivery.ArrowRain) ||
                   (ChargeFields.Contains(name) && Delivery == SkillDelivery.ChargedShot) ||
                   (ChannelFields.Contains(name) && Delivery == SkillDelivery.WeaponWhirl);

        if (!fits)
            HideInInspector(property);
    }

    protected override SkillDefinition CreateBaseDefinition()
        => SkillDefinition.ForAttack(Id, new AttackDefinition(NameOrId, WeaponDamagePercent, ConvertsDamageType ? DealtAs : null) { IgnoresPierceHitPenalty = IgnoresPierceHitPenalty }) with
        {
            Scatter = ScatterCount > 0 ? new ScatterSettings(ScatterCount, ScatterWeaponDamagePercent, ScatterImpactRadius, ScatterFlightSec) : null,
            Rain    = Delivery == SkillDelivery.ArrowRain ? new RainSettings(RainCount, RainRadius, RainImpactRadius, RainDelaySec, RainDurationSec) : null,
            Charge  = Delivery == SkillDelivery.ChargedShot
                    ? new ChargeSettings(ChargeRatePerSec, ChargeMinPercent, ChargeMaxPercent, ChargeOverholdSec, ChargeOverholdCooldownSec)
                    : null,
            Channel = Delivery == SkillDelivery.WeaponWhirl ? new ChannelSettings(ChannelManaPerSec, ChannelTicksPerAttack) : null
        };
}
