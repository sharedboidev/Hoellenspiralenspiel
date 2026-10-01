using System.Collections.Generic;
using System.Linq;
using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Rng;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class RoomPickerTests
{
    private static readonly RoomBlueprint Boss = TestRooms.Create("boss", 6, 6, RoomRole.Boss);

    private static readonly IReadOnlyList<RoomBlueprint> WithBoss = [.. TestRooms.All, Boss];

    private static LevelSettings Settings(bool isLastLevel, int roomCount = 8)
        => new() { RoomCount = roomCount, IsLastLevel = isLastLevel };

    [Test]
    public void AufDerLetztenEbeneStehtDerBossRaumAmEnde()
    {
        var picked = RoomPicker.Pick(WithBoss, Settings(true), new SeededRandom(1));

        Assert.That(picked[^1], Is.EqualTo(Boss));
        Assert.That(picked[0], Is.EqualTo(TestRooms.Start));
        Assert.That(picked.Contains(TestRooms.Exit), Is.False);
    }

    [Test]
    public void VorDerLetztenEbeneBleibtEsBeimAusgang()
    {
        var picked = RoomPicker.Pick(WithBoss, Settings(false), new SeededRandom(1));

        Assert.That(picked[^1], Is.EqualTo(TestRooms.Exit));
        Assert.That(picked.Contains(Boss), Is.False);
    }

    [Test]
    public void OhneBossRaumNimmtAuchDieLetzteEbeneDenAusgang()
    {
        var picked = RoomPicker.Pick(TestRooms.All, Settings(true), new SeededRandom(1));

        Assert.That(picked[^1], Is.EqualTo(TestRooms.Exit));
    }

    [Test]
    public void DerBossRaumIstNieEinFuellraum()
    {
        for (var seed = 1; seed <= 30; seed++)
        {
            var picked = RoomPicker.Pick(WithBoss, Settings(false, 14), new SeededRandom(seed));

            Assert.That(picked.Count(room => room == Boss), Is.Zero, $"Seed {seed}");
            Assert.That(picked.Count(room => room.Role == RoomRole.Exit), Is.EqualTo(1), $"Seed {seed}");
        }
    }

    [Test]
    public void EineEbeneMitBossRaumLaesstSichErzeugenUndDerBossRaumIstIhrAusgang()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            var layout = LevelGenerator.Generate(WithBoss, Settings(true, 10), seed);
            var exit   = layout.Rooms[layout.ExitRoom];

            Assert.That(exit.Blueprint, Is.EqualTo(Boss), $"Seed {seed}");
            Assert.That(exit.Rect.Width, Is.EqualTo(6), $"Seed {seed}");
            Assert.That(exit.Rect.Height, Is.EqualTo(6), $"Seed {seed}");
            Assert.That(layout.Rooms.Count(room => room.Blueprint == Boss), Is.EqualTo(1), $"Seed {seed}");
        }
    }
}
