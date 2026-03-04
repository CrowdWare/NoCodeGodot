namespace Forge.Ai.Core;

public sealed record ForgeAiClientOptions(
    string ApiKey,
    string BaseUrl = "https://api.x.ai/v1")
{
    public static ForgeAiClientOptions FromEnvironment(string envVar = "GROK_API_KEY", string? baseUrl = null)
    {
        var key = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(key) && !string.Equals(envVar, "XAI_API_KEY", StringComparison.Ordinal))
        {
            key = Environment.GetEnvironmentVariable("XAI_API_KEY");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ForgeAiException($"Missing API key environment variable '{envVar}' (or fallback 'XAI_API_KEY').");
        }

        return new ForgeAiClientOptions(key, baseUrl ?? "https://api.x.ai/v1");
    }
}
