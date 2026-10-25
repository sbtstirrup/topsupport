using System.Data;
using Microsoft.Data.SqlClient;

namespace TopSupport.Worker.Data;

/// <summary>
/// The worker's equivalent of TopSupport.Api's DbExecutor (that lives in
/// the Api project, so can't be referenced): runs stored procedures against the same
/// "ConnectionStrings:TopSupport" database. No EF Core, no Dapper, as in the rest of the solution.
/// </summary>
public sealed class WorkerDb
{
    private readonly string _connectionString;

    public WorkerDb(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TopSupport")
            ?? throw new InvalidOperationException("Missing \"ConnectionStrings:TopSupport\" configuration value.");
    }

    /// <summary>Runs the proc and hands each result set, in order, to the matching callback.</summary>
    public async Task ReadAsync(
        string storedProcedureName,
        Action<SqlParameterCollection> parameters,
        CancellationToken cancellationToken,
        params Func<SqlDataReader, Task>[] readSets)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = Create(connection, storedProcedureName, parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        foreach (var readSet in readSets)
        {
            await readSet(reader);
            await reader.NextResultAsync(cancellationToken);
        }
    }

    /// <summary>First column of the first row, or default when there's no row.</summary>
    public async Task<T?> ScalarAsync<T>(string storedProcedureName, Action<SqlParameterCollection> parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = Create(connection, storedProcedureName, parameters);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? default : (T)value;
    }

    public async Task ExecuteAsync(string storedProcedureName, Action<SqlParameterCollection> parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = Create(connection, storedProcedureName, parameters);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqlCommand Create(SqlConnection connection, string storedProcedureName, Action<SqlParameterCollection> parameters)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = storedProcedureName;
        parameters(command.Parameters);
        return command;
    }
}
