using Hoellenspiralenspiel.Scripts.Core.Levels.Fields;
using NUnit.Framework;

namespace Hoellenspiralenspiel.Tests.Levels.Fields;

[TestFixture]
public class FieldPictureTests
{
    [Test]
    [Explicit("Druckt Flächen als Textbild zum Ansehen")]
    public void Flaechen_Drucken()
    {
        TestContext.Out.WriteLine("Legende: Abgrund leer, Boden \".\", Hindernis \"#\", Eingang \"S\", Ausgang \"X\", Gruppe \"!\", Ruine \"R\", Dungeon-Eingang \"D\", Event \"E\", Arena \"A\", Zellen mit Tür klein");

        foreach (var seed in new[] { 1, 2, 3 })
            Print($"Fläche mit Seed {seed}", FieldGenerator.Generate(TestFields.All, new FieldSettings { EventRoomId = seed == 2 ? TestFields.RitualSite.Id : string.Empty }, seed));

        Print("Letzte Fläche mit Seed 4", FieldGenerator.Generate(TestFields.All, new FieldSettings { IsLastField = true }, 4));
    }

    private static void Print(string title, FieldLayout field)
    {
        TestContext.Out.WriteLine();
        TestContext.Out.WriteLine(title);
        TestContext.Out.WriteLine();
        TestContext.Out.Write(TestFields.Describe(field));
    }
}
