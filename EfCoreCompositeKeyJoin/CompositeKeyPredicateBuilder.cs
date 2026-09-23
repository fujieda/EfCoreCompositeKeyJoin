namespace EfCoreCompositeKeyJoin;

public sealed class CompositeKeyPredicateBuilder<TEntity, TKey>
    where TEntity : class
    where TKey : class
{
    private readonly PropertyInfo[] _entityProperties;
    private readonly Func<TKey, object?>[] _keyGetters;
    private readonly Type[] _keyPropertyTypes;

    public CompositeKeyPredicateBuilder(IReadOnlyList<string> propertyNames)
        : this(propertyNames, propertyNames)
    {
    }

    public CompositeKeyPredicateBuilder(
        IReadOnlyList<string> entityPropertyNames, IReadOnlyList<string> keyPropertyNames)
    {
        ArgumentNullException.ThrowIfNull(entityPropertyNames);
        ArgumentNullException.ThrowIfNull(keyPropertyNames);

        if (entityPropertyNames.Count == 0 || entityPropertyNames.Count != keyPropertyNames.Count)
        {
            throw new ArgumentException("Entity and key property lists must have the same non-zero length.");
        }
        _entityProperties = entityPropertyNames
            .Select(name => GetReadableProperty(typeof(TEntity), name))
            .ToArray();
        var keyProperties = keyPropertyNames
            .Select(name => GetReadableProperty(typeof(TKey), name))
            .ToArray();
        _keyPropertyTypes = keyProperties.Select(property => property.PropertyType).ToArray();
        _keyGetters = keyProperties.Select(property => CompileGetter<TKey>(property)).ToArray();
        for (var index = 0; index < _entityProperties.Length; index++)
        {
            if (_entityProperties[index].PropertyType != _keyPropertyTypes[index])
            {
                throw new ArgumentException(
                    $"Property types at index {index} must match: " +
                    $"{_entityProperties[index].PropertyType.Name} and {_keyPropertyTypes[index].Name}.");
            }
        }
    }

    public Expression<Func<TEntity, bool>> Build(IEnumerable<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var entityParameter = Expression.Parameter(typeof(TEntity), "entity");
        var matches = new List<Expression>();
        foreach (var key in keys)
        {
            ArgumentNullException.ThrowIfNull(key);
            var comparisons = new Expression[_entityProperties.Length];
            for (var index = 0; index < _entityProperties.Length; index++)
            {
                var property = _entityProperties[index];
                var propertyAccess = Expression.Property(entityParameter, property);
                var constant = Expression.Constant(_keyGetters[index](key), property.PropertyType);
                comparisons[index] = Expression.Equal(propertyAccess, constant);
            }
            matches.Add(BuildBalancedAnd(comparisons, 0, comparisons.Length));
        }
        var predicate = BuildBalancedOr(matches, 0, matches.Count)
            ?? Expression.Constant(false);
        return Expression.Lambda<Func<TEntity, bool>>(predicate, entityParameter);
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

    private static Func<T, object?> CompileGetter<T>(PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(T), "value");
        var access = Expression.Property(parameter, property);
        var boxed = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<T, object?>>(boxed, parameter).Compile();
    }

    private static Expression BuildBalancedAnd(IReadOnlyList<Expression> expressions, int start, int length)
    {
        if (length == 1)
        {
            return expressions[start];
        }
        var leftLength = length / 2;
        var rightLength = length - leftLength;
        return Expression.AndAlso(
            BuildBalancedAnd(expressions, start, leftLength),
            BuildBalancedAnd(expressions, start + leftLength, rightLength));
    }

    private static Expression? BuildBalancedOr(
        IReadOnlyList<Expression> expressions,
        int start,
        int length)
    {
        if (length == 0)
        {
            return null;
        }
        if (length == 1)
        {
            return expressions[start];
        }
        var leftLength = length / 2;
        var rightLength = length - leftLength;
        return Expression.OrElse(
            BuildBalancedOr(expressions, start, leftLength)!,
            BuildBalancedOr(expressions, start + leftLength, rightLength)!);
    }
}