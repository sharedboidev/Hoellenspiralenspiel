using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Saving;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class MainMenu : Control
{
    [Export(PropertyHint.File, "*.tscn")]
    public string GameScenePath { get; set; } = "res://Scenes/game.tscn";

    public CharacterSlotView[] Slots { get; private set; } = [];

    public override void _Ready()
    {
        SaveSlots.UseDirectoryFromCommandLine();
        SaveSlots.AdoptLegacySave();

        Slots = this.GetAllChildren<CharacterSlotView>().OrderBy(slot => slot.Slot).ToArray();

        foreach (var slot in Slots)
        {
            slot.PlayRequested   += Play;
            slot.DeleteRequested += Delete;

            slot.ShowSave(SaveSlots.Read(slot.Slot));
        }

        GetNode<Button>("%QuitButton").Pressed += () => GetTree().Quit();
    }

    public void Play(int slot, string newName)
    {
        SaveSlots.Select(slot, newName);

        GetTree().ChangeSceneToFile(GameScenePath);
    }

    public void Delete(int slot)
    {
        SaveSlots.Delete(slot);

        Slots.FirstOrDefault(view => view.Slot == slot)?.ShowSave(null);
    }
}
