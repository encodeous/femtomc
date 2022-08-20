using System.Collections.Concurrent;
using CmlLib.Core;
using CmlLib.Core.Downloader;
using CmlLib.Core.Files;
using CmlLib.Core.Version;

namespace femtomc;
using static Bullseye.Targets;

public class Preflight
{
    private ConcurrentDictionary<string, DownloadFile[]?> _fileMap = new ();
    private MinecraftPath _path;
    private MVersion _version;
    private CMLauncher _launcher;

    public Preflight(MinecraftPath path, MVersion version, CMLauncher launcher)
    {
        _path = path;
        _version = version;
        _launcher = launcher;
    }

    public void Run()
    {
        var targets = new []
        {
            AddCheckTarget("assets", new AssetChecker()),
            AddCheckTarget("client", new ClientChecker()),
            AddCheckTarget("libraries", new LibraryChecker()),
            AddCheckTarget("java-env", new JavaChecker()),
            AddCheckTarget("log", new LogChecker())
        };
        Target("preflight", DependsOn(targets));
    }

    private string AddCheckTarget(string name, IFileChecker checker)
    {
        Target("check-" + name, async () =>
        {
            _fileMap[name] = checker.CheckFiles(_path, _version, _launcher.pFileChanged);
        });
        Target("download-" + name, DependsOn("check-" + name), async () =>
        {
            if (_fileMap[name] is null || _fileMap[name]!.Length == 0)
            {
                Console.WriteLine("Target skipped, files are up-to-date.");
                return;
            }

            await _launcher.DownloadGameFiles(_fileMap[name]!);
        });
        Target("preflight-" + name, DependsOn("download-" + name));
        return "preflight-" + name;
    }
}