/*
#############################################################################
# Copyright (C) 2026 CrowdWare
#
# This file is part of Forge.
#
# SPDX-License-Identifier: GPL-3.0-or-later OR LicenseRef-CrowdWare-Commercial
#
# Forge is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# Forge is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with Forge. If not, see <https://www.gnu.org/licenses/>.
#
# Commercial licensing is available from CrowdWare for proprietary use.
#############################################################################
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Forge.Ai.Core;
using Forge.Ai.Imaging;
using Forge.Ai.Util;
using Forge.Ai.Video;

namespace PromptPlugin;

public static class EntryPoints
{
    private const string KeychainService = "ForgePoser";
    private const string KeychainAccount = "GROK_API_KEY";
    private static string _lastError = string.Empty;
    private static string _sessionApiKey = string.Empty;

    public static string OnPromptChanged(string projectPath, string prompt)
    {
        var path = string.IsNullOrWhiteSpace(projectPath) ? "(unsaved)" : projectPath;
        var text = string.IsNullOrWhiteSpace(prompt) ? "" : prompt.Trim();
        return $"Prompt updated for {path} ({text.Length} chars)";
    }

    public static bool IsConfigured()
    {
        try
        {
            _ = BuildOptions();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetApiKeyForSession(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _sessionApiKey = string.Empty;
            return false;
        }

        _sessionApiKey = apiKey.Trim();
        return true;
    }

    public static string GetApiKeyStatus()
    {
        if (!string.IsNullOrWhiteSpace(_sessionApiKey))
        {
            return "Configured via session override key.";
        }

        var grok = Environment.GetEnvironmentVariable("GROK_API_KEY");
        if (!string.IsNullOrWhiteSpace(grok))
        {
            return "Configured via GROK_API_KEY.";
        }

        var xai = Environment.GetEnvironmentVariable("XAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(xai))
        {
            return "Configured via XAI_API_KEY.";
        }

        return "No API key found in GROK_API_KEY or XAI_API_KEY.";
    }

    public static bool SaveApiKeyToKeychain(string apiKey)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _lastError = "Keychain storage is currently only implemented for macOS.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _lastError = "API key is empty.";
            return false;
        }

        var key = apiKey.Trim();
        var args = $"add-generic-password -a {QuoteArg(KeychainAccount)} -s {QuoteArg(KeychainService)} -w {QuoteArg(key)} -U";
        var (ok, stdOut, stdErr) = RunProcess("/usr/bin/security", args);
        if (!ok)
        {
            _lastError = $"SaveApiKeyToKeychain failed: {stdErr}";
            return false;
        }

        _lastError = string.Empty;
        return true;
    }

    public static string LoadApiKeyFromKeychain()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _lastError = "Keychain storage is currently only implemented for macOS.";
            return string.Empty;
        }

        var args = $"find-generic-password -a {QuoteArg(KeychainAccount)} -s {QuoteArg(KeychainService)} -w";
        var (ok, stdOut, stdErr) = RunProcess("/usr/bin/security", args);
        if (!ok)
        {
            _lastError = $"LoadApiKeyFromKeychain failed: {stdErr}";
            return string.Empty;
        }

        _lastError = string.Empty;
        return stdOut.Trim();
    }

    public static bool DeleteApiKeyFromKeychain()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _lastError = "Keychain storage is currently only implemented for macOS.";
            return false;
        }

        var args = $"delete-generic-password -a {QuoteArg(KeychainAccount)} -s {QuoteArg(KeychainService)}";
        var (ok, _, stdErr) = RunProcess("/usr/bin/security", args);
        if (!ok)
        {
            _lastError = $"DeleteApiKeyFromKeychain failed: {stdErr}";
            return false;
        }

        _lastError = string.Empty;
        return true;
    }

    public static string GetLastError()
    {
        return _lastError;
    }

    public static string StylizeImage(
        string posePath,
        string outputPath,
        string prompt,
        string stylePath,
        string extraPath,
        string negativePrompt,
        string model)
    {
        try
        {
            _lastError = string.Empty;
            var resolvedPose = NormalizePath(posePath);
            var resolvedOutput = VersionedOutputPath.Resolve(NormalizePath(outputPath));
            var resolvedStyle = NormalizeOptionalPath(stylePath);
            var resolvedExtra = NormalizeOptionalPath(extraPath);
            var effectiveModel = string.IsNullOrWhiteSpace(model) ? "grok-imagine-image" : model;
            var effectiveNegative = string.IsNullOrWhiteSpace(negativePrompt) ? null : negativePrompt;

            var options = BuildOptions();
            var service = new GrokImageService(options);
            _ = service.EditImageAsync(new GrokImageEditRequest(
                    Prompt: prompt ?? string.Empty,
                    PoseImagePath: resolvedPose,
                    OutputPath: resolvedOutput,
                    Model: effectiveModel,
                    StyleImagePath: resolvedStyle,
                    ExtraImagePath: resolvedExtra,
                    NegativePrompt: effectiveNegative))
                .GetAwaiter()
                .GetResult();

            return resolvedOutput;
        }
        catch (Exception ex)
        {
            _lastError = $"StylizeImage failed: {ex.Message}";
            return string.Empty;
        }
    }

    public static string StylizeVideo(
        string inputVideoPath,
        string outputPath,
        string prompt,
        string negativePrompt,
        string model)
    {
        try
        {
            _lastError = string.Empty;
            var resolvedInput = NormalizePath(inputVideoPath);
            var resolvedOutput = VersionedOutputPath.Resolve(NormalizePath(outputPath));
            var effectiveModel = string.IsNullOrWhiteSpace(model) ? "grok-imagine-video" : model;
            var effectiveNegative = string.IsNullOrWhiteSpace(negativePrompt) ? null : negativePrompt;

            var options = BuildOptions();
            var service = new GrokVideoService(options);
            _ = service.StylizeVideoAsync(new GrokVideoStylizeRequest(
                    InputVideoPath: resolvedInput,
                    Prompt: prompt ?? string.Empty,
                    OutputPath: resolvedOutput,
                    Model: effectiveModel,
                    NegativePrompt: effectiveNegative))
                .GetAwaiter()
                .GetResult();

            return resolvedOutput;
        }
        catch (Exception ex)
        {
            _lastError = $"StylizeVideo failed: {ex.Message}";
            return string.Empty;
        }
    }

    public static string CreateVideoFromFrames(string framesDirectory, int fps, string outputPath, string pattern)
    {
        try
        {
            _lastError = string.Empty;
            var dir = NormalizePath(framesDirectory);
            if (!Directory.Exists(dir))
            {
                _lastError = $"Frame directory not found: {dir}";
                return string.Empty;
            }

            var resolvedOutput = VersionedOutputPath.Resolve(NormalizePath(outputPath));
            var outputDir = Path.GetDirectoryName(resolvedOutput);
            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var framePattern = string.IsNullOrWhiteSpace(pattern) ? "frame_%04d.png" : pattern;
            var ffmpegExecutable = ResolveFfmpegExecutable();
            if (string.IsNullOrWhiteSpace(ffmpegExecutable))
            {
                _lastError = "ffmpeg not found. Install it (macOS: `brew install ffmpeg`) and ensure it is in PATH.";
                return string.Empty;
            }

            var args =
                $"-hide_banner -loglevel error -nostats -y -framerate {Math.Max(1, fps)} -i \"{Path.Combine(dir, framePattern)}\" -c:v libx264 -pix_fmt yuv420p \"{resolvedOutput}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegExecutable,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            RemoveDyldOverrides(startInfo);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _lastError = "Failed to start ffmpeg process.";
                return string.Empty;
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var stdErr = process.StandardError.ReadToEnd();
                _lastError = $"ffmpeg exit code {process.ExitCode}: {stdErr}";
                return string.Empty;
            }

            return resolvedOutput;
        }
        catch (Exception ex)
        {
            _lastError = $"CreateVideoFromFrames failed: {ex.Message}";
            return string.Empty;
        }
    }

    private static string ResolveFfmpegExecutable()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("FFMPEG_PATH"),
            "/opt/homebrew/bin/ffmpeg",
            "/usr/local/bin/ffmpeg",
            "ffmpeg"
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var trimmed = candidate.Trim();
            if (Path.IsPathRooted(trimmed))
            {
                if (File.Exists(trimmed))
                {
                    return trimmed;
                }

                continue;
            }

            if (CanExecute(trimmed))
            {
                return trimmed;
            }
        }

        return string.Empty;
    }

    private static bool CanExecute(string executable)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            RemoveDyldOverrides(startInfo);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(2000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveDyldOverrides(ProcessStartInfo startInfo)
    {
        startInfo.EnvironmentVariables.Remove("DYLD_LIBRARY_PATH");
        startInfo.EnvironmentVariables.Remove("DYLD_FALLBACK_LIBRARY_PATH");
        startInfo.EnvironmentVariables.Remove("DYLD_INSERT_LIBRARIES");
        startInfo.EnvironmentVariables.Remove("DYLD_PRINT_LIBRARIES");
        startInfo.EnvironmentVariables.Remove("DYLD_PRINT_TO_FILE");
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return trimmed;
        }

        return Path.GetFullPath(trimmed);
    }

    private static string? NormalizeOptionalPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : NormalizePath(path);
    }

    private static ForgeAiClientOptions BuildOptions()
    {
        if (!string.IsNullOrWhiteSpace(_sessionApiKey))
        {
            return new ForgeAiClientOptions(_sessionApiKey, "https://api.x.ai/v1");
        }

        return ForgeAiClientOptions.FromEnvironment();
    }

    private static (bool Ok, string StdOut, string StdErr) RunProcess(string fileName, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (false, string.Empty, "Could not start process.");
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode == 0, stdout, string.IsNullOrWhiteSpace(stderr) ? $"Exit code {process.ExitCode}" : stderr.Trim());
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    private static string QuoteArg(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
