using Microsoft.Data.SqlClient;

namespace TopSupport.Api.Data;

/// <summary>
/// Reads the "TopSupport" connection string from configuration (see
/// appsettings.Development.json) and hands out open connections. No EF Core, no Dapper —
/// callers execute stored procedures directly via SqlCommand (see IDbExecutor).
/// </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TopSupport")
            ?? throw new InvalidOperationException("Missing \"ConnectionStrings:TopSupport\" configuration value.");
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
