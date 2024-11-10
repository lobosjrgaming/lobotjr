using System.Windows;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    public delegate void PreviewRoutedPropertyChangedEventHandler<T>(object sender, PreviewRoutedPropertyChangedEventArgs<T> e);

    public class PreviewRoutedPropertyChangedEventArgs<T> : RoutedPropertyChangedEventArgs<T>
    {
        public bool Cancel { get; set; }

        public PreviewRoutedPropertyChangedEventArgs(T oldValue, T newValue) : base(oldValue, newValue) { }
        public PreviewRoutedPropertyChangedEventArgs(RoutedPropertyChangedEventArgs<T> inner) : base(inner.OldValue, inner.NewValue, inner.RoutedEvent)
        {
            Source = inner.Source;
        }
    }

    /// <summary>
    /// Interaction logic for CancellableTreeView.xaml
    /// </summary>
    public partial class PreviewTreeView : TreeView
    {

        public event PreviewRoutedPropertyChangedEventHandler<object> PreviewSelectedItemChanged;
        private bool IsReverting = false;

        public PreviewTreeView() : base() { }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsReverting)
            {
                var preview = new PreviewRoutedPropertyChangedEventArgs<object>(e);
                PreviewSelectedItemChanged?.Invoke(this, preview);
                if (!preview.Cancel)
                {
                    base.OnSelectedItemChanged(e);
                }
                else
                {
                    IsReverting = true;
                    (e.OldValue as TreeViewItem).IsSelected = true;
                }
            }
            else
            {
                IsReverting = false;
            }
        }
    }
}
