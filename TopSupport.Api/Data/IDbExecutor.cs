using Microsoft.Data.SqlClient;

namespace TopSupport.Api.Data;

/// <summary>
/// Thin wrapper around SqlConnection/SqlCommand for calling stored procedures, so
/// repositories don't each re-implement connection handling and command setup.
/// </summary>
public interface IDbExecutor
{
    /// <summary>
    /// Executes a stored procedure and projects every result set in order via <paramref name="readSets"/>,
    /// one callback per SELECT the procedure returns (see usp_Ticket_GetById, which returns four).
    /// </summary>
    Task ExecuteReaderAsync(
        string storedProcedureName,
        Action<SqlCommand> configureParameters,
        CancellationToken cancellationToken,
        params Func<SqlDataReader, CancellationToken, Task>[] readSets);

    /// <summary>Executes a stored procedure that returns no result set (e.g. an insert/update).</summary>
    Task ExecuteNonQueryAsync(string storedProcedureName, Action<SqlCommand> configureParameters, CancellationToken cancellationToken = default);
}
