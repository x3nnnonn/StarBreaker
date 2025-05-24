using Avalonia.Platform.Storage;

namespace StarBreaker;

public static class Constants
{
    public const string DataP4k = "Data.p4k";
    public const string BuildManifest = "build_manifest.id";
    public static readonly string DefaultStarCitizenFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen");
    public static readonly string DefaultRSILauncherFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rsilauncher");
    private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarBreaker");
    public static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");

    public static FilePickerOpenOptions GetP4kFilter(IStorageFolder? defaultPath) => new()
    {
        FileTypeFilter =
        [
            new FilePickerFileType("P4k File")
            {
                Patterns = ["*.p4k"],
            }
        ],
        AllowMultiple = false,
        Title = "Select a P4k file",
        SuggestedFileName = DataP4k,
        SuggestedStartLocation = defaultPath
    };
}

public class AppSettings
{
    public string? CustomInstallFolder { get; set; }
    public string? DiffGameFolder { get; set; }
    public string? DiffOutputDirectory { get; set; }
    public string? SelectedChannel { get; set; }
    public string? TextFormat { get; set; }
}