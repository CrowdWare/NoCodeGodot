namespace ForgeCli.Core;

internal enum AiProviderType
{
    Mock,
    Grok,
    OpenRouter
}

internal sealed record NewCommandOptions(string ProjectName, string OutputDirectory, bool Force)
{
    public static NewCommandOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException("Missing project name. Usage: forgecli new <name> [--output <dir>] [--force]");
        }

        var projectName = args[0];
        var outputDirectory = Path.Combine(Environment.CurrentDirectory, projectName);
        var force = false;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                    i++;
                    outputDirectory = ReadValue(args, i, "--output");
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown option '{args[i]}' for 'new'.");
            }
        }

        return new NewCommandOptions(projectName, outputDirectory, force);
    }

    private static string ReadValue(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        return args[index];
    }
}

internal sealed record ValidateCommandOptions(string ProjectDirectory, bool Verbose)
{
    public static ValidateCommandOptions Parse(string[] args)
    {
        var projectDir = Environment.CurrentDirectory;
        var verbose = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--project":
                    i++;
                    projectDir = ReadValue(args, i, "--project");
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown option '{args[i]}' for 'validate'.");
            }
        }

        return new ValidateCommandOptions(projectDir, verbose);
    }

    private static string ReadValue(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        return args[index];
    }
}

internal sealed record GenerateCommandOptions(
    string ProjectDirectory,
    string Prompt,
    AiProviderType Provider,
    string? Model,
    string? ApiKey,
    int MaxIterations,
    bool DryRun)
{
    public static GenerateCommandOptions Parse(string[] args)
    {
        var projectDir = Environment.CurrentDirectory;
        string? prompt = null;
        var provider = AiProviderType.Mock;
        string? model = null;
        string? apiKey = null;
        var maxIterations = 4;
        var dryRun = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--project":
                    i++;
                    projectDir = ReadValue(args, i, "--project");
                    break;
                case "--prompt":
                    i++;
                    prompt = ReadValue(args, i, "--prompt");
                    break;
                case "--provider":
                    i++;
                    provider = ParseProvider(ReadValue(args, i, "--provider"));
                    break;
                case "--model":
                    i++;
                    model = ReadValue(args, i, "--model");
                    break;
                case "--api-key":
                    i++;
                    apiKey = ReadValue(args, i, "--api-key");
                    break;
                case "--max-iterations":
                    i++;
                    maxIterations = int.Parse(ReadValue(args, i, "--max-iterations"));
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown option '{args[i]}' for 'generate'.");
            }
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException("Missing --prompt for 'generate'.");
        }

        if (maxIterations < 1)
        {
            throw new InvalidOperationException("--max-iterations must be >= 1.");
        }

        return new GenerateCommandOptions(projectDir, prompt, provider, model, apiKey, maxIterations, dryRun);
    }

    private static AiProviderType ParseProvider(string raw)
    {
        return raw.Trim().ToLowerInvariant() switch
        {
            "mock" => AiProviderType.Mock,
            "grok" => AiProviderType.Grok,
            "openrouter" => AiProviderType.OpenRouter,
            _ => throw new InvalidOperationException($"Unsupported provider '{raw}'.")
        };
    }

    private static string ReadValue(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for {option}.");
        }

        return args[index];
    }
}
