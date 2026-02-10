using Godot;

namespace Runtime.Logging;

public static class RunnerLogger
{
    public static void Info(string subsystem, string message)
    {
        GD.Print($"[{subsystem}] {message}");
    }

    public static void Warn(string subsystem, string message)
    {
        GD.Print($"WARNING: [{subsystem}] {message}");
    }

    public static void Error(string subsystem, string message)
    {
        GD.PrintErr($"ERROR: [{subsystem}] {message}");
    }
}
