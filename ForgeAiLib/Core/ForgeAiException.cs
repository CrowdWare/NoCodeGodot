namespace Forge.Ai.Core;

public sealed class ForgeAiException : Exception
{
    public ForgeAiException(string message) : base(message)
    {
    }

    public ForgeAiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
