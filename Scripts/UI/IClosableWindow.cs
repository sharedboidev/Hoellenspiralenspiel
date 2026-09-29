namespace Hoellenspiralenspiel.Scripts.UI;

//Ein Fenster im Spiel, das Escape und die Leertaste schließen, bevor das Pausenmenü aufgeht
public interface IClosableWindow
{
    bool IsOpen { get; }

    void Close();
}
