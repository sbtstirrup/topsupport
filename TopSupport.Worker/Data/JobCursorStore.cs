using System.Data;

namespace TopSupport.Worker.Data;

/// <summary>ADO.NET implementation of IJobCursorStore over worker.usp_JobCursor_*.</summary>
public sealed class JobCursorStore : IJobCursorStore
{
    private readonly WorkerDb _db;

    public JobCursorStore(WorkerDb db)
    {
        _db = db;
    }

    public Task<DateTimeOffset?> GetAsync(string jobName, CancellationToken cancellationToken)
        => _db.ScalarAsync<DateTimeOffset?>("worker.usp_JobCursor_Get", p => p.AddWithValue("@JobName", jobName), cancellationToken);

    public Task SetAsync(string jobName, DateTimeOffset lastCompletedUtc, CancellationToken cancellationToken)
        => _db.ExecuteAsync("worker.usp_JobCursor_Set", p =>
        {
            p.AddWithValue("@JobName", jobName);
            p.Add("@LastCompletedUtc", SqlDbType.DateTimeOffset).Value = lastCompletedUtc;
        }, cancellationToken);
}
