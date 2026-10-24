using Microsoft.Data.SqlClient;

namespace TopSupport.Api.Data;

/// <summary>Creates open ADO.NET connections to the TopSupport database.</summary>
public interface ISqlConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
