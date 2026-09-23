namespace EfCoreCompositeKeyJoin.Tests;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestRecord> Records => Set<TestRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestRecord>(entity =>
        {
            entity.ToTable("DbRecords");
            entity.HasKey(record => new { record.Key1, record.Key2 });
            entity.Property(record => record.Key2).HasMaxLength(50).IsUnicode(false);
            entity.Property(record => record.Payload).HasMaxLength(100).IsUnicode(false);
        });
    }
}

public sealed class TestRecord
{
    public int Key1 { get; set; }

    public string Key2 { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
}

public sealed record TestCompositeKey(int Key1, string Key2);