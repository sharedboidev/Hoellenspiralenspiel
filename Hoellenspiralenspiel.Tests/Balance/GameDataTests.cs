using System.Linq;
using Hoellenspiralenspiel.Enums;
using Hoellenspiralenspiel.Scripts.Core.Combat;
using Hoellenspiralenspiel.Scripts.Core.Enemies;
using Hoellenspiralenspiel.Scripts.Core.Items;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using Hoellenspiralenspiel.Scripts.Core.Stats;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Balance;

[TestFixture]
public class GameDataTests
{
    [Test]
    public void JederGegnerImOrdner_LaesstSichLesen()
    {
        var enemies = GameData.AllEnemies();

        Assert.Multiple(() =>
        {
            Assert.That(enemies, Is.Not.Empty);
            Assert.That(enemies.Select(enemy => enemy.Id), Is.All.Not.Empty);
            Assert.That(enemies.Select(enemy => enemy.Id), Is.Unique);
        });
    }

    //Die Leben las die Laufzeitprüfung der Etappe 2 von M8 an gespawnten Gegnern im Spiel ab
    [TestCase("skeleton", 1, "", 50)]
    [TestCase("skeleton", 6, "", 71)]
    [TestCase("blue_blob", 1, "", 9)]
    [TestCase("yellow_blob", 3, "stalwart", 136)]
    [TestCase("test_enemy", 5, "", 278)]
    [TestCase("skeleton_king", 7, null, 536)]
    public void GeleseneGegner_HabenDasLebenAusDemSpiel(string id, int level, string modId, int expectedLife)
    {
        var enemy = GameData.Enemy(id);
        var mods  = modId is null ? null : modId.Length == 0 ? [] : new[] { GameData.PoolMod(modId) };

        Assert.That(Fighter.Enemy(enemy, level, mods).CreateStats().GetFinalWhole(CombatStat.Life), Is.EqualTo(expectedLife));
    }

    [Test]
    public void Testgegner_SpucktFeuerAlsProjektil()
    {
        var enemy = GameData.Enemy("test_enemy");
        var spit  = enemy.Skills.Single();

        Assert.Multiple(() =>
        {
            Assert.That(spit.Id, Is.EqualTo("fire_spit"));
            Assert.That(spit.Spell, Is.EqualTo(new SpellDefinition("Fire Spit", 3, 6, DamageType.Fire, 5)));
            Assert.That(spit.Delivery, Is.EqualTo(SkillDelivery.Projectile));
            Assert.That(enemy.UsesProjectiles, Is.True);
        });
    }

    [Test]
    public void SkeletonKing_IstBossMitDreiFestenMods_UndStalwartGibt60ProzentMehrLeben()
    {
        var king = GameData.Enemy("skeleton_king");

        Assert.Multiple(() =>
        {
            Assert.That(king.IsBoss, Is.True);
            Assert.That(king.FixedMods.Select(mod => mod.Id), Is.EqualTo(new[] { "stalwart", "royal_brood", "berserk" }));
            Assert.That(king.FixedMods[0].Modifiers.Single(), Is.EqualTo(new CombatStatModifier(CombatStat.Life, ModificationType.More, 0.6f)));
            Assert.That(king.Behaviour, Is.EqualTo(new EnemyBehaviour { AggroRange = 600f, ChaseTimeSec = 10 }));
        });
    }

    //Die Werte hat der User am 05.10.2026 vorgegeben
    [Test]
    public void MagmaStrike_WandeltInFeuer_UndWirftDreiKugeln()
    {
        var magma = GameData.PlayerSkill("magma_strike");

        Assert.Multiple(() =>
        {
            Assert.That(magma.Name, Is.EqualTo("Magma Strike"));
            Assert.That(magma.Delivery, Is.EqualTo(SkillDelivery.MeleeStrike));
            Assert.That(magma.NeedsMeleeWeapon, Is.True);
            Assert.That(magma.Attack, Is.EqualTo(new AttackDefinition("Magma Strike", 80f, DamageType.Fire)));
            Assert.That(magma.ManaCost, Is.EqualTo(2f));
            Assert.That(magma.CooldownSec, Is.Zero);
            Assert.That(magma.Scatter, Is.EqualTo(new ScatterSettings(3, 65f, 75f, 0.6f)));
        });
    }

    //Die Werte hat der User am 07.10.2026 vorgegeben
    [Test]
    public void DarkenSky_BrauchtEinenBogen_UndLaesstFuenfPfeileFallen()
    {
        var sky = GameData.PlayerSkill("darken_sky");

        Assert.Multiple(() =>
        {
            Assert.That(sky.Name, Is.EqualTo("Darken Sky"));
            Assert.That(sky.Delivery, Is.EqualTo(SkillDelivery.ArrowRain));
            Assert.That(sky.NeedsBow, Is.True);
            Assert.That(sky.NeedsMeleeWeapon, Is.False);
            Assert.That(sky.Attack, Is.EqualTo(new AttackDefinition("Darken Sky", 80f)));
            Assert.That(sky.ManaCost, Is.EqualTo(3f));
            Assert.That(sky.CooldownSec, Is.EqualTo(0));
            Assert.That(sky.Rain, Is.EqualTo(new RainSettings(5, 200f, 75f, 0.5f, 1f)));
            Assert.That(sky.Scatter, Is.Null);
        });
    }

    //Die Werte hat der User am 07.10.2026 vorgegeben, die Rate hat er nach dem Spielen im Editor von 20 auf 50 angehoben. Der Waffenschaden der Resource gilt bei voller Ladung
    [Test]
    public void ChargedShot_BrauchtEinenBogen_UndLaedtFuenfzigProzentJeSekunde()
    {
        var shot = GameData.PlayerSkill("charged_shot");

        Assert.Multiple(() =>
        {
            Assert.That(shot.Name, Is.EqualTo("Charged Shot"));
            Assert.That(shot.Delivery, Is.EqualTo(SkillDelivery.ChargedShot));
            Assert.That(shot.NeedsBow, Is.True);
            Assert.That(shot.IsCharged, Is.True);
            Assert.That(shot.Attack, Is.EqualTo(new AttackDefinition("Charged Shot", 300f) { IgnoresPierceHitPenalty = true }), "trifft immer, der Malus von Pierce gilt nicht");
            Assert.That(shot.ManaCost, Is.EqualTo(2f));
            Assert.That(shot.CooldownSec, Is.Zero, "die Abklingzeit kommt nur nach dem Verpuffen");
            Assert.That(shot.Charge, Is.EqualTo(new ChargeSettings(50f, 100f / 3f, 150f, 0.5f, 5)));
            Assert.That(GameData.PlayerSkill("darken_sky").Charge, Is.Null, "die Rate gehört nur dem geladenen Schuss");
            Assert.That(shot.Charge.GetAttack(shot.Attack, 100f / 3f).WeaponDamagePercent, Is.EqualTo(100f).Within(0.001f));
            Assert.That(shot.Rain, Is.Null);
            Assert.That(shot.Scatter, Is.Null);
        });
    }

    //Das Profil las die Laufzeitprüfung am Helden ab, der das Schwert trug
    [Test]
    public void Trainingsschwert_HatDasProfilAusDemSpiel_UndPariert()
    {
        var sword = GameData.Weapon("training_sword");

        Assert.Multiple(() =>
        {
            Assert.That(new ItemInstance(sword).ToWeaponProfile(), Is.EqualTo(new WeaponProfile(4, 9, 1.4f, 5, DamageType.Slash, 100, false, 1400)));
            Assert.That(sword.Weapon.WieldStrategy, Is.EqualTo(WieldStrategy.MainHand));
            Assert.That(sword.Guard.MeleeParry, Is.EqualTo(5f));
        });
    }


    //Die Werte hat der User am 07.10.2026 vorgegeben: 60 % Waffenschaden je Tick, 3 Mana je Sekunde, ein Tick je Angriff, 1,25-fache Reichweite.
    //Am selben Tag dazu: zwei Umdrehungen je Tick und Phasing, solange er läuft
    [Test]
    public void Typhoon_BrauchtEineNahkampfwaffe_UndWirbeltMitDreiManaJeSekunde()
    {
        var typhoon = GameData.PlayerSkill("typhoon");

        Assert.Multiple(() =>
        {
            Assert.That(typhoon.Name, Is.EqualTo("Typhoon"));
            Assert.That(typhoon.Delivery, Is.EqualTo(SkillDelivery.WeaponWhirl));
            Assert.That(typhoon.NeedsMeleeWeapon, Is.True);
            Assert.That(typhoon.NeedsBow, Is.False);
            Assert.That(typhoon.IsChanneled, Is.True);
            Assert.That(typhoon.Attack, Is.EqualTo(new AttackDefinition("Typhoon", 60f)));
            Assert.That(typhoon.ManaCost, Is.Zero, "der Beginn kostet nichts, bezahlt wird je Sekunde");
            Assert.That(typhoon.CooldownSec, Is.Zero);
            Assert.That(typhoon.Channel, Is.EqualTo(new ChannelSettings(3f, 1f, 2f, true)), "zwei Umdrehungen je Tick, Phasing solange er läuft");
            Assert.That(typhoon.Sweep, Is.EqualTo(new SweepSettings(360f, 1.25f)));
            Assert.That(GameData.PlayerSkill("cleave").Channel, Is.Null, "nur der Wirbel kanalisiert");
            Assert.That(GameData.PlayerSkill("cleave").Sweep, Is.EqualTo(new SweepSettings(180f, 1.5f)));
            Assert.That(typhoon.Scatter, Is.Null);
            Assert.That(typhoon.Rain, Is.Null);
            Assert.That(typhoon.Charge, Is.Null);
        });
    }
}
