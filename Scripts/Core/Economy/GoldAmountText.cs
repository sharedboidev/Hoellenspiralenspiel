using System;
using System.Text;

namespace Hoellenspiralenspiel.Scripts.Core.Economy;

public static class GoldAmountText
{
    public const int MaxDigits = 10;

    //Kürzt erst nach dem Filtern, MaxLength am Feld schnitte eingefügten Text schon davor ab. Zu viel fällt direkt vor dem Caret weg, dort steht das gerade Getippte
    public static (string Text, int Caret) KeepDigits(string text, int caret)
    {
        text ??= string.Empty;

        var before    = 0;
        var total     = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (!char.IsAsciiDigit(text[i]))
                continue;

            total++;

            if (i < caret)
                before++;
        }

        var dropped   = Math.Min(Math.Max(0, total - MaxDigits), before);
        var keptCaret = before - dropped;
        var digits    = new StringBuilder(MaxDigits);
        var index     = 0;

        foreach (var character in text)
        {
            if (!char.IsAsciiDigit(character))
                continue;

            var isDropped = index >= keptCaret && index < before;

            index++;

            if (!isDropped && digits.Length < MaxDigits)
                digits.Append(character);
        }

        return (digits.ToString(), keptCaret);
    }

    public static int Parse(string text)
    {
        var amount = 0L;

        foreach (var character in text ?? string.Empty)
        {
            if (char.IsAsciiDigit(character))
                amount = Math.Min(int.MaxValue, amount * 10 + (character - '0'));
        }

        return (int)amount;
    }
}
