#if TOOLS
using Godot;

namespace Hoellenspiralenspiel.addons.customnodesplugin;

[Tool]
public partial class CustomNodesPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
    }
}
#endif