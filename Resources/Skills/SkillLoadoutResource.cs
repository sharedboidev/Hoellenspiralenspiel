using Godot;
using Godot.Collections;

namespace Hoellenspiralenspiel.Resources.Skills;

//Eine Belegung der Skill-Leiste. Die Stelle in der Liste ist der Platz auf der Leiste, leere Einträge bleiben frei
[GlobalClass]
public partial class SkillLoadoutResource : Resource
{
    [Export]
    public Array<SkillResource> Slots { get; set; } = new();
}
