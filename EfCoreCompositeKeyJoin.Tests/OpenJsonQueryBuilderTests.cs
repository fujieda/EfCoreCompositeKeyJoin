namespace EfCoreCompositeKeyJoin.Tests;

public sealed class OpenJsonQueryBuilderTests
{
    [Fact]
    public void Build_generates_distinct_openjson_join_sql()
    {
        using var context = CreateContext();
        var builder = new OpenJsonQueryBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);

        var sql = builder.Build(
            context.Records,
            [new TestCompositeKey(1, "K00001")]).ToQueryString();

        Assert.Equal(
            """
            DECLARE @json nvarchar(28) = N'[{"Key1":1,"Key2":"K00001"}]';

            SELECT r.[Key1], r.[Key2], r.[Payload]
            FROM [dbo].[DbRecords] AS r
            INNER JOIN (
                SELECT DISTINCT [Key1], [Key2]
                FROM OPENJSON(@json)
                WITH (
                    [Key1] int '$.Key1',
                    [Key2] varchar(50) '$.Key2'
                )
            ) AS inputKeys
                ON r.[Key1] = inputKeys.[Key1]
                AND r.[Key2] = inputKeys.[Key2]
            """.TrimEnd(),
            sql.TrimEnd());
    }

    [Fact]
    public void Build_accepts_empty_key_collection()
    {
        using var context = CreateContext();
        var builder = new OpenJsonQueryBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);

        var query = builder.Build(context.Records, Array.Empty<TestCompositeKey>());

        Assert.Contains(
            "DECLARE @json nvarchar(2) = N'[]';",
            query.ToQueryString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Build_maps_different_entity_and_key_property_names()
    {
        using var context = CreateContext();
        var builder = new OpenJsonQueryBuilder<TestRecord, AlternateCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)],
            [nameof(AlternateCompositeKey.First), nameof(AlternateCompositeKey.Second)]);

        var sql = builder.Build(
            context.Records,
            [new AlternateCompositeKey(1, "K00001")]).ToQueryString();

        Assert.Contains("N'[{\"First\":1,\"Second\":\"K00001\"}]'", sql, StringComparison.Ordinal);
        Assert.Contains("[First] int '$.First'", sql, StringComparison.Ordinal);
        Assert.Contains("[Second] varchar(50) '$.Second'", sql, StringComparison.Ordinal);
        Assert.Contains("r.[Key1] = inputKeys.[First]", sql, StringComparison.Ordinal);
        Assert.Contains("r.[Key2] = inputKeys.[Second]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_rejects_null_source()
    {
        var builder = new OpenJsonQueryBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);

        Assert.Throws<ArgumentNullException>(() => builder.Build(null!, Array.Empty<TestCompositeKey>()));
    }

    [Fact]
    public void Build_rejects_null_keys()
    {
        using var context = CreateContext();
        var builder = new OpenJsonQueryBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);

        Assert.Throws<ArgumentNullException>(() => builder.Build(context.Records, null!));
    }

    [Fact]
    public void Build_rejects_null_key()
    {
        using var context = CreateContext();
        var builder = new OpenJsonQueryBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);
        TestCompositeKey[] keys = { null! };

        Assert.Throws<ArgumentNullException>(() => builder.Build(context.Records, keys));
    }

    [Fact]
    public void Build_rejects_unmapped_entity_property()
    {
        using var context = CreateContext();
        var builder = new OpenJsonQueryBuilder<TestRecord, TestCompositeKey>(
            ["Missing"],
            [nameof(TestCompositeKey.Key1)]);

        Assert.Throws<ArgumentException>(() => builder.Build(context.Records, Array.Empty<TestCompositeKey>()));
    }

    [Fact]
    public void Constructor_rejects_empty_property_lists()
    {
        Assert.Throws<ArgumentException>(() =>
            new OpenJsonQueryBuilder<TestRecord, TestCompositeKey>(Array.Empty<string>(), Array.Empty<string>()));
    }

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=EfCoreCompositeKeyJoinTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new TestDbContext(options);
    }

    private sealed record AlternateCompositeKey(int First, string Second);
}