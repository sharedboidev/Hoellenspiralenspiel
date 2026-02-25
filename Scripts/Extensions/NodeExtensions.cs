using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Hoellenspiralenspiel.Scripts.Extensions;

public static class NodeExtensions
{
    public static T[] GetAllChildren<T>(this Node node)
            where T : Node
    {
        var myChildren = node.GetChildren()
                             .Where(mc => mc is T)
                             .Cast<T>()
                             .ToList();
        
        var retVal = new List<T>();

        foreach (var child in myChildren)
        {
            var grandChildren = child.GetAllChildren<T>().ToList();

            retVal.AddRange(grandChildren);
        }

        var myOtherChildren = node.GetChildren()
                                  .Except(myChildren)
                                  .ToList();

        foreach (var otherChild in myOtherChildren)
        {
            var grandChildren = otherChild.GetAllChildren<T>().ToList();

            retVal.AddRange(grandChildren);
        }

        retVal.AddRange(myChildren);

        return retVal.ToArray();
    }
}