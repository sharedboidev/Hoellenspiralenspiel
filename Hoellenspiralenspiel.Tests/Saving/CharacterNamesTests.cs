using Hoellenspiralenspiel.Scripts.Core.Saving;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Saving;

[TestFixture]
public class CharacterNamesTests
{
    [TestCase("Dante", "Dante")]
    [TestCase("  Dante  ", "Dante")]
    [TestCase("Dan\tte\n", "Dante")]
    [TestCase("Dante Alighieri", "Dante Alighieri")]
    public void EinNameWirdBereinigt(string input, string expected)
        => Assert.That(CharacterNames.Clean(input), Is.EqualTo(expected));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void OhneNamenGiltDerErsatz(string input)
        => Assert.That(CharacterNames.Clean(input), Is.EqualTo(CharacterNames.Fallback));

    [Test]
    public void EinLangerNameWirdGekuerzt()
    {
        var name = CharacterNames.Clean("Durante di Alighiero degli Alighieri");

        Assert.That(name, Is.EqualTo("Durante di Aligh"));
        Assert.That(name.Length, Is.LessThanOrEqualTo(CharacterNames.MaxLength));
    }

    [Test]
    public void DerNameStehtImSpielstand()
    {
        var save = new SaveGame { Character = new CharacterSave { Name = "Dante", Level = 4 } };

        Assert.That(SaveGameSerializer.TryDeserialize(SaveGameSerializer.Serialize(save), out var read), Is.True);
        Assert.That(read.Character.Name, Is.EqualTo("Dante"));
    }
}
