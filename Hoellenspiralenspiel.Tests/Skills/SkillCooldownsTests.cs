using System.Collections.Generic;
using Hoellenspiralenspiel.Scripts.Core.Skills;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Skills;

[TestFixture]
public class SkillCooldownsTests
{
    private const string Fireball  = "fireball";
    private const string FrostNova = "frost_nova";

    [Test]
    public void OhneAbklingzeit_IstJederSkillBereit()
    {
        var cooldowns = new SkillCooldowns();

        Assert.Multiple(() =>
        {
            Assert.That(cooldowns.IsReady(Fireball), Is.True);
            Assert.That(cooldowns.GetRemainingSec(Fireball), Is.Zero);
            Assert.That(cooldowns.HasAny, Is.False);
        });
    }

    [Test]
    public void Start_SperrtNurDenEinenSkill()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Fireball, 2);

        Assert.Multiple(() =>
        {
            Assert.That(cooldowns.IsReady(Fireball), Is.False);
            Assert.That(cooldowns.IsReady(FrostNova), Is.True);
            Assert.That(cooldowns.GetRemainingSec(Fireball), Is.EqualTo(2));
            Assert.That(cooldowns.GetTotalSec(Fireball), Is.EqualTo(2));
        });
    }

    [Test]
    public void Advance_ZaehltHerunterUndGibtDenSkillWiederFrei()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Fireball, 2);
        cooldowns.Advance(1.5);

        Assert.That(cooldowns.GetRemainingSec(Fireball), Is.EqualTo(0.5).Within(0.0001));
        Assert.That(cooldowns.IsReady(Fireball), Is.False);

        cooldowns.Advance(0.5);

        Assert.Multiple(() =>
        {
            Assert.That(cooldowns.IsReady(Fireball), Is.True);
            Assert.That(cooldowns.HasAny, Is.False);
        });
    }

    [Test]
    public void Advance_LaesstMehrereAbklingzeitenUnabhaengigLaufen()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Fireball, 1);
        cooldowns.Start(FrostNova, 3);
        cooldowns.Advance(2);

        Assert.Multiple(() =>
        {
            Assert.That(cooldowns.IsReady(Fireball), Is.True);
            Assert.That(cooldowns.IsReady(FrostNova), Is.False);
            Assert.That(cooldowns.GetRemainingSec(FrostNova), Is.EqualTo(1).Within(0.0001));
        });
    }

    [Test]
    public void Start_OhneDauer_SperrtNicht()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Fireball, 0);

        Assert.That(cooldowns.IsReady(Fireball), Is.True);
    }

    [Test]
    public void Start_WaehrendDerAbklingzeit_BeginntVonVorn()
    {
        var cooldowns = new SkillCooldowns();

        cooldowns.Start(Fireball, 2);
        cooldowns.Advance(1.5);
        cooldowns.Start(Fireball, 2);

        Assert.That(cooldowns.GetRemainingSec(Fireball), Is.EqualTo(2));
    }

    [Test]
    public void Ereignisse_MeldenBeginnUndEnde()
    {
        var cooldowns = new SkillCooldowns();
        var started   = new List<string>();
        var finished  = new List<string>();

        cooldowns.Started  += started.Add;
        cooldowns.Finished += finished.Add;

        cooldowns.Start(Fireball, 1);
        cooldowns.Advance(0.5);

        Assert.That(finished, Is.Empty);

        cooldowns.Advance(0.5);

        Assert.Multiple(() =>
        {
            Assert.That(started, Is.EqualTo(new[] { Fireball }));
            Assert.That(finished, Is.EqualTo(new[] { Fireball }));
        });
    }

    [Test]
    public void Clear_GibtAlleSkillsFrei()
    {
        var cooldowns = new SkillCooldowns();
        var finished  = new List<string>();

        cooldowns.Start(Fireball, 5);
        cooldowns.Start(FrostNova, 5);

        cooldowns.Finished += finished.Add;
        cooldowns.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(cooldowns.HasAny, Is.False);
            Assert.That(finished, Is.EquivalentTo(new[] { Fireball, FrostNova }));
        });
    }
}
