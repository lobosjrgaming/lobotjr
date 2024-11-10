using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Data;
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

        /// <summary>
        /// Converts the name of a property in pascal case to something more
        /// akin to standard text. Adds spaces between each word, and
        /// simplifies some common words.
        /// </summary>
        /// <param name="pascalString">The string to convert.</param>
        /// <returns>The converted string.</returns>
        public static string PascalToReadable(string pascalString)
        {
            var pattern = new Regex("([a-z])([A-Z])");
            var matches = pattern.Matches(pascalString);
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                pascalString = pascalString.Substring(0, match.Index + 1) + " " + pascalString.Substring(match.Index + 1);
            }
            return pascalString.Replace("Minimum", "Min").Replace("Maximum", "Max");
        }

        /// <summary>
        /// Creates a data grid text column from a property name.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="isReadOnly">True if the column should be read-only.</param>
        /// <returns>A DataGridColumn that binds the given property.</returns>
        public static DataGridColumn CreateColumn(string propertyName, bool isReadOnly = false)
        {
            return new DataGridTextColumn() { Header = PascalToReadable(propertyName), Binding = new Binding(propertyName), IsReadOnly = isReadOnly };
        }

        /// <summary>
        /// Creates a data grid combo box column from a property name, with
        /// options for the combo box.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="items">A collection of items to use for the combo box.</param>
        /// <returns>A DataGridColumn that binds the given property.</returns>
        public static DataGridColumn CreateColumn(string propertyName, IEnumerable items)
        {
            return new DataGridComboBoxColumn() { Header = PascalToReadable(propertyName), SelectedItemBinding = new Binding(propertyName), ItemsSource = items };
        }
    }
}
