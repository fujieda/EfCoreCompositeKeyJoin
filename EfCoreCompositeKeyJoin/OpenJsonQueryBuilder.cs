namespace EfCoreCompositeKeyJoin;

public sealed class OpenJsonQueryBuilder<TEntity, TKey>
    where TEntity : class
    where TKey : class
{
    private static readonly ConcurrentDictionary<string, string> SqlShapes = new();
    private readonly IReadOnlyList<string> _entityPropertyNames;
    private readonly IReadOnlyList<string> _keyPropertyNames;

    public OpenJsonQueryBuilder(IReadOnlyList<string> propertyNames)
        : this(propertyNames, propertyNames)
    {
    }

    public OpenJsonQueryBuilder(IReadOnlyList<string> entityPropertyNames, IReadOnlyList<string> keyPropertyNames)
    {
        ArgumentNullException.ThrowIfNull(entityPropertyNames);
        ArgumentNullException.ThrowIfNull(keyPropertyNames);
        if (entityPropertyNames.Count == 0 || entityPropertyNames.Count != keyPropertyNames.Count)
        {
            throw new ArgumentException("Entity and key property lists must have the same non-zero length.");
        }
        _entityPropertyNames = entityPropertyNames.ToArray();
        _keyPropertyNames = keyPropertyNames.ToArray();
    }

    public IQueryable<TEntity> Build(DbSet<TEntity> source, IReadOnlyCollection<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in keys)
        {
            ArgumentNullException.ThrowIfNull(key);
        }
        var shape = SqlShapes.GetOrAdd(
            GetShapeKey(source.EntityType),
            _ => CreateSql(source.EntityType));
        var json = JsonSerializer.Serialize(keys);
        var parameter = new Microsoft.Data.SqlClient.SqlParameter("@json", json);
        return source.FromSqlRaw(shape, parameter);
    }

    private string GetShapeKey(IEntityType entityType) =>
        $"{entityType.Name}|{entityType.GetTableName()}|{entityType.GetSchema()}|{string.Join("|", _entityPropertyNames)}|{string.Join("|", _keyPropertyNames)}";

    private string CreateSql(IEntityType entityType)
    {
        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"No table mapping found for {typeof(TEntity).Name}.");
        var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
        var entityProperties = _entityPropertyNames
            .Select(name => entityType.FindProperty(name)
                ?? throw new ArgumentException($"Property '{name}' was not mapped on {typeof(TEntity).Name}."))
            .ToArray();
        var keyProperties = _keyPropertyNames
            .Select(name => GetReadableProperty(typeof(TKey), name))
            .ToArray();
        for (var index = 0; index < entityProperties.Length; index++)
        {
            if (entityProperties[index].ClrType != keyProperties[index].PropertyType)
            {
                throw new ArgumentException($"Property types at index {index} must match for OPENJSON mapping.");
            }
        }
        var selectColumns = string.Join(", ", entityType.GetProperties()
            .Select(property => $"r.{QuoteIdentifier(property.GetColumnName(storeObject))}"));
        var inputColumns = string.Join(",\n", entityProperties.Zip(keyProperties).Select(pair =>
            $"        {QuoteIdentifier(pair.Second.Name)} {GetSqlType(pair.First, storeObject)} '$.{pair.Second.Name}'"));
        var inputSelectColumns = string.Join(", ", keyProperties
            .Select(property => QuoteIdentifier(property.Name)));
        var joinConditions = string.Join("\n    AND ", entityProperties.Zip(keyProperties).Select(pair =>
            $"r.{QuoteIdentifier(pair.First.GetColumnName(storeObject))} = inputKeys.{QuoteIdentifier(pair.Second.Name)}"));
        return $"""
            SELECT {selectColumns}
            FROM {QuoteIdentifier(entityType.GetSchema() ?? "dbo")}.{QuoteIdentifier(tableName)} AS r
            INNER JOIN (
                SELECT DISTINCT {inputSelectColumns}
                FROM OPENJSON(@json)
                WITH (
            {inputColumns}
                )
            ) AS inputKeys
                ON {joinConditions}
            """;
    }

    private static PropertyInfo GetReadableProperty(Type type, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new ArgumentException($"Public property '{name}' was not found on {type.Name}.");
        if (!property.CanRead || property.GetMethod is null)
        {
            throw new ArgumentException($"Property '{type.Name}.{name}' must be readable.");
        }
        return property;
    }

    private static string QuoteIdentifier(string? name) =>
        $"[{(name ?? throw new InvalidOperationException("A mapped column name is required.")).Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string GetSqlType(IProperty property, StoreObjectIdentifier storeObject)
    {
        return property.GetColumnType(storeObject)
            ?? throw new InvalidOperationException($"No SQL column type found for '{property.Name}'.");
    }
}