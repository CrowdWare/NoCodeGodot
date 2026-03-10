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

using Runtime.Sml;
using Xunit;

namespace SMLCore.Tests;

public sealed class SmlUriResolverTests
{
    [Theory]
    [InlineData("res:/docs/app.sml", "res://docs/app.sml")]
    [InlineData("user:/cache/a.bin", "user://cache/a.bin")]
    [InlineData("ipfs://bafybeicid/path/file.md", "ipfs:/bafybeicid/path/file.md")]
    [InlineData("ipfs:/bafybeicid", "ipfs:/bafybeicid")]
    [InlineData("https://example.com/app.sml", "https://example.com/app.sml")]
    public void Normalize_MapsKnownShortForms(string input, string expected)
    {
        var actual = SmlUriResolver.Normalize(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("res://docs/app.sml", SmlUriSchemeKind.Res)]
    [InlineData("user://cache/app.sml", SmlUriSchemeKind.User)]
    [InlineData("file:///tmp/a.sml", SmlUriSchemeKind.File)]
    [InlineData("http://example.com/a.sml", SmlUriSchemeKind.Http)]
    [InlineData("https://example.com/a.sml", SmlUriSchemeKind.Https)]
    [InlineData("ipfs:/bafybeicid/file", SmlUriSchemeKind.Ipfs)]
    [InlineData("images/pic.png", SmlUriSchemeKind.Relative)]
    public void ClassifyScheme_DetectsExpectedKind(string uri, SmlUriSchemeKind expected)
    {
        var kind = SmlUriResolver.ClassifyScheme(uri);
        Assert.Equal(expected, kind);
    }

    [Fact]
    public void ResolveRelative_ForResBase_ResolvesPathSegments()
    {
        var resolved = SmlUriResolver.ResolveRelative("images/pic.png", "res:/docs/book/ch1.md");
        Assert.Equal("res://docs/book/images/pic.png", resolved);
    }

    [Fact]
    public void ResolveRelative_ForHttpBase_ResolvesPathSegments()
    {
        var resolved = SmlUriResolver.ResolveRelative("images/pic.png", "https://example.com/docs/book/ch1.md");
        Assert.Equal("https://example.com/docs/book/images/pic.png", resolved);
    }

    [Fact]
    public void ResolveRelative_LeavesAbsoluteUrisUntouched()
    {
        var resolved = SmlUriResolver.ResolveRelative("https://cdn.example.com/i.png", "res://docs/ch1.md");
        Assert.Equal("https://cdn.example.com/i.png", resolved);
    }

    [Fact]
    public void MapIpfsToHttp_UsesGateway()
    {
        var mapped = SmlUriResolver.MapIpfsToHttp("ipfs:/bafybeicid/path/file.png", "https://gateway.example/ipfs");
        Assert.Equal("https://gateway.example/ipfs/bafybeicid/path/file.png", mapped);
    }
}