namespace EfCoreCompositeKeyJoin.Benchmark;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        AppDbContext db,
        int recordCount,
        CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await db.Records.ExecuteDeleteAsync(cancellationToken);
        var records = Enumerable.Range(0, recordCount)
            .Select(index => new DbRecord
            {
                Key1 = index,
                Key2 = $"K{index:D5}",
                Payload = $"Payload-{index:D5}"
            });
        await db.Records.AddRangeAsync(records, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}