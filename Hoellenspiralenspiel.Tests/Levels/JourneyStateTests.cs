using Hoellenspiralenspiel.Scripts.Core.Levels;
using Hoellenspiralenspiel.Scripts.Core.Saving;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels;

[TestFixture]
public class JourneyStateTests
{
    private const string Lust     = "lust";
    private const string Gluttony = "gluttony";

    private static ExplorationMap CreateExplored(params Cell[] cells)
    {
        var map = new ExplorationMap(8, 6);

        foreach (var cell in cells)
            map.Reveal(cell);

        return map;
    }

    private static JourneyState SaveAndLoad(JourneyState journey, string legacyCircleId = Lust)
    {
        var save   = new SaveGame();
        var loaded = new JourneyState();

        SaveGameMapper.CaptureJourney(journey, save);

        Assert.That(SaveGameSerializer.TryDeserialize(SaveGameSerializer.Serialize(save), out var read), Is.True);
        Assert.That(SaveGameMapper.RestoreJourney(read, loaded, legacyCircleId), Is.True);

        return loaded;
    }

    [Test]
    public void JederKreisHatSeinenEigenenAbstieg()
    {
        var journey = new JourneyState();

        journey.BeginAnew(Lust, 11);
        journey.BeginAnew(Gluttony, 22);
        journey.GetDescent(Lust).GoTo(3);

        Assert.That(journey.GetDescent(Lust), Is.SameAs(journey.GetDescent(Lust)));
        Assert.That(journey.GetDescent(Lust).Seed, Is.EqualTo(11));
        Assert.That(journey.GetDescent(Gluttony).Seed, Is.EqualTo(22));
        Assert.That(journey.GetDescent(Gluttony).DeepestDepth, Is.EqualTo(0));
    }

    [Test]
    public void AmAnfangIstNurDerErsteKreisOffen()
    {
        var journey = new JourneyState();

        Assert.That(journey.IsUnlocked(1), Is.True);
        Assert.That(journey.IsUnlocked(2), Is.False);
        Assert.That(journey.IsUnlocked(0), Is.False);
    }

    [Test]
    public void EinOffenerKreisBleibtOffen()
    {
        var journey = new JourneyState();

        journey.Unlock(3);
        journey.Unlock(2);

        Assert.That(journey.UnlockedCircles, Is.EqualTo(3));
        Assert.That(journey.IsUnlocked(3), Is.True);
        Assert.That(journey.IsUnlocked(4), Is.False);
    }

    [Test]
    public void EinNeuesTownPortalErsetztDasAlte()
    {
        var journey = new JourneyState();

        journey.OpenTownPortal(new TownPortalSpot(Lust, 2, 1f, 2f));
        journey.OpenTownPortal(new TownPortalSpot(Gluttony, 1, 3f, 4f));

        Assert.That(journey.TownPortal, Is.EqualTo(new TownPortalSpot(Gluttony, 1, 3f, 4f)));
    }

    [Test]
    public void ImHubLaesstSichKeinTownPortalOeffnen()
    {
        var journey = new JourneyState();

        journey.OpenTownPortal(new TownPortalSpot(Lust, 0, 1f, 2f));
        journey.OpenTownPortal(new TownPortalSpot(string.Empty, 2, 1f, 2f));

        Assert.That(journey.TownPortal, Is.Null);
    }

    [Test]
    public void EinNeuerAbstiegSchliesstDasTownPortalSeinesKreises()
    {
        var journey = new JourneyState();

        journey.BeginAnew(Lust, 1);
        journey.OpenTownPortal(new TownPortalSpot(Lust, 2, 1f, 2f));
        journey.BeginAnew(Gluttony, 2);

        Assert.That(journey.TownPortal, Is.Not.Null);

        journey.BeginAnew(Lust, 3);

        Assert.That(journey.TownPortal, Is.Null);
    }

    [Test]
    public void DieReiseUeberstehtDasSpeichern()
    {
        var journey = new JourneyState();
        var first   = CreateExplored(new Cell(1, 1), new Cell(2, 1));
        var second  = CreateExplored(new Cell(7, 5));

        journey.Unlock(2);
        journey.BeginAnew(Lust, 4711);
        journey.BeginAnew(Gluttony, -5);

        var lust = journey.GetDescent(Lust);

        lust.GoTo(3);
        lust.Remember(1, first);
        lust.Remember(2, second);
        lust.RememberKill(2, 4);
        lust.RememberKill(2, 17);
        lust.RememberKill(3, 0);

        journey.OpenTownPortal(new TownPortalSpot(Lust, 3, 12.5f, -8f));

        var loaded     = SaveAndLoad(journey);
        var loadedLust = loaded.GetDescent(Lust);

        Assert.That(loaded.UnlockedCircles, Is.EqualTo(2));
        Assert.That(loaded.TownPortal, Is.EqualTo(new TownPortalSpot(Lust, 3, 12.5f, -8f)));
        Assert.That(loaded.GetDescent(Gluttony).Seed, Is.EqualTo(-5));
        Assert.That(loaded.GetDescent(Gluttony).HasBegun, Is.True);

        Assert.That(loadedLust.Seed, Is.EqualTo(4711));
        Assert.That(loadedLust.DeepestDepth, Is.EqualTo(3));
        Assert.That(loadedLust.GetRevealed(1), Is.EqualTo(first.Encode()));
        Assert.That(loadedLust.GetRevealed(2), Is.EqualTo(second.Encode()));
        Assert.That(loadedLust.GetRevealed(3), Is.Null);
        Assert.That(loadedLust.GetKilled(2), Is.EqualTo(new[] { 4, 17 }));
        Assert.That(loadedLust.GetKilled(3), Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void NachDemLadenStehtDerHeldImHub()
    {
        var journey = new JourneyState();

        journey.BeginAnew(Lust, 1);
        journey.GetDescent(Lust).GoTo(2);

        var loaded = SaveAndLoad(journey);

        Assert.That(loaded.GetDescent(Lust).Depth, Is.EqualTo(0));
        Assert.That(loaded.GetDescent(Lust).HasReached(2), Is.True);
    }

    [Test]
    public void EinKreisOhneAbstiegStehtNichtImSpielstand()
    {
        var journey = new JourneyState();
        var save    = new SaveGame();

        journey.GetDescent(Lust);

        SaveGameMapper.CaptureJourney(journey, save);

        Assert.That(save.Journey.Circles, Is.Empty);
        Assert.That(save.Journey.TownPortal, Is.Null);
    }

    [Test]
    public void EinSpielstandDerVersion1BringtSeinenAbstiegMit()
    {
        const string json = "{\"Version\":1,\"Character\":{\"Level\":3},\"Descent\":{\"Seed\":5,\"Depth\":3,\"Levels\":[{\"Depth\":2,\"Revealed\":\"AQ==\"}]}}";

        var journey = new JourneyState();

        Assert.That(SaveGameSerializer.TryDeserialize(json, out var read), Is.True);
        Assert.That(SaveGameMapper.RestoreJourney(read, journey, Lust), Is.True);

        var descent = journey.GetDescent(Lust);

        Assert.That(descent.Seed, Is.EqualTo(5));
        Assert.That(descent.Depth, Is.EqualTo(0));
        Assert.That(descent.DeepestDepth, Is.EqualTo(3));
        Assert.That(descent.GetRevealed(2), Is.EqualTo("AQ=="));
        Assert.That(descent.GetKilled(2), Is.Empty);
    }

    [Test]
    public void EinSpielstandOhneAbstiegLaesstDieReiseWieSieIst()
    {
        var journey = new JourneyState();

        journey.BeginAnew(Lust, 9);

        Assert.That(SaveGameSerializer.TryDeserialize("{\"Version\":1,\"Character\":{\"Level\":3}}", out var read), Is.True);
        Assert.That(SaveGameMapper.RestoreJourney(read, journey, Lust), Is.False);
        Assert.That(journey.GetDescent(Lust).Seed, Is.EqualTo(9));
    }

    [Test]
    public void EinNeuerSpielstandTraegtKeinenAltenAbstieg()
    {
        var save = new SaveGame { Descent = new DescentSave { Seed = 1, Depth = 2 } };

        SaveGameMapper.CaptureJourney(new JourneyState(), save);

        Assert.That(save.Descent, Is.Null);
        Assert.That(save.Version, Is.EqualTo(SaveGame.CurrentVersion));
    }
}
