namespace EfCoreCompositeKeyJoin.Benchmark;

[LongRunJob]
[MemoryDiagnoser]
public class CompositeKeyJoinBenchmark
{
    private const int DatabaseRecordCount = 100_000;

    private AppDbContext _db = null!;
    private IReadOnlyList<CompositeKey> _keys = null!;

    [Params(100, 1_000, 10_000)]
    public int KeyCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var connectionString = Environment.GetEnvironmentVariable("EF_JOIN_CONNECTION_STRING")
            ?? "Server=localhost,14333;Database=EfCoreJoinBenchmark;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True;";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.CommandTimeout(120))
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        DatabaseInitializer.InitializeAsync(_db, DatabaseRecordCount).GetAwaiter().GetResult();
        _keys = Enumerable.Range(0, KeyCount)
            .Select(index => index * DatabaseRecordCount / KeyCount)
            .Select(index => new CompositeKey(index, $"K{index:D5}"))
            .ToArray();
    }

    [Benchmark(Baseline = true)]
    public List<DbRecord> ExpressionTree() =>
        JoinQueries.ExecuteExpressionTreeAsync(_db, _keys).GetAwaiter().GetResult();

    [Benchmark]
    public List<DbRecord> OpenJson() =>
        JoinQueries.ExecuteOpenJsonAsync(_db, _keys).GetAwaiter().GetResult();

    [GlobalCleanup]
    public void GlobalCleanup() => _db.Dispose();
}