namespace Runtime.Sms;

public class ScriptError : Exception
{
    public ScriptError(string message, Position? position = null, Exception? inner = null)
        : base(position is null ? message : $"Error at {position}: {message}", inner)
    {
        Position = position;
    }

    public Position? Position { get; }
}

public sealed class LexError(string message, Position position, Exception? inner = null)
    : ScriptError(message, position, inner);

public sealed class ParseError(string message, Position? position = null, Exception? inner = null)
    : ScriptError(message, position, inner);

public sealed class RuntimeError(string message, Position? position = null, Exception? inner = null)
    : ScriptError(message, position, inner);
