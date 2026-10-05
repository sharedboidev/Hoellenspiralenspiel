namespace Hoellenspiralenspiel.Scripts.Utils;

public static class CommandResolver
{
    private const string SpawnCommand = "spawn";

    public static void Resolve(string commandInput)
    {
        var arguments = commandInput.Split(' ');

        var command = arguments[0];

        if (command.Equals(SpawnCommand))
            SpawnUnit(arguments);
    }

    private static void SpawnUnit(string[] commandInput)
    {
        var unitId       = commandInput[1];
        var amountString = commandInput[2];

        var amountParsable = int.TryParse(amountString, out var amount);
        
        if(!amountParsable)
            return;
        
        
    }
}