using ForgeCli.Ai;

namespace ForgeCli.Core;

internal static class CommandRouter
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        try
        {
            return command switch
            {
                "new" => RunNew(rest),
                "validate" => RunValidate(rest),
                "generate" => await RunGenerateAsync(rest),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"forgecli error: {ex.Message}");
            return 1;
        }
    }

    private static int RunNew(string[] args)
    {
        var options = NewCommandOptions.Parse(args);
        ProjectScaffolder.Create(options.ProjectName, options.OutputDirectory, options.Force);
        Console.WriteLine($"Created Forge project scaffold at '{Path.GetFullPath(options.OutputDirectory)}'.");
        Console.WriteLine("Next: run `forgecli validate --project <path>`.");
        return 0;
    }

    private static int RunValidate(string[] args)
    {
        var options = ValidateCommandOptions.Parse(args);
        var report = ValidationService.ValidateProject(options.ProjectDirectory);
        ValidationService.PrintReport(report, options.Verbose);
        return report.IsValid ? 0 : 2;
    }

    private static async Task<int> RunGenerateAsync(string[] args)
    {
        var options = GenerateCommandOptions.Parse(args);
        var projectRoot = Path.GetFullPath(options.ProjectDirectory);
        if (!Directory.Exists(projectRoot))
        {
            throw new InvalidOperationException($"Project directory does not exist: {projectRoot}");
        }

        IAiProvider provider = options.Provider switch
        {
            AiProviderType.Mock => new MockAiProvider(),
            AiProviderType.Grok => OpenAiCompatibleProvider.ForGrok(options.ApiKey, options.Model),
            AiProviderType.OpenRouter => OpenAiCompatibleProvider.ForOpenRouter(options.ApiKey, options.Model),
            _ => throw new InvalidOperationException($"Unsupported provider '{options.Provider}'.")
        };

        var runner = new GenerationRunner(provider);
        var result = await runner.RunAsync(new GenerationRequest(
            projectRoot,
            options.Prompt,
            options.MaxIterations,
            options.DryRun));

        GenerationRunner.PrintResult(result);
        return result.Success ? 0 : 3;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.\n");
        PrintHelp();
        return 1;
    }

    private static bool IsHelp(string arg)
    {
        var value = arg.Trim().ToLowerInvariant();
        return value is "help" or "--help" or "-h";
    }

    private static void PrintHelp()
    {
        Console.WriteLine("ForgeCli - scaffold, validate, and AI-generate Forge apps");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  forgecli new <name> [--output <dir>] [--force]");
        Console.WriteLine("  forgecli validate [--project <dir>] [--verbose]");
        Console.WriteLine("  forgecli generate --prompt <text> [--project <dir>] [--provider mock|grok|openrouter]");
        Console.WriteLine("                   [--model <name>] [--api-key <key>] [--max-iterations <n>] [--dry-run]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  forgecli new HelloDock");
        Console.WriteLine("  forgecli validate --project ./HelloDock");
        Console.WriteLine("  forgecli generate --project ./HelloDock --provider mock --prompt \"window with docking and centered viewport3d\"");
    }
}
