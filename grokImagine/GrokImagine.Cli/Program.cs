using GrokImagine.Client;
using System.Text.RegularExpressions;

class Program
{
    static async Task Main(string[] args)
    {
        string? apiKey = null, smlFile = null;

        if (args.Length == 1 && !args[0].StartsWith("--"))
        {
            smlFile = args[0];
        }
        else
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--apikey":
                        if (i + 1 < args.Length) apiKey = args[++i];
                        break;
                }
            }
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = Environment.GetEnvironmentVariable("GROK_API_KEY");
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API key required. Provide via --apikey or GROK_API_KEY environment variable.");
            return;
        }

        string? model = null, outputfile = null, prompt = null, negative = null, inputPose = null, inputStyle = null, inputExtra = null, aspect_ratio = null, resolution = null;
        double? imageStrength = null, guidanceScale = null, styleStrength = null;
        int? steps = null;

        if (!string.IsNullOrEmpty(smlFile))
        {
            if (!File.Exists(smlFile))
            {
                Console.WriteLine($"SML file not found: {smlFile}");
                return;
            }

            var smlContent = File.ReadAllText(smlFile);
            model = ExtractValue(smlContent, "model");
            prompt = ExtractValue(smlContent, "prompt");
            negative = ExtractValue(smlContent, "negativePrompt");
            inputPose = ExtractValue(smlContent, "inputPose");
            inputStyle = ExtractValue(smlContent, "inputStyle");
            inputExtra = ExtractValue(smlContent, "inputExtra");
            outputfile = ExtractValue(smlContent, "output");
            imageStrength = ExtractDouble(smlContent, "image_strength");
            guidanceScale = ExtractDouble(smlContent, "guidance_scale");
            styleStrength = ExtractDouble(smlContent, "style_strength");
            aspect_ratio = ExtractValue(smlContent, "aspect_ratio");
            resolution = ExtractValue(smlContent, "resolution");
            steps = ExtractInt(smlContent, "steps");

            if (string.IsNullOrEmpty(model) || string.IsNullOrEmpty(inputPose) || string.IsNullOrEmpty(outputfile) || string.IsNullOrEmpty(prompt))
            {
                Console.WriteLine("SML file must contain model, input, output, prompt.");
                return;
            }

            // Handle version in output
            if (outputfile.Contains("<version>"))
            {
                var baseName = outputfile.Replace("<version>", "");
                var dir = "output";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var files = Directory.GetFiles(dir, Path.GetFileName(baseName).Replace(".png", "_*.png"));
                int maxVersion = 0;
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var parts = name.Split('_');
                    if (parts.Length > 1 && int.TryParse(parts.Last(), out int v))
                    {
                        maxVersion = Math.Max(maxVersion, v);
                    }
                }
                outputfile = baseName.Replace(".png", $"_{maxVersion + 1}.png");
            }
        }
        else
        {
            // Old way, but simplified since user wants SML
            Console.WriteLine("Usage: dotnet run -- <sml-file> [--apikey <key>]");
            Console.WriteLine("Or use SML file with all parameters.");
            return;
        }

        var sourcePose = Path.Combine("input", inputPose);
        var sourceStyle = !string.IsNullOrEmpty(inputStyle) ? Path.Combine("input", inputStyle) : null;
        var sourceExtra = !string.IsNullOrEmpty(inputExtra) ? Path.Combine("input", inputExtra) : null;
        var output = Path.Combine("output", outputfile);

        var client = new GrokImagineClient();
        try
        {
            await client.GenerateImageAsync(apiKey, model, sourcePose, sourceStyle, sourceExtra, output, prompt, negative, imageStrength, guidanceScale, steps, aspect_ratio, resolution, styleStrength);
            Console.WriteLine("Image generated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string? ExtractValue(string content, string key)
    {
        var regex = new System.Text.RegularExpressions.Regex($@"{key}:\s*""([\s\S]*?)""", RegexOptions.Singleline);
        var match = regex.Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    static double? ExtractDouble(string content, string key)
    {
        var val = ExtractValue(content, key);
        return val != null && double.TryParse(val, out var d) ? d : null;
    }

    static int? ExtractInt(string content, string key)
    {
        var val = ExtractValue(content, key);
        return val != null && int.TryParse(val, out var i) ? i : null;
    }
}
