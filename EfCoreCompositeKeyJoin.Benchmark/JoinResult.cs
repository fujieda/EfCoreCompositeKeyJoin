namespace EfCoreCompositeKeyJoin.Benchmark;

public sealed class JoinResult
{
    public int Key1 { get; set; }

    public string Key2 { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
}