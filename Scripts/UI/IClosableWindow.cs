namespace Hoellenspiralenspiel.Scripts.UI;

//Ein Fenster der Hud, das Escape und die Taste für Close Windows schließen, bevor das Pausenmenü aufgeht.
//Die Einstellungen gehen nur aus einem offenen Menü auf und schließen sich auf Escape selbst
public interface IClosableWindow
{
    bool IsOpen { get; }

    void Close();
}
