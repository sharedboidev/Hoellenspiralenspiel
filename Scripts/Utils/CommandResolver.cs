using System;

namespace Hoellenspiralenspiel.Scripts.Utils;

public record SpawnDefinition(string UnitId, int Amount);

public record InvalidCommand(string Reason);

public static class CommandResolver
{
    private const string SpawnCommand = "spawn";
    private const string SpawnUsage   = "spawn <unit_id> <amount>";

    public static object Resolve(string commandInput)
    {
        var arguments = (commandInput ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (arguments.Length == 0)
            return null;

        var command = arguments[0];

        if (command.Equals(SpawnCommand))
            return ResolveUnitSpawn(arguments);

        return new InvalidCommand($"Die Kommandozeile kennt den Befehl \"{command}\" nicht.");
    }

    private static object ResolveUnitSpawn(string[] commandInput)
    {
        if (commandInput.Length < 3)
            return new InvalidCommand($"Dem Befehl fehlen Angaben. Aufruf: {SpawnUsage}");

        var unitId       = commandInput[1];
        var amountString = commandInput[2];

        var amountParsable = int.TryParse(amountString, out var amount);

        if (!amountParsable || amount < 1)
            return new InvalidCommand($"\"{amountString}\" ist keine Anzahl. Aufruf: {SpawnUsage}");

        return new SpawnDefinition(unitId, amount);
    }
}
