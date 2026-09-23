namespace EfCoreCompositeKeyJoin.Benchmark;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DbRecord> Records => Set<DbRecord>();

    public DbSet<JoinResult> JoinResults => Set<JoinResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbRecord>(entity =>
        {
            entity.ToTable("DbRecords");
            entity.HasKey(record => new { record.Key1, record.Key2 });
            entity.Property(record => record.Key2).HasMaxLength(50).IsUnicode(false);
            entity.Property(record => record.Payload).HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<JoinResult>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
            entity.Property(result => result.Key2).HasMaxLength(50).IsUnicode(false);
            entity.Property(result => result.Payload).HasMaxLength(100).IsUnicode(false);
        });
    }
}