namespace LobotJR.Interface.Settings
{
    public interface ISettingsPage<T>
    {
        string Category { get; }
        void Load(T settingsObject);
        void Save(T settingsObject);
    }
}
