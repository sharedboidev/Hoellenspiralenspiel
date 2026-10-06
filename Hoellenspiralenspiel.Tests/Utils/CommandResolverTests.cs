using Hoellenspiralenspiel.Scripts.Utils;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Utils;

[TestFixture]
public class CommandResolverTests
{
    [Test]
    public void Spawn_LiefertGegnerUndAnzahl()
        => Assert.That(CommandResolver.Resolve("spawn skeleton 3"), Is.EqualTo(new SpawnDefinition("skeleton", 3)));

    [TestCase("  spawn   skeleton  3  ")]
    [TestCase("spawn skeleton 3\n")]
    public void UeberzaehligeLeerzeichen_StoerenNicht(string input)
        => Assert.That(CommandResolver.Resolve(input), Is.EqualTo(new SpawnDefinition("skeleton", 3)));

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void LeereEingabe_IstKeinBefehl(string input)
        => Assert.That(CommandResolver.Resolve(input), Is.Null);

    [TestCase("spawn")]
    [TestCase("spawn skeleton")]
    public void SpawnOhneAngaben_IstUngueltigStattZuWerfen(string input)
        => Assert.That(CommandResolver.Resolve(input), Is.InstanceOf<InvalidCommand>());

    [TestCase("spawn skeleton viele")]
    [TestCase("spawn skeleton 0")]
    [TestCase("spawn skeleton -2")]
    [TestCase("spawn skeleton 2.5")]
    public void SpawnOhneGueltigeAnzahl_IstUngueltigStattZuWerfen(string input)
        => Assert.That(CommandResolver.Resolve(input), Is.InstanceOf<InvalidCommand>());

    [Test]
    public void UnbekannterBefehl_IstUngueltigUndWirdGenannt()
    {
        var result = CommandResolver.Resolve("teleport hub");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<InvalidCommand>());
            Assert.That((result as InvalidCommand)?.Reason, Does.Contain("teleport"));
        });
    }
}
