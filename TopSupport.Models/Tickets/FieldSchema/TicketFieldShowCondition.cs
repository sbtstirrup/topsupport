namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>
/// The comparison a show-condition uses against another field's current value.
/// </summary>
public enum TicketFieldShowOperator
{
    /// <summary>True if the other field's value is "truthy" in the JS sense: not null/empty
    /// string/empty array/false (e.g. `show: x => !!x.deviceType`).</summary>
    Truthy,

    /// <summary>True if the other field's value equals one of EqualsAnyValues
    /// (e.g. `show: x => x.device === 'notlisted'`, or the two-value mailbox check).</summary>
    EqualsAny
}

/// <summary>
/// Mirrors every `show:` predicate in topicFields() (topsupport-app.js): every one of them is
/// a simple truthiness check or an equals-one-of check against exactly one other field, so no
/// general expression language is needed.
/// </summary>
public class TicketFieldShowCondition
{
    public string DependsOnKey { get; set; } = string.Empty;
    public TicketFieldShowOperator Operator { get; set; }
    public List<string>? EqualsAnyValues { get; set; }

    /// <summary>Evaluates this condition against a set of already-submitted field values.</summary>
    public bool Evaluate(IReadOnlyDictionary<string, object?> fields)
    {
        fields.TryGetValue(DependsOnKey, out var raw);

        if (Operator == TicketFieldShowOperator.Truthy)
        {
            return IsTruthy(raw);
        }

        var current = raw?.ToString();
        return current != null && EqualsAnyValues != null && EqualsAnyValues.Contains(current);
    }

    private static bool IsTruthy(object? value)
    {
        switch (value)
        {
            case null:
                return false;
            // Values posted to the Api arrive as JsonElement: apply JS truthiness to the JSON
            // value itself, so "", [], false, 0 and null don't count as answered.
            case System.Text.Json.JsonElement json:
                return json.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => json.GetString()!.Length > 0,
                    System.Text.Json.JsonValueKind.Array => json.GetArrayLength() > 0,
                    System.Text.Json.JsonValueKind.Number => json.TryGetDouble(out var n) && n != 0,
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.Object => true,
                    _ => false
                };
            case string s:
                return s.Length > 0;
            case System.Collections.IEnumerable enumerable and not string:
                foreach (var _ in enumerable) return true;
                return false;
            case bool b:
                return b;
            default:
                return true;
        }
    }
}
