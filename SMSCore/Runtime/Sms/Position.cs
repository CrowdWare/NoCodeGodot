namespace Runtime.Sms;

public readonly record struct Position(int Line, int Column)
{
    public override string ToString() => $"line {Line}, column {Column}";
}
