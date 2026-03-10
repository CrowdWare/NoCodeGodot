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

using Runtime.Sms;
using Xunit;

namespace SMSCore.Tests;

public sealed class ProjectFsTests : IDisposable
{
    private readonly string _root;

    public ProjectFsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "smscore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ReadWriteText_WorksInsideRoot()
    {
        var fs = new ProjectFs(_root);
        fs.CreateDir("src");
        fs.WriteText("src/app.sms", "fun main() = 1");

        var text = fs.ReadText("src/app.sms");

        Assert.Equal("fun main() = 1", text);
        Assert.True(fs.Exists("src/app.sms"));
    }

    [Fact]
    public void Traversal_IsRejected()
    {
        var fs = new ProjectFs(_root);

        var ex = Assert.Throws<ProjectFsException>(() => fs.ReadText("../secret.txt"));

        Assert.Equal(ProjectFsErrorCode.InvalidPath, ex.Code);
    }

    [Fact]
    public void AbsolutePath_IsRejected()
    {
        var fs = new ProjectFs(_root);

        var ex = Assert.Throws<ProjectFsException>(() => fs.Exists("/etc/passwd"));

        Assert.Equal(ProjectFsErrorCode.InvalidPath, ex.Code);
    }

    [Fact]
    public void SymlinkEscape_IsRejected()
    {
        var outsideDir = Path.Combine(_root, "..", "outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "secret.txt");
        File.WriteAllText(outsideFile, "secret");

        var linkPath = Path.Combine(_root, "escape-link");
        try
        {
            Directory.CreateSymbolicLink(linkPath, outsideDir);
        }
        catch
        {
            // Symbolic links may be restricted on some environments.
            return;
        }

        var fs = new ProjectFs(_root);
        var ex = Assert.Throws<ProjectFsException>(() => fs.ReadText("escape-link/secret.txt"));
        Assert.Equal(ProjectFsErrorCode.AccessDenied, ex.Code);
    }

    [Fact]
    public void SizeLimits_AreEnforced()
    {
        var fs = new ProjectFs(_root, maxReadBytes: 4, maxWriteBytes: 4);
        fs.WriteText("tiny.txt", "1234");

        var writeEx = Assert.Throws<ProjectFsException>(() => fs.WriteText("too-large.txt", "12345"));
        Assert.Equal(ProjectFsErrorCode.TooLarge, writeEx.Code);

        File.WriteAllText(Path.Combine(_root, "read-too-large.txt"), "12345");
        var readEx = Assert.Throws<ProjectFsException>(() => fs.ReadText("read-too-large.txt"));
        Assert.Equal(ProjectFsErrorCode.TooLarge, readEx.Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
