using System;
using System.Linq;
using Godot;
using Hoellenspiralenspiel.Scripts.Core.Saving;

namespace Hoellenspiralenspiel.Scripts.UI;

//Einer der drei Plätze im Hauptmenü: leer, belegt oder gerade beim Anlegen eines Charakters
public partial class CharacterSlotView : PanelContainer
{
    private Button   deleteButton;
    private Label    infoLabel;
    private bool     isConfirmingDelete;
    private bool     isNaming;
    private LineEdit nameEdit;
    private Label    nameLabel;
    private Button   playButton;
    private SaveGame save;
    private Label    slotLabel;

    [Export(PropertyHint.Range, "1,3,1")]
    public int Slot { get; set; } = 1;

    public bool IsOccupied => save is not null;

    //Der Name ist nur gesetzt, wenn der Charakter neu entsteht
    public event Action<int, string> PlayRequested;
    public event Action<int>         DeleteRequested;

    public override void _Ready()
    {
        slotLabel    = GetNode<Label>("%SlotLabel");
        nameLabel    = GetNode<Label>("%NameLabel");
        infoLabel    = GetNode<Label>("%InfoLabel");
        nameEdit     = GetNode<LineEdit>("%NameEdit");
        playButton   = GetNode<Button>("%PlayButton");
        deleteButton = GetNode<Button>("%DeleteButton");

        nameEdit.MaxLength = CharacterNames.MaxLength;

        nameEdit.TextSubmitted += _ => OnPlayPressed();
        playButton.Pressed     += OnPlayPressed;
        deleteButton.Pressed   += OnDeletePressed;

        Refresh();
    }

    public void ShowSave(SaveGame shown)
    {
        save               = shown;
        isNaming           = false;
        isConfirmingDelete = false;

        if (IsNodeReady())
            Refresh();
    }

    private void Refresh()
    {
        slotLabel.Text = $"Slot {Slot}";
        nameLabel.Text = IsOccupied ? CharacterNames.Clean(save.Character.Name) : "Empty";
        infoLabel.Text = IsOccupied ? Describe(save) : string.Empty;

        nameLabel.Visible = !isNaming;
        nameEdit.Visible  = isNaming;

        playButton.Text = IsOccupied ? "Play" : isNaming ? "Create" : "New Character";

        deleteButton.Visible = IsOccupied || isNaming;
        deleteButton.Text    = isNaming ? "Cancel" : isConfirmingDelete ? "Really delete?" : "Delete";
    }

    private static string Describe(SaveGame shown)
    {
        var deepest = shown.Journey?.Circles?.Select(circle => circle.DeepestDepth).DefaultIfEmpty(0).Max() ?? shown.Descent?.Depth ?? 0;

        return deepest > 0 ? $"Level {shown.Character.Level} · reached depth {deepest}" : $"Level {shown.Character.Level}";
    }

    private void OnPlayPressed()
    {
        if (IsOccupied)
        {
            PlayRequested?.Invoke(Slot, null);

            return;
        }

        if (isNaming)
        {
            PlayRequested?.Invoke(Slot, CharacterNames.Clean(nameEdit.Text));

            return;
        }

        isNaming = true;

        Refresh();

        nameEdit.GrabFocus();
    }

    //Der erste Klick fragt nach, erst der zweite löscht
    private void OnDeletePressed()
    {
        if (isNaming)
        {
            isNaming      = false;
            nameEdit.Text = string.Empty;

            Refresh();

            return;
        }

        if (!isConfirmingDelete)
        {
            isConfirmingDelete = true;

            Refresh();

            return;
        }

        DeleteRequested?.Invoke(Slot);
    }
}
