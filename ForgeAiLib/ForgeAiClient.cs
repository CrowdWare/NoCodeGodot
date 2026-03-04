using Forge.Ai.Chat;
using Forge.Ai.Core;
using Forge.Ai.Imaging;
using Forge.Ai.Video;

namespace Forge.Ai;

public sealed class ForgeAiClient
{
    public ForgeAiClient(ForgeAiClientOptions options, HttpClient? httpClient = null)
    {
        Chat = new GrokChatService(options, httpClient);
        Imaging = new GrokImageService(options, httpClient);
        Video = new GrokVideoService(options, httpClient);
    }

    public GrokChatService Chat { get; }
    public GrokImageService Imaging { get; }
    public GrokVideoService Video { get; }
}
