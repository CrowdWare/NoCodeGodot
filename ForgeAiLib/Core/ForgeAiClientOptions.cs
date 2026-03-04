namespace Forge.Ai.Core;

public sealed record ForgeAiClientOptions(
    string ApiKey,
    string BaseUrl = "https://api.x.ai/v1")
{
    public static ForgeAiClientOptions FromEnvironment(string envVar = "GROK_API_KEY", string? baseUrl = null)
    {
        var key = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ForgeAiException($"Missing API key environment variable '{envVar}'.");
        }

        return new ForgeAiClientOptions(key, baseUrl ?? "https://api.x.ai/v1");
    }
}
