/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of NoCodeRunner.
 *
 *  NoCodeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  NoCodeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with NoCodeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

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
        PrintRichColored($"WARNING: [{subsystem}] {message}", "#ffd166");
    }

    public static void Warn(string subsystem, string message, Exception ex)
    {
        Warn(subsystem, BuildExceptionMessage(message, ex));
    }

    public static void Error(string subsystem, string message)
    {
        PrintRichColored($"ERROR: [{subsystem}] {message}", "#ff6b6b");
    }

    public static void Success(string subsystem, string message)
    {
        PrintRichColored($"SUCCESS: [{subsystem}] {message}", "#70e000");
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

    private static void PrintRichColored(string message, string colorHex)
    {
        GD.PrintRich($"[color={colorHex}]{message}[/color]");
    }
}
