using System;
using System.Collections.Generic;
using System.IO;

namespace Runtime.Sml;

public enum SmlUriSchemeKind
{
    Relative,
    Res,
    User,
    File,
    Http,
    Https,
    Ipfs,
    Unknown
}

public static class SmlUriResolver
{
    public const string DefaultIpfsGateway = "https://ipfs.io/ipfs";

    public static string Normalize(string rawUri)
    {
        if (string.IsNullOrWhiteSpace(rawUri))
        {
            return string.Empty;
        }

        var value = rawUri.Trim();

        if (value.StartsWith("res:/", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return "res://" + value["res:/".Length..].TrimStart('/', '\\');
        }

        if (value.StartsWith("user:/", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            return "user://" + value["user:/".Length..].TrimStart('/', '\\');
        }

        if (value.StartsWith("ipfs://", StringComparison.OrdinalIgnoreCase))
        {
            return "ipfs:/" + value["ipfs://".Length..].TrimStart('/', '\\');
        }

        if (value.StartsWith("ipfs:/", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("ipfs://", StringComparison.OrdinalIgnoreCase))
        {
            return "ipfs:/" + value["ipfs:/".Length..].TrimStart('/', '\\');
        }

        if (value.StartsWith("file:/", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return "file://" + value["file:".Length..];
        }

        return value;
    }

    public static SmlUriSchemeKind ClassifyScheme(string rawOrNormalizedUri)
    {
        var value = Normalize(rawOrNormalizedUri);

        if (string.IsNullOrWhiteSpace(value))
        {
            return SmlUriSchemeKind.Relative;
        }

        if (value.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return SmlUriSchemeKind.Res;
        }

        if (value.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            return SmlUriSchemeKind.User;
        }

        if (value.StartsWith("ipfs:/", StringComparison.OrdinalIgnoreCase))
        {
            return SmlUriSchemeKind.Ipfs;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.Scheme.ToLowerInvariant() switch
            {
                "file" => SmlUriSchemeKind.File,
                "http" => SmlUriSchemeKind.Http,
                "https" => SmlUriSchemeKind.Https,
                _ => SmlUriSchemeKind.Unknown
            };
        }

        return Path.IsPathRooted(value)
            ? SmlUriSchemeKind.File
            : SmlUriSchemeKind.Relative;
    }

    public static bool IsLocalScheme(SmlUriSchemeKind kind)
    {
        return kind is SmlUriSchemeKind.Res or SmlUriSchemeKind.User or SmlUriSchemeKind.File;
    }

    public static bool IsRemoteScheme(SmlUriSchemeKind kind)
    {
        return kind is SmlUriSchemeKind.Http or SmlUriSchemeKind.Https or SmlUriSchemeKind.Ipfs;
    }

    public static string ResolveRelative(string reference, string baseUri)
    {
        var normalizedReference = Normalize(reference);
        if (string.IsNullOrWhiteSpace(normalizedReference))
        {
            return normalizedReference;
        }

        if (ClassifyScheme(normalizedReference) is not SmlUriSchemeKind.Relative)
        {
            return normalizedReference;
        }

        var normalizedBase = Normalize(baseUri);
        if (string.IsNullOrWhiteSpace(normalizedBase))
        {
            return normalizedReference;
        }

        var baseKind = ClassifyScheme(normalizedBase);
        if (baseKind is SmlUriSchemeKind.Res or SmlUriSchemeKind.User)
        {
            return ResolveRelativeCustomScheme(normalizedReference, normalizedBase, baseKind == SmlUriSchemeKind.Res ? "res://" : "user://");
        }

        if (Uri.TryCreate(normalizedBase, UriKind.Absolute, out var baseAbsolute)
            && Uri.TryCreate(baseAbsolute, normalizedReference, out var combined))
        {
            return Normalize(combined.ToString());
        }

        if (Path.IsPathRooted(normalizedBase))
        {
            var directory = Path.GetDirectoryName(normalizedBase) ?? normalizedBase;
            return Path.GetFullPath(Path.Combine(directory, normalizedReference));
        }

        return normalizedReference;
    }

    public static string MapIpfsToHttp(string ipfsUri, string? gatewayBase = null)
    {
        var normalized = Normalize(ipfsUri);
        if (ClassifyScheme(normalized) != SmlUriSchemeKind.Ipfs)
        {
            throw new ArgumentException($"URI '{ipfsUri}' is not an ipfs URI.", nameof(ipfsUri));
        }

        var remainder = normalized["ipfs:/".Length..].TrimStart('/', '\\');
        if (string.IsNullOrWhiteSpace(remainder))
        {
            throw new ArgumentException("IPFS URI must contain a CID/path segment.", nameof(ipfsUri));
        }

        var gateway = string.IsNullOrWhiteSpace(gatewayBase) ? DefaultIpfsGateway : gatewayBase.Trim();
        gateway = gateway.TrimEnd('/');

        return $"{gateway}/{remainder}";
    }

    private static string ResolveRelativeCustomScheme(string relativeReference, string normalizedBase, string prefix)
    {
        var basePath = normalizedBase[prefix.Length..];
        var baseDir = basePath.EndsWith("/", StringComparison.Ordinal)
            ? basePath
            : basePath.Contains('/')
                ? basePath[..(basePath.LastIndexOf('/') + 1)]
                : string.Empty;

        var merged = MergePosixPath(baseDir, relativeReference);
        return prefix + merged;
    }

    private static string MergePosixPath(string baseDir, string relative)
    {
        var segments = new List<string>();

        void PushSegments(string raw)
        {
            foreach (var segment in raw.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }

                    continue;
                }

                segments.Add(segment);
            }
        }

        PushSegments(baseDir);
        PushSegments(relative);

        return string.Join('/', segments);
    }
}