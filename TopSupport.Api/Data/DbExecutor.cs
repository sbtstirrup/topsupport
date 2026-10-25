using System.Data;
using Microsoft.Data.SqlClient;

namespace TopSupport.Api.Data;

/// <summary>
/// Calls the TopSupport stored procedures over ADO.NET (no EF Core, no Dapper -- see
/// TopSupport.Database/README.md), so repositories don't each re-implement connection handling
/// and command setup. The connection string is "ConnectionStrings:TopSupport".
/// </summary>
public class DbExecutor
{
    private readonly string _connectionString;

    public DbExecutor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TopSupport")
            ?? throw new InvalidOperationException("Missing \"ConnectionStrings:TopSupport\" configuration value.");
    }

    /// <summary>
    /// Executes a stored procedure and projects every result set in order via <paramref name="readSets"/>,
    /// one callback per SELECT the procedure returns (see usp_Ticket_GetById, which returns four).
    /// </summary>
    public async Task ExecuteReaderAsync(
        string storedProcedureName,
        Action<SqlCommand> configureParameters,
        CancellationToken cancellationToken,
        params Func<SqlDataReader, CancellationToken, Task>[] readSets)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, storedProcedureName, configureParameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        foreach (var readSet in readSets)
        {
            await readSet(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
        }
    }

    /// <summary>Executes a stored procedure that returns no result set (e.g. an insert/update).</summary>
    public async Task ExecuteNonQueryAsync(string storedProcedureName, Action<SqlCommand> configureParameters, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, storedProcedureName, configureParameters);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string storedProcedureName, Action<SqlCommand> configureParameters)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = storedProcedureName;
        configureParameters(command);
        return command;
    }
}
