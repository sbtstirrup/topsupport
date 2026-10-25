using System.Globalization;

namespace TopSupport.Blazor.Services;

/// <summary>
/// Ports of the demo's date/duration formatters used by the admin dashboard and audit trail:
/// fmtDur, fmtRel, fmtDT (dmy + time) and fmtH from topsupport-app.js. Times are shown in the
/// server's local zone, like the rest of the Blazor app's ToLocalTime() formatting.
/// </summary>
public static class DurationFormat
{
    /// <summary>fmtDate() in the demo: "05/Mar/2026".</summary>
    public static string Date(DateTimeOffset value)
        => value.ToLocalTime().ToString("dd'/'MMM'/'yyyy", CultureInfo.InvariantCulture);

    /// <summary>fmtDur(ms): "45m", "3h 20m" (minutes only under 10h), "26h", "3d 4h".</summary>
    public static string Duration(TimeSpan span)
    {
        var m = (int)Math.Round(Math.Abs(span.TotalMilliseconds) / 60000, MidpointRounding.AwayFromZero);
        if (m < 60) return m + "m";
        int h = m / 60, mm = m % 60;
        if (h < 48) return h + "h" + (mm != 0 && h < 10 ? " " + mm + "m" : string.Empty);
        int d = h / 24, hh = h % 24;
        return d + "d" + (hh != 0 ? " " + hh + "h" : string.Empty);
    }

    /// <summary>fmtRel(s): "just now" under a minute, otherwise "{fmtDur} ago".</summary>
    public static string Relative(DateTimeOffset at)
    {
        var elapsed = DateTimeOffset.UtcNow - at;
        return elapsed.TotalMilliseconds < 60000 ? "just now" : Duration(elapsed) + " ago";
    }

    // The demo's MON array (fixed English abbreviations -- en-GB culture data would give "Sept").
    private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>fmtDT(s): "24/Oct/2026, 14:05".</summary>
    public static string DateTime(DateTimeOffset at)
    {
        var local = at.ToLocalTime();
        return $"{local.Day:00}/{Months[local.Month - 1]}/{local.Year}, {local.Hour:00}:{local.Minute:00}";
    }

    /// <summary>Short day + month, e.g. "18 Jul" (the dashboard's week labels).</summary>
    public static string DayMonth(DateTimeOffset at)
    {
        var local = at.ToLocalTime();
        return $"{local.Day} {Months[local.Month - 1]}";
    }

    /// <summary>fmtH(v): "—" for null, minutes under an hour ("40m"), else one decimal ("2.5h").</summary>
    public static string Hours(double? hours) => hours switch
    {
        null => "—",
        < 1 => Math.Round(hours.Value * 60, MidpointRounding.AwayFromZero) + "m",
        _ => hours.Value.ToString("0.0", CultureInfo.InvariantCulture) + "h"
    };
}
