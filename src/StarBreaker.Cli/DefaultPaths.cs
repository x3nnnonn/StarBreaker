using System;
using System.IO;

namespace StarBreaker.Chf;

//TODO: remove this. make the base path user-configurable and derive the rest from it
public static class DefaultPaths
{
    public static readonly string StarCitizenFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Roberts Space Industries", "StarCitizen");
    public static readonly string StarCitizenCharactersFolder = Path.Combine(StarCitizenFolder, "LIVE", "user", "client", "0", "CustomCharacters");

    public static readonly string Base = Path.Combine(Environment.CurrentDirectory, "data");
    public static readonly string WebsiteCharacters = Path.Combine(Base, "websiteCharacters");
    public static readonly string LocalCharacters  = Path.Combine(Base, "localCharacters");
    public static readonly string ModdedCharacters  = Path.Combine(Base, "moddedCharacters");
}