using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ForgeCli.Ai;

internal sealed class OpenAiCompatibleProvider : IAiProvider
{
    private static readonly HttpClient Http = new();

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string? _referer;
    private readonly string? _title;

    private OpenAiCompatibleProvider(string apiKey, string baseUrl, string model, string? referer = null, string? title = null)
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _referer = referer;
        _title = title;
    }

    public static OpenAiCompatibleProvider ForGrok(string? apiKey, string? model)
    {
        var key = ResolveApiKey(apiKey, "GROK_API_KEY", "grok");
        return new OpenAiCompatibleProvider(key, "https://api.x.ai/v1", model ?? "grok-4-latest");
    }

    public static OpenAiCompatibleProvider ForOpenRouter(string? apiKey, string? model)
    {
        var key = ResolveApiKey(apiKey, "OPENROUTER_API_KEY", "openrouter");
        return new OpenAiCompatibleProvider(
            key,
            "https://openrouter.ai/api/v1",
            model ?? "openai/gpt-4o-mini",
            "https://forge.local",
            "ForgeCli");
    }

    public async Task<GenerationCandidate> GenerateAsync(string prompt, string feedback, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = BuildUserPrompt(prompt, feedback) }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        if (!string.IsNullOrWhiteSpace(_referer))
        {
            request.Headers.TryAddWithoutValidation("HTTP-Referer", _referer);
        }

        if (!string.IsNullOrWhiteSpace(_title))
        {
            request.Headers.TryAddWithoutValidation("X-Title", _title);
        }

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await Http.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"AI provider request failed ({(int)response.StatusCode}): {content}");
        }

        var messageContent = ExtractMessageContent(content);
        var parsed = ParseCandidate(messageContent);
        return parsed with { Raw = messageContent };
    }

    private static string ResolveApiKey(string? supplied, string envName, string providerName)
    {
        var value = supplied;
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(envName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing API key for provider '{providerName}'. Use --api-key or set {envName}.");
        }

        return value;
    }

    private static string ExtractMessageContent(string completionJson)
    {
        using var doc = JsonDocument.Parse(completionJson);
        var root = doc.RootElement;
        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("AI response had no choices.");
        }

        var message = choices[0].GetProperty("message");
        var content = message.GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("AI response content is empty.");
        }

        return content;
    }

    private static GenerationCandidate ParseCandidate(string content)
    {
        var json = ExtractJsonObject(content);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("mainSml", out var mainSmlEl) || !root.TryGetProperty("mainSms", out var mainSmsEl))
        {
            throw new InvalidOperationException("AI response must provide JSON with 'mainSml' and 'mainSms'.");
        }

        var mainSml = mainSmlEl.GetString();
        var mainSms = mainSmsEl.GetString();
        if (string.IsNullOrWhiteSpace(mainSml) || string.IsNullOrWhiteSpace(mainSms))
        {
            throw new InvalidOperationException("AI response returned empty mainSml/mainSms.");
        }

        return new GenerationCandidate(mainSml, mainSms);
    }

    private static string BuildUserPrompt(string prompt, string feedback)
    {
        return $"""
Task:
{prompt}

Validation feedback from previous attempt (if any):
{feedback}

Return JSON only.
""";
    }

    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end < start)
        {
            throw new InvalidOperationException("AI response did not contain a JSON object.");
        }

        return text[start..(end + 1)];
    }

    private const string SystemPrompt =
"""
You are generating Forge UI files.
Return strict JSON with:
- mainSml: string
- mainSms: string
Rules:
- SML uses Godot class names directly.
- Keep syntax valid and deterministic.
- Prefer explicit IDs for interactive elements.
- Use SMS 'fun ready()' and 'on <id>.<event>()' handlers.
- Do not wrap JSON in markdown fences.
""";
}
