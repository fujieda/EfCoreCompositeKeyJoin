# EF Core Composite Key Join

This repository contains a reusable EF Core library and a BenchmarkDotNet sample that compare two ways to match an in-memory set of two-column keys with SQL Server records:

- Expression tree expansion with key values embedded as expression constants.
- SQL Server `OPENJSON` with the complete key set passed as one JSON parameter.

## Projects

| Project | Purpose |
| --- | --- |
| `EfCoreCompositeKeyJoin` | Reusable class library containing `CompositeKeyPredicateBuilder` and `OpenJsonQueryBuilder`. |
| `EfCoreCompositeKeyJoin.Benchmark` | BenchmarkDotNet executable, SQL Server context, sample data, and benchmark scenarios. |
| `EfCoreCompositeKeyJoin.Tests` | Unit tests for the reusable library. |

## Use the library

Reference `EfCoreCompositeKeyJoin` from an EF Core application and import its namespace:

```csharp
using EfCoreCompositeKeyJoin;
```

`CompositeKeyPredicateBuilder<TEntity, TKey>` builds an expression for use with LINQ queries. Entity and key property names are matched by position, and their CLR types must match.

```csharp
var builder = new CompositeKeyPredicateBuilder<DbRecord, CompositeKey>(
	["Key1", "Key2"]);

var records = await db.Records
	.Where(builder.Build(keys))
	.ToListAsync();
```

`OpenJsonQueryBuilder<TEntity, TKey>` generates a SQL Server `OPENJSON` query for a `DbSet<TEntity>`. It requires a SQL Server provider and passes the complete key collection as one JSON parameter.

```csharp
var builder = new OpenJsonQueryBuilder<DbRecord, CompositeKey>(
	["Key1", "Key2"]);

var records = await builder.Build(db.Records, keys).ToListAsync();
```

## Prerequisites

- .NET 10 SDK
- Docker Desktop for the benchmark database

Start SQL Server:

```powershell
docker compose up -d
```

Run the benchmark:

```powershell
$env:EF_JOIN_CONNECTION_STRING = "Server=localhost,14333;Database=EfCoreJoinBenchmark;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True;"
dotnet run --project .\EfCoreCompositeKeyJoin.Benchmark -c Release
```

Run the library tests:

```powershell
dotnet test .\EfCoreCompositeKeyJoin.Tests
```

The expression-tree approach does not create one SQL parameter per key. The `OPENJSON` approach passes the serialized key set as one SQL parameter. The 2,100-parameter SQL Server limit is therefore not the per-key limit for either implementation shown here.