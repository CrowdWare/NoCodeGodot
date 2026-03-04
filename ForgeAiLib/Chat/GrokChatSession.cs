using Forge.Ai.Core;

namespace Forge.Ai.Chat;

public sealed class GrokChatSession
{
    private readonly GrokChatService _service;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int? _maxTokens;
    private readonly List<(string Role, string Content)> _messages = [];

    public GrokChatSession(
        GrokChatService service,
        string model = "grok-4",
        double temperature = 0.0,
        int? maxTokens = 4096,
        string? systemPrompt = null)
    {
        _service = service;
        _model = model;
        _temperature = temperature;
        _maxTokens = maxTokens;

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            _messages.Add(("system", systemPrompt));
        }
    }

    public IReadOnlyList<(string Role, string Content)> Messages => _messages;

    public void ClearKeepSystemPrompt()
    {
        var system = _messages.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
        _messages.Clear();
        if (!string.IsNullOrWhiteSpace(system.Content))
        {
            _messages.Add(system);
        }
    }

    public async Task<string> AskAsync(string userPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new ForgeAiException("Prompt is required.");
        }

        _messages.Add(("user", userPrompt));

        var systemPrompt = _messages.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)).Content;
        var stitchedPrompt = string.Join(
            "\n\n",
            _messages.Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
                     .Select(m => $"{m.Role}: {m.Content}"));

        var result = await _service.CompleteAsync(new GrokChatRequest(
            Prompt: stitchedPrompt,
            SystemPrompt: systemPrompt,
            Model: _model,
            Temperature: _temperature,
            MaxTokens: _maxTokens), cancellationToken).ConfigureAwait(false);

        _messages.Add(("assistant", result.Content));
        return result.Content;
    }
}
