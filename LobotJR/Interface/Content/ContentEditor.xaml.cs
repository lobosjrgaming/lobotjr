using LobotJR.Data;
using LobotJR.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LobotJR.Interface.Content
{
    /// <summary>
    /// Interaction logic for ContentEditor.xaml
    /// </summary>
    public partial class ContentEditor : Window, INotifyPropertyChanged
    {
        public delegate void ColumnGenerator(IDatabase db);

        private readonly Dictionary<string, List<IContentTable>> Tables = new Dictionary<string, List<IContentTable>>()
        {
/*
            {
                "Dungeon",
                new List<Type>() {
                    typeof(Dungeon),
                    typeof(DungeonMode),
                    typeof(Encounter),
                    typeof(LevelRange),
                    typeof(Loot),
                }
            },
            {
                "Player",
                new List<Type>()
                {
                    typeof(CharacterClass),
                    typeof(Equippables),
                }
            },
            {
                "Item",
                new List<Type>()
                {
                    typeof(Item),
                    typeof(ItemQuality),
                    typeof(ItemSlot),
                    typeof(ItemType),
                }
            },
            {
                "Pet",
                new List<Type>()
                {
                    typeof(Pet),
                    typeof(PetRarity),
                }
            },
*/
            {
                "Fish",
                new List<IContentTable>()
                {
                    new FishTable(),
                    new FishSizeTable(),
                    new FishRarityTable()
                }
            }
        };
        private Dictionary<string, IContentTable> TableMap = new Dictionary<string, IContentTable>();
        private readonly IConnectionManager ConnectionManager;
        private IContentTable CurrentTable;
        private bool IsReverting;
        private TreeViewItem SelectedItem;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasChanges { get; private set; }

        public ContentEditor(IConnectionManager connectionManager)
        {
            InitializeComponent();
            DataContext = this;
            ConnectionManager = connectionManager;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TableMap = Tables.SelectMany(x => x.Value).ToDictionary(x => x.ContentType.Name, x => x);
            TreeViewItem first = null;
            foreach (var control in Tables)
            {
                var parent = new TreeViewItem() { Header = control.Key, IsExpanded = true };
                CategoryView.Items.Add(parent);
                foreach (var item in control.Value)
                {
                    var leaf = new TreeViewItem() { Header = item.ContentType.Name };
                    parent.Items.Add(leaf);
                    if (first == null)
                    {
                        first = leaf;
                    }
                }
            }
            first.IsSelected = true;
        }

        private async void CategoryView_PreviewSelectedItemChanged(object sender, PreviewRoutedPropertyChangedEventArgs<object> e)
        {
            if (HasChanges)
            {
                var result = MessageBox.Show("Save changes to current table?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await SaveChanges();
                    CleanForm();
                }
                else if (result == MessageBoxResult.No)
                {
                    CleanForm();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            if (!CategoryView.SelectedItem.Equals(SelectedItem))
            {
                SelectedItem = CategoryView.SelectedItem as TreeViewItem;
                if (SelectedItem != null)
                {
                    if (TableMap.TryGetValue(SelectedItem.Header.ToString(), out var table))
                    {
                        CurrentTable = table;
                        using (var db = await ConnectionManager.OpenConnection())
                        {
                            EditorTable.Columns.Clear();
                            var columns = CurrentTable.CreateColumns(db);
                            foreach (var column in columns)
                            {
                                EditorTable.Columns.Add(column);
                            }
                            var source = new ObservableCollection<TableObject>(CurrentTable.GetSource(db));
                            source.CollectionChanged += Source_CollectionChanged;
                            EditorTable.ItemsSource = source;
                        }
                    }
                }
            }
        }

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DirtyForm();
        }

        private void DirtyForm()
        {
            Title = Title.Replace("*", "") + "*";
            HasChanges = true;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(HasChanges));
        }

        private void CleanForm()
        {
            Title = Title.Replace("*", "");
            HasChanges = false;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(HasChanges));
        }

        private async Task SaveChanges()
        {
            if (CurrentTable != null)
            {
                using (var db = await ConnectionManager.OpenConnection())
                {
                    CurrentTable.SaveData(db, EditorTable.ItemsSource as IEnumerable<TableObject>);
                }
            }
            CleanForm();
        }

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            await SaveChanges();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            await SaveChanges();
        }

        private void EditorTable_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DirtyForm();
        }
    }
}
