using Godot;

namespace Hoellenspiralenspiel.Scripts.UI.Settings;

//Ein Reiter des Einstellungsfensters
public interface ISettingsTab
{
    //Beim Öffnen des Fensters: zeigt, was gerade gilt
    void ShowCurrent();

    //Beim Schließen: schreibt, was noch offen ist, oder nimmt eine unbestätigte Änderung zurück
    void Commit();

    //Sieht jede Eingabe vor dem Fenster, etwa um eine Taste abzufangen. true heißt, sie ist verbraucht
    bool TakesInput(InputEvent inputEvent);
}
