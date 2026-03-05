/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

using Godot;
using System;

namespace Runtime.Logging;

public static class RunnerLogger
{
    public static bool IncludeStackTraces { get; private set; }
    public static bool ShowParserWarnings { get; private set; } = true;
    public static bool ShowDebugLogs { get; private set; }
    public static bool ShowWarningBacktraces { get; private set; }

    public static void Configure(bool includeStackTraces, bool showParserWarnings, bool showDebugLogs, bool showWarningBacktraces)
    {
        IncludeStackTraces = includeStackTraces;
        ShowParserWarnings = showParserWarnings;
        ShowDebugLogs = showDebugLogs;
        ShowWarningBacktraces = showWarningBacktraces;
        Debug("Log", $"Logger configured: includeStackTraces={IncludeStackTraces}, showParserWarnings={ShowParserWarnings}, showDebugLogs={ShowDebugLogs}, showWarningBacktraces={ShowWarningBacktraces}");
    }

    public static void Info(string subsystem, string message)
    {
        GD.Print(message);
    }

    public static void Warn(string subsystem, string message)
    {
        var formatted = $"WARNING ({subsystem}): {message}";
        if (ShowWarningBacktraces)
        {
            GD.PushWarning(formatted);
            return;
        }

        PrintRichColored(formatted, "yellow");
    }

    public static void Warn(string subsystem, string message, Exception ex)
    {
        Warn(subsystem, BuildExceptionMessage(message, ex));
    }

    public static void Error(string subsystem, string message)
    {
        var formatted = $"ERROR ({subsystem}): {message}";
        if (ShowWarningBacktraces)
        {
            GD.PushError(formatted);
            return;
        }

        PrintRichColored(formatted, "red");
    }

    public static void Success(string subsystem, string message)
    {
        PrintRichColored(message, "lime");
    }

    public static void Debug(string subsystem, string message)
    {
        if (!ShowDebugLogs)
        {
            return;
        }

        GD.Print($"DEBUG: {message}");
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

    private static void PrintRichColored(string message, string colorNameOrHex)
    {
        if (OS.HasFeature("editor"))
        {
            GD.PrintRich($"[color={colorNameOrHex}][b]{message}[/b][/color]");
            return;
        }

        GD.Print($"{ToAnsiColor(colorNameOrHex)}{message}\u001b[0m");
    }

    private static string ToAnsiColor(string colorNameOrHex)
    {
        return colorNameOrHex switch
        {
            "lime" or "#70e000" => "\u001b[32m", // success green
            "yellow" or "#f1c40f" => "\u001b[33m", // warning yellow/orange
            "red" or "#e74c3c" => "\u001b[31m", // error red
            _ => "\u001b[37m" // info/default white
        };
    }
}
