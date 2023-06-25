using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nrrdio.MapGenerator.App.Contracts.Services;
using Nrrdio.MapGenerator.App.Models;
using Windows.Storage;

namespace Nrrdio.MapGenerator.App.Services;

public class StateManager : IStateManager {
    public static string SettingsPath { get; } = Path.Combine(ApplicationData.Current.LocalFolder.Path, "localSettings.json");

    Settings Settings { get; }

    public StateManager(
        IOptions<Settings> settings
    ) {
        Settings = settings.Value;

        // Ensures any changes to settings will get saved back to the filesystem.
        Settings.PropertyChanged += UpdateSettings;
    }

    public void UpdateSettings(object? sender, PropertyChangedEventArgs e) {
        if (sender is Settings) {
            WriteSettings();
        }
    }

    void WriteSettings() {
        WriteSettings(new {
            Settings,
        });
    }

    public static void EnsureSettings() {
        if (!File.Exists(SettingsPath)) {
            WriteSettings(new object());
        }
    }

    public static void WriteSettings(object settingsObject) {
        var fileContent = JsonSerializer.Serialize(settingsObject, DefaultSerializerOptions);

        File.WriteAllText(SettingsPath, fileContent, Encoding.UTF8);
    }

    static JsonSerializerOptions DefaultSerializerOptions => new() { WriteIndented = true };
}
