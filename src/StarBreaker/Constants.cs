﻿using Avalonia.Platform.Storage;

namespace StarBreaker;

public static class Constants
{
    public const string DataP4k = "Data.p4k";
    public const string BuildManifest = "build_manifest.id";
    public static readonly string DefaultStarCitizenFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen");
    public static readonly string DefaultRSILauncherFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rsilauncher");

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