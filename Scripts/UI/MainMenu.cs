using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Extensions;
using Hoellenspiralenspiel.Scripts.Saving;

namespace Hoellenspiralenspiel.Scripts.UI;

public partial class MainMenu : Control
{
    private const string LoadingText = "Loading...";

    private bool isStarting;

    [Export(PropertyHint.File, "*.tscn")]
    public string GameScenePath { get; set; } = "res://Scenes/game.tscn";

    [Export]
    public Curtain Curtain { get; set; }

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

    //Das Laden der Spielszene hält das Bild an. Der Vorhang muss deshalb vorher einmal gezeichnet sein
    public async void Play(int slot, string newName)
    {
        if (isStarting)
            return;

        isStarting = true;

        SaveSlots.Select(slot, newName);

        if (Curtain is not null)
        {
            Curtain.Drop(detail: LoadingText);

            await Curtain.WaitUntilShown();

            Curtain.HandOver();
        }

        GetTree().ChangeSceneToFile(GameScenePath);
    }

    public void Delete(int slot)
    {
        SaveSlots.Delete(slot);

        Slots.FirstOrDefault(view => view.Slot == slot)?.ShowSave(null);
    }
}
