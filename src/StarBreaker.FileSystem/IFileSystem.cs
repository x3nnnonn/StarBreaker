namespace StarBreaker.FileSystem;

/// <summary>
///     Interface for file system operations.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    ///     Get the files in a directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>The files in the directory.</returns>
    IEnumerable<string> GetFiles(string path);

    /// <summary>
    ///    Get the files in a directory that match a search pattern.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="searchPattern">The search pattern.</param>
    /// <returns>The files in the directory that match the search pattern.</returns>
    IEnumerable<string> GetFiles(string path, string searchPattern);
    
    /// <summary>
    ///     Get the directories in a directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>The directories in the directory.</returns>
    IEnumerable<string> GetDirectories(string path);

    /// <summary>
    ///     Check if a file exists.
    /// </summary>
    /// <param name="path">The path to the file or folder.</param>
    /// <returns>True if the file or folder exists, false otherwise.</returns>
    bool FileExists(string path);
    
    /// <summary>
    ///     Open a file for reading.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A stream for reading the file.</returns>
    Stream OpenRead(string path);

    byte[] ReadAllBytes(string path);
}