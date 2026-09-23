namespace EfCoreCompositeKeyJoin.Tests;

public sealed class CompositeKeyPredicateBuilderTests
{
    [Fact]
    public void Build_matches_all_key_columns_and_rows()
    {
        var builder = new CompositeKeyPredicateBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);
        var predicate = builder.Build(
        [
            new TestCompositeKey(1, "K00001"),
            new TestCompositeKey(3, "K00003")
        ]).Compile();

        Assert.True(predicate(new TestRecord { Key1 = 1, Key2 = "K00001" }));
        Assert.True(predicate(new TestRecord { Key1 = 3, Key2 = "K00003" }));
        Assert.False(predicate(new TestRecord { Key1 = 1, Key2 = "K00003" }));
        Assert.False(predicate(new TestRecord { Key1 = 2, Key2 = "K00002" }));
    }

    [Fact]
    public void Build_with_no_keys_returns_false_predicate()
    {
        var builder = new CompositeKeyPredicateBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);

        var predicate = builder.Build([]).Compile();

        Assert.False(predicate(new TestRecord { Key1 = 1, Key2 = "K00001" }));
    }

    [Fact]
    public void Build_rejects_null_key()
    {
        var builder = new CompositeKeyPredicateBuilder<TestRecord, TestCompositeKey>(
            [nameof(TestRecord.Key1), nameof(TestRecord.Key2)]);

        Assert.Throws<ArgumentNullException>(() => builder.Build([null!]));
    }

    [Fact]
    public void Constructor_rejects_mismatched_property_lists()
    {
        Assert.Throws<ArgumentException>(() =>
            new CompositeKeyPredicateBuilder<TestRecord, TestCompositeKey>(
                [nameof(TestRecord.Key1)],
                [nameof(TestCompositeKey.Key1), nameof(TestCompositeKey.Key2)]));
    }

    [Fact]
    public void Constructor_rejects_missing_property()
    {
        Assert.Throws<ArgumentException>(() =>
            new CompositeKeyPredicateBuilder<TestRecord, TestCompositeKey>(
                ["Missing"],
                [nameof(TestCompositeKey.Key1)]));
    }

    [Fact]
    public void Constructor_rejects_property_type_mismatch()
    {
        Assert.Throws<ArgumentException>(() =>
            new CompositeKeyPredicateBuilder<TestRecord, TestCompositeKey>(
                [nameof(TestRecord.Key1)],
                [nameof(TestCompositeKey.Key2)]));
    }
}