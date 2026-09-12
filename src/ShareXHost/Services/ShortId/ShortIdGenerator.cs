namespace ShareXHost;

public sealed class ShortIdGenerator : IShortIdGenerator
{
    private const int Length = 6;
    
    public string Generate()
    {
        return AlphaNumericRandomGenerator.Generate(Length);
    }
}