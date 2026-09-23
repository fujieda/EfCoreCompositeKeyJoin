namespace EfCoreCompositeKeyJoin.Benchmark;

public static class JoinQueries
{
    private static readonly CompositeKeyPredicateBuilder<DbRecord, CompositeKey> CompositeKeyBuilder =
        new(
            [nameof(DbRecord.Key1), nameof(DbRecord.Key2)],
            [nameof(CompositeKey.Key1), nameof(CompositeKey.Key2)]);

    private static readonly OpenJsonQueryBuilder<DbRecord, CompositeKey> OpenJsonBuilder =
        new(
            [nameof(DbRecord.Key1), nameof(DbRecord.Key2)],
            [nameof(CompositeKey.Key1), nameof(CompositeKey.Key2)]);

    public static Task<List<DbRecord>> ExecuteExpressionTreeAsync(
        AppDbContext db,
        IReadOnlyCollection<CompositeKey> keys,
        CancellationToken cancellationToken = default)
    {
        var predicate = CompositeKeyBuilder.Build(keys);
        return db.Records
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public static Task<List<TEntity>> ExecuteExpressionTreeAsync<TEntity, TKey>(
        DbSet<TEntity> source,
        IReadOnlyCollection<TKey> keys,
        IReadOnlyList<string> entityPropertyNames,
        IReadOnlyList<string> keyPropertyNames,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TKey : class
    {
        var builder = new CompositeKeyPredicateBuilder<TEntity, TKey>(
            entityPropertyNames, keyPropertyNames);
        return source
            .AsNoTracking()
            .Where(builder.Build(keys))
            .ToListAsync(cancellationToken);
    }

    public static Task<List<DbRecord>> ExecuteOpenJsonAsync(
        AppDbContext db,
        IReadOnlyCollection<CompositeKey> keys,
        CancellationToken cancellationToken = default) =>
        OpenJsonBuilder.Build(db.Records, keys)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public static Task<List<DbRecord>> ExecuteOpenJsonAsync<TKey>(
        AppDbContext db,
        IReadOnlyCollection<TKey> keys,
        IReadOnlyList<string> propertyNames,
        CancellationToken cancellationToken = default)
        where TKey : class =>
        ExecuteOpenJsonAsync(db, keys, propertyNames, propertyNames, cancellationToken);

    public static Task<List<DbRecord>> ExecuteOpenJsonAsync<TKey>(
        AppDbContext db,
        IReadOnlyCollection<TKey> keys,
        IReadOnlyList<string> entityPropertyNames,
        IReadOnlyList<string> keyPropertyNames,
        CancellationToken cancellationToken = default)
        where TKey : class
    {
        var builder = new OpenJsonQueryBuilder<DbRecord, TKey>(entityPropertyNames, keyPropertyNames);
        return builder.Build(db.Records, keys)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public static Task<List<TEntity>> ExecuteOpenJsonAsync<TEntity, TKey>(
        DbSet<TEntity> source,
        IReadOnlyCollection<TKey> keys,
        IReadOnlyList<string> propertyNames,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TKey : class =>
        ExecuteOpenJsonAsync(source, keys, propertyNames, propertyNames, cancellationToken);

    public static Task<List<TEntity>> ExecuteOpenJsonAsync<TEntity, TKey>(
        DbSet<TEntity> source,
        IReadOnlyCollection<TKey> keys,
        IReadOnlyList<string> entityPropertyNames,
        IReadOnlyList<string> keyPropertyNames,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TKey : class
    {
        var builder = new OpenJsonQueryBuilder<TEntity, TKey>(entityPropertyNames, keyPropertyNames);
        return builder.Build(source, keys)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}