using Elegance.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Tests.TestSupport;

// A real relational EF provider (not the InMemory provider, which does not support
// ExecuteUpdateAsync - and AdminAuthService's replay-prevention update relies on it). Uses a
// named, shared-cache in-memory database so CreateAdditionalContext() can open independent
// connections/DbContexts against the *same* data, for genuine concurrent-access tests - a plain
// "DataSource=:memory:" connection is private to that one connection. The anchor connection is
// kept open for the fixture's lifetime so the database isn't dropped once empty.
public sealed class SqliteTestDb : IDisposable
{
    private readonly string _connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _anchorConnection;
    public EleganceDbContext Context { get; }

    public SqliteTestDb()
    {
        _anchorConnection = new SqliteConnection(_connectionString);
        _anchorConnection.Open();

        Context = new EleganceDbContext(BuildOptions());
        Context.Database.EnsureCreated();
    }

    // An independent connection + DbContext against the same shared-cache database, for tests
    // that need two real concurrent operations (EF's DbContext is not thread-safe, so concurrent
    // work must never share one instance).
    public EleganceDbContext CreateAdditionalContext() => new(BuildOptions());

    private DbContextOptions<EleganceDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<EleganceDbContext>().UseSqlite(_connectionString).Options;

    public void Dispose()
    {
        Context.Dispose();
        _anchorConnection.Dispose();
    }
}
