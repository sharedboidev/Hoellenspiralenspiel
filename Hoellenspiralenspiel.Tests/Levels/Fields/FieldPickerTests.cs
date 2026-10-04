using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Levels.Fields;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels.Fields;

[TestFixture]
public class FieldPickerTests
{
    [Test]
    public void AufDerLetztenFlaeche_StehtDieArenaVorn_DavorNie()
    {
        var last   = FieldPicker.Pick(TestFields.All, new FieldSettings { IsLastField = true }, new SeededRandom(1));
        var before = FieldPicker.Pick(TestFields.All, new FieldSettings(), new SeededRandom(1));

        Assert.Multiple(() =>
        {
            Assert.That(last[0], Is.EqualTo(TestFields.JudgementArena));
            Assert.That(before.Where(blueprint => blueprint.Role == RoomRole.Boss), Is.Empty);
        });
    }

    [Test]
    public void DasGeplanteEvent_KommtAuchUnterSeinemMindestlevel()
    {
        var hidden = TestFields.RitualSite with { MinAreaLevel = 9 };
        var picked = FieldPicker.Pick([hidden, TestFields.Courtyard], new FieldSettings { EventRoomId = hidden.Id, AreaLevel = 1 }, new SeededRandom(1));

        Assert.That(picked.Count(blueprint => blueprint == hidden), Is.EqualTo(1));
    }

    [Test]
    public void OhneRuinenvorlagen_BleibtDieFlaecheOhneRuinen()
    {
        var picked = FieldPicker.Pick([TestFields.Trapdoor], new FieldSettings(), new SeededRandom(1));

        Assert.That(picked, Is.EqualTo(new[] { TestFields.Trapdoor }));
    }
}
