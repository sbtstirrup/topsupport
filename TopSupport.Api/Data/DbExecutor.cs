using System.Data;
using Microsoft.Data.SqlClient;

namespace TopSupport.Api.Data;

public class DbExecutor : IDbExecutor
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DbExecutor(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task ExecuteReaderAsync(
        string storedProcedureName,
        Action<SqlCommand> configureParameters,
        CancellationToken cancellationToken,
        params Func<SqlDataReader, CancellationToken, Task>[] readSets)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = storedProcedureName;
        configureParameters(command);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        foreach (var readSet in readSets)
        {
            await readSet(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
        }
    }

    public async Task ExecuteNonQueryAsync(string storedProcedureName, Action<SqlCommand> configureParameters, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = storedProcedureName;
        configureParameters(command);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
