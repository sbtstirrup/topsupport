using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using TopSupport.Models.Admin;
using TopSupport.Services.Abstractions;

namespace TopSupport.Api.Data;

/// <summary>ADO.NET implementation of IWorkerMonitorRepository over worker.usp_Monitor_*.</summary>
public class WorkerMonitorRepository : IWorkerMonitorRepository
{
    private readonly DbExecutor _db;

    public WorkerMonitorRepository(DbExecutor db)
    {
        _db = db;
    }

    public async Task<WorkerJobsDto> GetJobsAsync(int recentRuns, CancellationToken cancellationToken = default)
    {
        var result = new WorkerJobsDto();

        await _db.ExecuteReaderAsync(
            "worker.usp_Monitor_GetJobs",
            cmd => cmd.Parameters.AddWithValue("@Top", recentRuns),
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var id = (string)reader["ServerId"];
                    var parts = id.Split(':');
                    result.Servers.Add(new WorkerServerDto
                    {
                        Id = id,
                        Host = parts.Length > 1 ? parts[1] : id,
                        LastHeartbeat = Utc((DateTime)reader["LastHeartbeat"]),
                        StartedAt = DateTimeOffset.TryParse(reader["StartedAt"] as string, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var started) ? started : null
                    });
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    result.RecurringJobs.Add(new RecurringJobDto
                    {
                        Id = (string)reader["RecurringJobId"],
                        Cron = reader["Cron"] as string ?? string.Empty,
                        TimeZone = reader["TimeZoneId"] as string ?? string.Empty,
                        LastRun = FromUnixMs(reader["LastExecutionMs"]),
                        NextRun = FromUnixMs(reader["NextExecutionMs"]),
                        LastState = reader["LastState"] as string,
                        LastError = reader["LastError"] as string,
                        ScheduleError = reader["ScheduleError"] as string
                    });
                }
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    result.RecentRuns.Add(new JobRunDto
                    {
                        JobId = (long)reader["JobId"],
                        RecurringJobId = reader["RecurringJobId"] as string,
                        State = reader["State"] as string ?? string.Empty,
                        CreatedAt = Utc((DateTime)reader["CreatedAt"]),
                        StateAt = reader["StateAt"] is DateTime at ? Utc(at) : null,
                        DurationMs = reader["DurationMs"] as long?,
                        Error = reader["Error"] as string
                    });
                }
            });

        return result;
    }

    public async Task<WorkerNotificationsDto> GetNotificationsAsync(string? eventType, string? search, int page, int pageSize, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        var result = new WorkerNotificationsDto();

        await _db.ExecuteReaderAsync(
            "worker.usp_Monitor_GetNotifications",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@EventType", (object?)eventType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Page", page);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.Add("@NowUtc", SqlDbType.DateTimeOffset).Value = nowUtc;
            },
            cancellationToken,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    result.Counts.Add(new NotificationCountDto
                    {
                        EventType = (string)reader["EventType"],
                        Last24Hours = (int)reader["Last24Hours"],
                        Last7Days = (int)reader["Last7Days"],
                        AllTime = (int)reader["AllTime"]
                    });
                }
            },
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct)) result.Total = (int)reader["Total"];
            },
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    result.Items.Add(new NotificationLogItemDto
                    {
                        Id = (long)reader["Id"],
                        SentAt = (DateTimeOffset)reader["SentAtUtc"],
                        TicketId = (string)reader["TicketId"],
                        TicketNumber = reader["TicketNumber"] as string,
                        Subject = reader["Subject"] as string,
                        EventType = (string)reader["EventType"],
                        EventKey = (string)reader["EventKey"],
                        Recipient = (string)reader["Recipient"]
                    });
                }
            });

        return result;
    }

    // Hangfire writes its DATETIME columns in UTC.
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? FromUnixMs(object value) => value is long ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;
}
