using LobotJR.Data;
using System.Windows.Controls;

namespace LobotJR.Interface.Settings
{
    /// <summary>
    /// Interaction logic for AwardSettings.xaml
    /// </summary>
    public partial class AwardSettings : UserControl, ISettingsPage<GameSettings>
    {
        public string Category => "RPG";

        public string AwardFrequencyValue { get; set; }
        public string XpAmountValue { get; set; }
        public string CoinAmountValue { get; set; }
        public string SubMultiplierValue { get; set; }

        public AwardSettings()
        {
            InitializeComponent();
        }

        public void Load(GameSettings settingsObject)
        {
            AwardFrequencyValue = settingsObject.ExperienceFrequency.ToString();
            XpAmountValue = settingsObject.ExperienceValue.ToString();
            CoinAmountValue = settingsObject.CoinValue.ToString();
            SubMultiplierValue = settingsObject.SubRewardMultiplier.ToString();
        }

        public void Save(GameSettings settingsObject)
        {
            int value;
            if (int.TryParse(AwardFrequencyValue, out value))
            {
                settingsObject.ExperienceFrequency = value;
            }
            if (int.TryParse(XpAmountValue, out value))
            {
                settingsObject.ExperienceValue = value;
            }
            if (int.TryParse(CoinAmountValue, out value))
            {
                settingsObject.CoinValue = value;
            }
            if (int.TryParse(SubMultiplierValue, out value))
            {
                settingsObject.SubRewardMultiplier = value;
            }
        }
    }
}
