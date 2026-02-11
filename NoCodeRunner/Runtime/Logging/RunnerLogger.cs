using Godot;
using System;

namespace Runtime.Logging;

public static class RunnerLogger
{
    public static bool IncludeStackTraces { get; private set; }
    public static bool ShowParserWarnings { get; private set; } = true;

    public static void Configure(bool includeStackTraces, bool showParserWarnings)
    {
        IncludeStackTraces = includeStackTraces;
        ShowParserWarnings = showParserWarnings;
        Info("Log", $"Logger configured: includeStackTraces={IncludeStackTraces}, showParserWarnings={ShowParserWarnings}");
    }

    public static void Info(string subsystem, string message)
    {
        GD.Print($"[{subsystem}] {message}");
    }

    public static void Warn(string subsystem, string message)
    {
        GD.Print($"WARNING: [{subsystem}] {message}");
    }

    public static void Warn(string subsystem, string message, Exception ex)
    {
        Warn(subsystem, BuildExceptionMessage(message, ex));
    }

    public static void Error(string subsystem, string message)
    {
        GD.PrintErr($"ERROR: [{subsystem}] {message}");
    }

    public static void Error(string subsystem, string message, Exception ex)
    {
        Error(subsystem, BuildExceptionMessage(message, ex));
    }

    public static void ParserWarning(string message)
    {
        if (!ShowParserWarnings)
        {
            return;
        }

        Warn("SML", message);
    }

    private static string BuildExceptionMessage(string message, Exception ex)
    {
        if (!IncludeStackTraces)
        {
            return $"{message}: {ex.Message}";
        }

        return $"{message}: {ex}";
    }
}
