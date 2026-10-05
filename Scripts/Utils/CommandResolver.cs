using System;

namespace Hoellenspiralenspiel.Scripts.Utils;

public record SpawnDefinition(string UnitId, int Amount);

public static class CommandResolver
{
    private const string SpawnCommand = "spawn";

    public static object Resolve(string commandInput)
    {
        var arguments = commandInput.Split(' ');

        var command = arguments[0];

        if (command.Equals(SpawnCommand))
            return ResolveUnitSpawn(arguments);

        return null;
    }

    private static SpawnDefinition ResolveUnitSpawn(string[] commandInput)
    {
        var unitId = commandInput[1];
        var amountString = commandInput[2];

        var amountParsable = int.TryParse(amountString, out var amount);

        if (!amountParsable)
            throw new ArgumentException($"{amountString} is not a number.");

        return new SpawnDefinition(unitId, amount);
    }
}