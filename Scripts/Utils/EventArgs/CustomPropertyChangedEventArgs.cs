using System.ComponentModel;

namespace Hoellenspiralenspiel.Scripts.Utils.EventArgs;

public class CustomPropertyChangedEventArgs(object oldValue, object newValue, string propertyName)
        : PropertyChangedEventArgs(propertyName)
{
    public object OldValue { get; set; } = oldValue;
    public object NewValue { get; set; } = newValue;
}