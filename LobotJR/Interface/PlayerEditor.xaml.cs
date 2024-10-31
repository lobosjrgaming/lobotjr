using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.Model.Equipment;
using LobotJR.Command.Model.Pets;
using LobotJR.Command.Model.Player;
using LobotJR.Data;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace LobotJR.Interface
{
    /// <summary>
    /// Interaction logic for PlayerEditor.xaml
    /// </summary>
    public partial class PlayerEditor : Window, INotifyPropertyChanged
    {
        private readonly IConnectionManager ConnectionManager;
        private readonly UserController UserController;
        private readonly PlayerController PlayerController;
        private readonly EquipmentController EquipmentController;
        private readonly PetController PetController;

        private readonly List<Inventory> ItemsToRemove = new List<Inventory>();
        private readonly List<Stable> PetsToRemove = new List<Stable>();

        public event PropertyChangedEventHandler PropertyChanged;

        public User User { get; private set; }
        public PlayerCharacter Player { get; private set; }

        public bool IsPlayerLoaded { get; private set; }
        public bool IsEditing { get; private set; }
        public bool CanEdit { get { return IsPlayerLoaded && !IsEditing; } }
        public bool IsItemSelected { get; private set; }
        public bool IsPetSelected { get; private set; }

        public PlayerEditor(IConnectionManager connectionManager, UserController userController, PlayerController playerController, EquipmentController equipmentController, PetController petController)
        {
            InitializeComponent();
            ConnectionManager = connectionManager;
            UserController = userController;
            PlayerController = playerController;
            EquipmentController = equipmentController;
            PetController = petController;
            DataContext = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (var db = await ConnectionManager.OpenConnection())
            {
                var classes = db.CharacterClassData.Read();
                foreach (var characterClass in classes)
                {
                    CharacterClass.Items.Add(characterClass);
                }
                var pets = db.PetData.Read();
                foreach (var pet in pets)
                {
                    PetToAdd.Items.Add(pet.Name);
                }
            }
        }

        private void SearchText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                SearchButton_Click(this, new RoutedEventArgs());
                if (Player != null)
                {
                    SearchText.Text = string.Empty;
                }
            }
        }

        private void LoadRecord(IDatabase db, User user)
        {
            User = user;
            Player = PlayerController.GetPlayerByUser(user);
            var inventory = EquipmentController.GetInventoryByPlayer(Player);
            EquipList.Items.Clear();
            ItemsToRemove.Clear();
            foreach (var item in inventory)
            {
                // This triggers lazy loading of the linked tables so that it doesn't error once the context is closed.
                item.ToString();
                EquipList.Items.Add(item);
            }
            var equippableTypes = db.EquippableData.Read(x => x.CharacterClassId.Equals(Player.CharacterClassId)).Select(x => x.ItemTypeId).ToList();
            ItemToAdd.Items.Clear();
            var items = db.ItemData.Read(x => equippableTypes.Contains(x.TypeId));
            foreach (var item in items)
            {
                ItemToAdd.Items.Add(item.Name);
            }
            var pets = PetController.GetStableForUser(user);
            PetList.Items.Clear();
            PetsToRemove.Clear();
            foreach (var pet in pets)
            {
                // This triggers lazy loading of the linked tables so that it doesn't error once the context is closed.
                pet.ToString();
                PetList.Items.Add(pet);
            }
            IsPlayerLoaded = true;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(User), nameof(Player), nameof(IsPlayerLoaded), nameof(CanEdit));
            ItemToAdd.SelectedIndex = -1;
            PetToAdd.SelectedIndex = -1;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = await ConnectionManager.OpenConnection())
            {
                var user = UserController.GetUserByName(SearchText.Text);
                if (user != null)
                {
                    LoadRecord(db, user);
                    SearchText.Text = string.Empty;
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            IsEditing = true;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsEditing), nameof(CanEdit));
        }

        private void EquipList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            IsItemSelected = IsEditing && EquipList.SelectedItem != null;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsItemSelected));
        }

        private void PetList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            IsPetSelected = IsEditing && PetList.SelectedItem != null;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsPetSelected));
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemToAdd.SelectedItem != null)
            {
                using (await ConnectionManager.OpenConnection())
                {
                    var itemToAdd = EquipmentController.GetItemByName(ItemToAdd.SelectedItem.ToString());
                    var inventory = EquipmentController.GetInventoryByPlayer(Player).Concat(EquipList.Items.Cast<Inventory>()).ToList();
                    var existing = inventory.FirstOrDefault(x => x.ItemId == itemToAdd.Id);
                    if (existing != null)
                    {
                        if (existing.Count < existing.Item.Max)
                        {
                            existing.Count++;
                        }
                        else
                        {
                            MessageBox.Show($"Unable to add item, user already has max amount ({existing.Count}).", "Item Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        EquipList.Items.Add(new Inventory() { Count = 1, UserId = Player.UserId, Item = itemToAdd, ItemId = itemToAdd.Id });
                    }
                }
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = EquipList.SelectedItem as Inventory;
            if (selected != null)
            {
                EquipList.Items.Remove(selected);
                if (selected.Id > 0)
                {
                    ItemsToRemove.Add(selected);
                }
            }
        }

        private async void AddPet_Click(object sender, RoutedEventArgs e)
        {
            if (PetToAdd.SelectedItem != null)
            {
                var result = MessageBox.Show("Make this pet sparkly?", "Add Pet", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result != MessageBoxResult.Cancel)
                {
                    var isSparkly = result == MessageBoxResult.Yes;
                    using (await ConnectionManager.OpenConnection())
                    {
                        var petToAdd = PetController.GetPets().FirstOrDefault(x => x.Name.Equals(PetToAdd.SelectedItem.ToString()));
                        if (PetController.GetStableForUser(User).Any(x => x.Pet.Equals(petToAdd) && x.IsSparkly == isSparkly))
                        {
                            var sparklyString = isSparkly ? "sparkly " : "";
                            MessageBox.Show($"Unable to add pet, player already has a {sparklyString}{petToAdd.Name}", "Duplicate Pet", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            var record = new Stable()
                            {
                                UserId = User.TwitchId,
                                Name = petToAdd.Name,
                                Pet = petToAdd,
                                PetId = petToAdd.Id,
                                IsSparkly = isSparkly,
                                Level = 1
                            };
                            PetList.Items.Add(record);
                        }
                    }
                }
            }
        }

        private void RemovePet_Click(object sender, RoutedEventArgs e)
        {
            var selected = PetList.SelectedItem as Stable;
            if (selected != null)
            {
                PetList.Items.Remove(selected);
                if (selected.Id > 0)
                {
                    PetsToRemove.Add(selected);
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var db = await ConnectionManager.OpenConnection())
            {
                var dbPlayer = db.PlayerCharacters.FirstOrDefault(x => x.UserId == Player.UserId);
                var levelFromXp = PlayerController.LevelFromExperience(Player.Experience);
                if (Player.Level != levelFromXp)
                {
                    var result = MessageBox.Show("The Level and XP values do not match. Would you like to keep the level and update the XP value (yes) or update the level to match the XP (no)", "Experience Level Conflict", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        Player.Experience = PlayerController.ExperienceForLevel(Player.Level);
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        Player.Level = PlayerController.LevelFromExperience(Player.Experience);
                    }
                    else
                    {
                        return;
                    }
                }
                dbPlayer.Level = Player.Level;
                dbPlayer.Experience = Player.Experience;
                dbPlayer.Prestige = Player.Prestige;
                dbPlayer.Currency = Player.Currency;
                dbPlayer.CharacterClass = db.CharacterClassData.ReadById((CharacterClass.SelectedItem as CharacterClass).Id);
                var items = EquipList.Items.Cast<Inventory>();
                foreach (var item in items.Where(x => x.Id == 0))
                {
                    EquipmentController.AddInventoryRecord(User, db.ItemData.ReadById(item.ItemId));
                }
                foreach (var item in ItemsToRemove)
                {
                    db.Inventories.DeleteById(item.Id);
                }
                var pets = PetList.Items.Cast<Stable>();
                foreach (var pet in pets.Where(x => x.Id == 0))
                {
                    PetController.AddStableRecord(User, db.PetData.ReadById(pet.PetId), pet.IsSparkly);
                }
                foreach (var pet in PetsToRemove)
                {
                    db.Stables.DeleteById(pet.Id);
                }
            }
            // Dispose of the context to commit any pending changes, then open a new one
            using (var db = await ConnectionManager.OpenConnection())
            {
                LoadRecord(db, User);
            }
            IsEditing = false;
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsEditing), nameof(CanEdit));
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsEditing = false;
            using (var db = await ConnectionManager.OpenConnection())
            {
                LoadRecord(db, User);
            }
            InterfaceUtils.FireChangeEvent(this, PropertyChanged, nameof(IsEditing), nameof(CanEdit));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (IsEditing)
            {
                var result = MessageBox.Show("You are currently editing a player. Would you like to save your changes before closing?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SaveButton_Click(this, new RoutedEventArgs());
                }
                e.Cancel = result == MessageBoxResult.Cancel;
            }
        }
    }
}
