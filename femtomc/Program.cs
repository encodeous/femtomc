// increase connection limit to fast download

using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft.MsalClient;
using femtomc;
using Serilog;
using static Bullseye.Targets;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Session variable to store minecraft login result
MSession? session = null;

var app = MsalMinecraftLoginHelper.CreateDefaultApplicationBuilder("a5d0a015-361f-4e31-b300-7d2b46937190")
    .Build();

var instanceRoot = Path.Join(Environment.CurrentDirectory, ".femto");

MinecraftPathUtils.ReplacePaths(instanceRoot);

System.Net.ServicePointManager.DefaultConnectionLimit = 256;

//var path = new MinecraftPath("game_directory_path");
var path = new MinecraftPath(instanceRoot); // use default directory

var launcher = new CMLauncher(path);

// show launch progress to console
// launcher.FileChanged += (e) =>
// {
//     Console.WriteLine("[{0}] {1} - {2}/{3}", e.FileKind.ToString(), e.FileName, e.ProgressedFileCount, e.TotalFileCount);
// };
// launcher.ProgressChanged += (s, e) =>
// {
//     Console.WriteLine("{0}%", e.ProgressPercentage);
// };

var versions = await launcher.GetAllVersionsAsync();
foreach (var item in versions)
{
    // show all version names
    // use this version name in CreateProcessAsync method.
    Console.WriteLine(item.Name);
}


Target("default", DependsOn("launch-mc"));
Target("auth", async () =>
{
    var handler = new MsalMinecraftLoginHandler(app);
    try
    {
        Console.WriteLine("Attempting to use cached session to login");
        session = await handler.LoginSilent();
    }
    catch
    {
        session = await handler.LoginDeviceCode(result =>
        {
            Console.WriteLine($"Code: {result.UserCode}, Expires On: {result.ExpiresOn.LocalDateTime}");
            Console.WriteLine(result.Message);
            return Task.CompletedTask;
        });
    }
});

var version = launcher.GetVersion("1.7.10");

var preflight = new Preflight(path, version, launcher);
preflight.Run();

Target("launch-mc", DependsOn("auth", "preflight"), async () =>
{
    var launchOption = new MLaunchOption
    {
        MaximumRamMb = 1024,
        Session = session,
        StartVersion = version,
        Path = path
    };
    var launch = new MLaunch(launchOption);
    var proc = await Task.Run(launch.GetProcess).ConfigureAwait(false);
    Log.Information($"Started Minecraft process. The console output will now be redirected.");
    proc.Start();
    await Log4jParser.RedirectUntilEnd(proc.StandardOutput, session.AccessToken);
    await proc.WaitForExitAsync();
    Log.Information($"The Minecraft process has exited with code {proc.ExitCode}.");
});
await RunTargetsWithoutExitingAsync(args);