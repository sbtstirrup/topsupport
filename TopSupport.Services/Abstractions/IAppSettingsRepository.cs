namespace TopSupport.Services.Abstractions;

/// <summary>Data-access contract for dbo.AppSettings (key/value), calling usp_AppSettings_GetByPrefix/usp_AppSetting_Set.</summary>
public interface IAppSettingsRepository
{
    /// <summary>Settings whose key starts with <paramref name="prefix"/>, keyed by the remainder (e.g. "security." -> "minLength").</summary>
    Task<IReadOnlyDictionary<string, string>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
}
