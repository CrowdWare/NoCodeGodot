using Runtime.Sml;
using System.Text;

internal static class SmlConformance
{
    public static int Run(string fixtureDir)
    {
        if (!Directory.Exists(fixtureDir))
        {
            Console.WriteLine($"Conformance fixtures directory not found: {fixtureDir}");
            return 2;
        }

        if (!NativeSmlBridge.IsAstAvailable)
        {
            Console.WriteLine("Native SML AST API is not available. Set SML_NATIVE_LIB_DIR.");
            return 2;
        }

        var files = Directory.GetFiles(fixtureDir, "*.sml", SearchOption.TopDirectoryOnly)
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();
        if (files.Length == 0)
        {
            Console.WriteLine($"No *.sml fixtures found in: {fixtureDir}");
            return 2;
        }

        var failures = 0;
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var managedJson = BuildManagedAstJson(source);
            if (!NativeSmlBridge.TryParseAstJson(source, out var nativeJson))
            {
                failures++;
                Console.WriteLine($"[FAIL] {Path.GetFileName(file)}");
                Console.WriteLine($"  native parse failed: {NativeSmlBridge.LastError}");
                continue;
            }

            if (!string.Equals(managedJson, nativeJson, StringComparison.Ordinal))
            {
                failures++;
                Console.WriteLine($"[FAIL] {Path.GetFileName(file)}");
                Console.WriteLine("  AST mismatch between managed and native.");
                continue;
            }

            Console.WriteLine($"[OK]   {Path.GetFileName(file)}");
        }

        Console.WriteLine();
        Console.WriteLine($"Conformance: {files.Length - failures}/{files.Length} passing");
        return failures == 0 ? 0 : 1;
    }

    private static string BuildManagedAstJson(string source)
    {
        var parser = new SmlParser(source);
        var document = parser.ParseDocument();

        var sb = new StringBuilder();
        sb.Append("{\"roots\":[");
        for (var i = 0; i < document.Roots.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            AppendNodeJson(sb, document.Roots[i]);
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static void AppendNodeJson(StringBuilder sb, SmlNode node)
    {
        sb.Append("{\"name\":\"").Append(JsonEscape(node.Name)).Append("\",\"properties\":[");
        var first = true;
        foreach (var (name, value) in node.Properties)
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;
            AppendPropertyJson(sb, name, value);
        }
        sb.Append("],\"children\":[");
        for (var i = 0; i < node.Children.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            AppendNodeJson(sb, node.Children[i]);
        }
        sb.Append("]}");
    }

    private static void AppendPropertyJson(StringBuilder sb, string name, SmlValue value)
    {
        var kind = value.Kind switch
        {
            SmlValueKind.String => "string",
            SmlValueKind.Bool => "bool",
            SmlValueKind.Int => "number",
            SmlValueKind.Float => "number",
            SmlValueKind.Identifier => "identifier",
            _ => "unsupported"
        };

        var valueText = value.Kind switch
        {
            SmlValueKind.String => (string)value.Value,
            SmlValueKind.Bool => ((bool)value.Value) ? "true" : "false",
            SmlValueKind.Int => ((int)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture),
            SmlValueKind.Float => ((double)value.Value).ToString("0.################", System.Globalization.CultureInfo.InvariantCulture),
            SmlValueKind.Identifier => (string)value.Value,
            _ => "<unsupported>"
        };

        sb.Append("{\"name\":\"").Append(JsonEscape(name))
            .Append("\",\"kind\":\"").Append(kind)
            .Append("\",\"value\":\"").Append(JsonEscape(valueText))
            .Append("\"}");
    }

    private static string JsonEscape(string input)
    {
        var sb = new StringBuilder(input.Length + 8);
        foreach (var c in input)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}

