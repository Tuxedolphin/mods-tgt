using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace Backend.Tests.Services;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(
        "postgres:16-alpine"
    ).Build();

    private Respawner _respawner = null!;

    public AppDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options
        );

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var setupConn = new NpgsqlConnection(_postgres.GetConnectionString());
        await setupConn.OpenAsync();

        // This has to be manually added since it the profiles table is excluded from migrations

        // This is the same as the original sql command, except all the parts
        // referencing Supabase.auth.users have been removed
        await using var cmd = setupConn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS public."Profiles" (
                "Id" uuid PRIMARY KEY,
                "Username" text
            );
            """;
        await cmd.ExecuteNonQueryAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(
            conn,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres, SchemasToInclude = ["public"] }
        );
    }
}
