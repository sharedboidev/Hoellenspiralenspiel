using Godot;
using Godot.Collections;

namespace Hoellenspiralenspiel.Resources.Skills;

[GlobalClass]
public partial class SkillLoadoutResource : Resource
{
    [Export]
    public Array<SkillResource> Slots { get; set; } = new();
}
