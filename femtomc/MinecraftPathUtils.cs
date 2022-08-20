using System.Reflection;
using CmlLib.Core;

namespace femtomc;

public class MinecraftPathUtils
{
    public static void ReplacePaths(string newPath)
    {
        MinecraftPath.MacDefaultPath = newPath;
        MinecraftPath.LinuxDefaultPath = newPath;
        MinecraftPath.WindowsDefaultPath = newPath;
    }
}