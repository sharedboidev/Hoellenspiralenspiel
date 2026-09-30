using Hoellenspiralenspiel.Scripts.Core.Items;

namespace Hoellenspiralenspiel.Scripts.UI;

//Ein Fenster neben dem Charakterbogen, das Items aus dem Inventar annimmt: Truhe oder Händler
public interface ISideWindow : IClosableWindow
{
    //Strg+Klick auf ein Item im Inventar
    bool OfferQuick(ItemInstance item);

    //Steht im Tooltip der Items im Inventar, solange das Fenster offen ist
    string GetPriceNote(ItemInstance item);
}
