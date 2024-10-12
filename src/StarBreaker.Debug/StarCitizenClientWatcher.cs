using System.Text.Json;

namespace StarBreaker.Debug;

public class StarCitizenClientWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public event EventHandler<LoginData>? LoginDataChanged;

    public StarCitizenClientWatcher(string starCitizenBaseFolder)
    {
        _watcher = new FileSystemWatcher
        {
            Path = starCitizenBaseFolder,
            IncludeSubdirectories = true,
            Filter = "loginData.json",
            NotifyFilter = NotifyFilters.LastWrite
        };
        _watcher.Changed += OnChanged;
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var loginData = JsonSerializer.Deserialize<LoginData>(File.ReadAllText(e.FullPath));

        if (loginData == null)
            return;

        LoginDataChanged?.Invoke(this, loginData);
    }

    public async Task<LoginData> WaitForLoginData()
    {
        var targetFile = Directory.GetFiles(_watcher.Path, "loginData.json", SearchOption.AllDirectories).FirstOrDefault();
        if (targetFile != null && File.Exists(targetFile))
        {
            try
            {
                var loginData = JsonSerializer.Deserialize<LoginData>(await File.ReadAllTextAsync(targetFile));
                if (loginData != null)
                    return loginData;
            }
            catch
            {
                //this is fine, the file is probably empty or something
            }
        }

        var tcs = new TaskCompletionSource<LoginData>();
        LoginDataChanged += (_, data) => tcs.SetResult(data);
        return await tcs.Task;
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnChanged;
        _watcher.Dispose();
    }
}