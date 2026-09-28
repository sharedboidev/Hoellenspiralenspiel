using System.Collections.Generic;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Utils;

public static class ResourceFiles
{
    public static IEnumerable<string> ListIn(string directory)
    {
        var root = directory.TrimEnd('/');

        foreach (var entry in ResourceLoader.ListDirectory(root))
        {
            var path = $"{root}/{entry.TrimEnd('/')}";

            if (entry.EndsWith('/'))
            {
                foreach (var nestedPath in ListIn(path))
                    yield return nestedPath;
            }
            else if (entry.EndsWith(".tres") || entry.EndsWith(".res"))
                yield return path;
        }
    }
}
