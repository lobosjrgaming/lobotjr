using System.ComponentModel;
using System.Windows.Media;

namespace LobotJR.Utils
{
    public static class InterfaceUtils
    {
        /// <summary>
        /// Creates a brush that can be used to color an interface element from
        /// a numeric hex value representing a color (e.g. 0xFFFFFF for white).
        /// </summary>
        /// <param name="hex">An RGB hex value from 0x000000 to 0xFFFFFF.</param>
        /// <returns>A brush for applying color to UI elements.</returns>
        public static SolidColorBrush BrushFromHex(int hex)
        {
            var color = Color.FromArgb(
                255,
                (byte)((hex & 0xFF0000) >> 0x10),
                (byte)((hex & 0x00FF00) >> 8),
                (byte)(hex & 0x0000FF));
            return new SolidColorBrush(color);
        }

        /// <summary>
        /// Fires a property change event for any number of properties.
        /// </summary>
        /// <param name="notifier">The object invoking the changes.</param>
        /// <param name="handler">The event handler.</param>
        /// <param name="names">A collection of names of parameters that have
        /// changed.</param>
        public static void FireChangeEvent(INotifyPropertyChanged notifier, PropertyChangedEventHandler handler, params string[] names)
        {
            foreach (string name in names)
            {
                handler?.Invoke(notifier, new PropertyChangedEventArgs(name));
            }
        }
    }
}
