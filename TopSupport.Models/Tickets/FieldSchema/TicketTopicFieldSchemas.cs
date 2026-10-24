namespace TopSupport.Models.Tickets.FieldSchema;

/// <summary>
/// Per-topic dynamic field lists, hand-ported field-for-field from topicFields() in
/// topsupport-app.js (lines ~879-951). This is the single source of truth both the Blazor
/// forms and ITicketFormValidator consume — see the plan's "Key shared-contract decision"
/// section for why this lives once, in C#, instead of being duplicated per-language.
///
/// Fields present in every topic's form, matching the demo's shared `impact`/`banner`/`files`
/// descriptors reused across topicFields() branches, are built once by the private helpers
/// below and spliced into each topic's list in the same position the JS uses them.
/// </summary>
public static class TicketTopicFieldSchemas
{
    private static TicketFieldDescriptor Impact() => new()
    {
        Key = "impact",
        Label = "How Much Is It Affecting Work?",
        Type = TicketFieldType.Radio,
        Required = true,
        StaticOptions = TicketFieldOptionSets.IMPACT.ToList()
    };

    /// <summary>The "call the Service Desk" banner shown when impact == "cant".</summary>
    private static TicketFieldDescriptor UrgentBanner() => new()
    {
        Key = "_urgent",
        Label = string.Empty,
        Type = TicketFieldType.Banner,
        ShowWhen = new TicketFieldShowCondition
        {
            DependsOnKey = "impact",
            Operator = TicketFieldShowOperator.EqualsAny,
            EqualsAnyValues = new List<string> { "cant" }
        }
    };

    private static TicketFieldDescriptor Attachments() => new()
    {
        Key = "attachments",
        Label = "Attachments (Optional)",
        Type = TicketFieldType.Files,
        Hint = "Photos of damage or screenshots of errors."
    };

    /// <summary>
    /// Returns the field list for one topic id, mirroring topicFields(tp, f, aff) in the demo.
    /// Fields whose options depend on runtime data are returned with OptionsSource set and
    /// StaticOptions left null/empty — ITicketFieldOptionsResolver fills those in.
    /// </summary>
    public static List<TicketFieldDescriptor> GetFields(string topic) => topic switch
    {
        "hardware" => Hardware(),
        "software" => Software(),
        "leaver" => Leaver(),
        "newuser" => NewUser(),
        "server" => Server(),
        "vpn" => Vpn(),
        _ => General()
    };

    private static List<TicketFieldDescriptor> Hardware() => new()
    {
        new TicketFieldDescriptor
        {
            Key = "deviceType",
            Label = "Device Type",
            Type = TicketFieldType.Select,
            Required = true,
            StaticOptions = TicketFieldOptionSets.FromStrings(TicketFieldOptionSets.DEVICE_TYPES)
        },
        new TicketFieldDescriptor
        {
            Key = "device",
            Label = "Device (Optional)",
            Type = TicketFieldType.Select,
            OptionsSource = TicketFieldOptionsSource.AffectedUserAssetsByDeviceType,
            // Hint and option list both depend on deviceType/the resolved asset list; the
            // resolver fills in the final hint wording (mirrors the ternary in the demo).
            ShowWhen = new TicketFieldShowCondition
            {
                DependsOnKey = "deviceType",
                Operator = TicketFieldShowOperator.Truthy
            }
        },
        new TicketFieldDescriptor
        {
            Key = "deviceTag",
            Label = "Asset Tag (Optional)",
            Type = TicketFieldType.Text,
            Placeholder = "For example TCB-LT-0142",
            Hint = "Printed on a silver sticker underneath the device.",
            ShowWhen = new TicketFieldShowCondition
            {
                DependsOnKey = "device",
                Operator = TicketFieldShowOperator.EqualsAny,
                EqualsAnyValues = new List<string> { "notlisted" }
            }
        },
        new TicketFieldDescriptor
        {
            Key = "problem",
            Label = "What's the Problem?",
            Type = TicketFieldType.Select,
            Required = true,
            StaticOptions = TicketFieldOptionSets.FromStrings(TicketFieldOptionSets.PROBLEMS)
        },
        new TicketFieldDescriptor
        {
            Key = "started",
            Label = "When Did It Start?",
            Type = TicketFieldType.Select,
            Required = true,
            StaticOptions = TicketFieldOptionSets.FromStrings(TicketFieldOptionSets.STARTED)
        },
        Impact(),
        UrgentBanner(),
        new TicketFieldDescriptor
        {
            Key = "tried",
            Label = "What Have You Already Tried?",
            Type = TicketFieldType.Checks,
            StaticOptions = TicketFieldOptionSets.FromStrings(TicketFieldOptionSets.TRIED)
        },
        new TicketFieldDescriptor
        {
            Key = "details",
            Label = "Issue Details",
            Type = TicketFieldType.TextArea,
            Required = true,
            Placeholder = "What happens, any error messages, and when it happens"
        },
        Attachments(),
        new TicketFieldDescriptor
        {
            Key = "location",
            Label = "Where Can We Find the Device?",
            Type = TicketFieldType.Radio,
            Required = true,
            // The demo fills in per-option descriptions dynamically (the affected user's
            // office, or a prompt to choose the affected user first) — StaticOptions here
            // carry placeholder descriptions; the resolver/service layer may refine them.
            StaticOptions = new List<TicketFieldOption>
            {
                new() { Value = "desk", Label = "The affected user's desk", Description = "Choose the affected user first." },
                new() { Value = "other", Label = "Different location", Description = "Tell us where the device is." },
                new() { Value = "remote", Label = "Remote — will need collecting or a courier", Description = "We will arrange collection from a home address." }
            }
        },
        new TicketFieldDescriptor
        {
            Key = "locationOther",
            Label = "Where Is the Device?",
            Type = TicketFieldType.Text,
            Required = true,
            Placeholder = "For example Manchester meeting room 3",
            ShowWhen = new TicketFieldShowCondition
            {
                DependsOnKey = "location",
                Operator = TicketFieldShowOperator.EqualsAny,
                EqualsAnyValues = new List<string> { "other" }
            }
        },
        new TicketFieldDescriptor
        {
            Key = "visitTime",
            Label = "Best Time for a Technician Visit (Optional)",
            Type = TicketFieldType.Text,
            Placeholder = "For example weekday mornings before 11am"
        }
    };

    private static List<TicketFieldDescriptor> Software() => new()
    {
        new TicketFieldDescriptor
        {
            Key = "appName",
            Label = "Application Name",
            Type = TicketFieldType.Text,
            Required = true,
            Placeholder = "For example Microsoft Outlook"
        },
        new TicketFieldDescriptor
        {
            Key = "version",
            Label = "Version, If Known",
            Type = TicketFieldType.Text,
            Placeholder = "Shown under Help › About"
        },
        new TicketFieldDescriptor
        {
            Key = "errorMsg",
            Label = "Error Message",
            Type = TicketFieldType.TextArea,
            Placeholder = "Copy the exact wording if you can",
            Rows = 2
        },
        Impact(),
        UrgentBanner(),
        new TicketFieldDescriptor
        {
            Key = "details",
            Label = "Issue Details",
            Type = TicketFieldType.TextArea,
            Required = true,
            Placeholder = "What were you doing when the problem happened?"
        },
        Attachments()
    };

    private static List<TicketFieldDescriptor> Leaver() => new()
    {
        new TicketFieldDescriptor
        {
            Key = "leavingDate",
            Label = "Leaving Date",
            Type = TicketFieldType.Date,
            Required = true
        },
        new TicketFieldDescriptor
        {
            Key = "lineManager",
            Label = "Line Manager",
            Type = TicketFieldType.Select,
            Required = true,
            OptionsSource = TicketFieldOptionsSource.ActiveUsersExcludingAffected
        },
        new TicketFieldDescriptor
        {
            Key = "returnAssets",
            Label = "Equipment to Return",
            Type = TicketFieldType.Checks,
            OptionsSource = TicketFieldOptionsSource.AffectedUserAssets
            // Hint ("Equipment assigned to {name}.") and EmptyMessage ("{name} has no
            // equipment in the asset list.") are filled in by the resolver, which knows
            // the affected user's name.
        },
        new TicketFieldDescriptor
        {
            Key = "mailbox",
            Label = "Mailbox Handling",
            Type = TicketFieldType.Radio,
            Required = true,
            StaticOptions = new List<TicketFieldOption>
            {
                new() { Value = "Forward to someone", Label = "Forward to someone", Description = "New emails go to another person for 90 days." },
                new() { Value = "Archive", Label = "Archive", Description = "The mailbox is closed and kept for 7 years." },
                new() { Value = "Delegate access", Label = "Delegate access", Description = "Another person can open the mailbox for 90 days." }
            }
        },
        new TicketFieldDescriptor
        {
            // Label switches between "Give mailbox access to" (Delegate access) and "Forward
            // emails to" (Forward to someone) in the demo based on the current `mailbox`
            // value; callers needing the exact dynamic label should special-case this key.
            Key = "mailboxTarget",
            Label = "Forward emails to",
            Type = TicketFieldType.Select,
            Required = true,
            OptionsSource = TicketFieldOptionsSource.ActiveUsersExcludingAffected,
            ShowWhen = new TicketFieldShowCondition
            {
                DependsOnKey = "mailbox",
                Operator = TicketFieldShowOperator.EqualsAny,
                EqualsAnyValues = new List<string> { "Forward to someone", "Delegate access" }
            }
        },
        new TicketFieldDescriptor
        {
            Key = "details",
            Label = "Anything Else We Should Know? (Optional)",
            Type = TicketFieldType.TextArea,
            Placeholder = "For example where to collect equipment from",
            Rows = 3
        }
    };

    private static List<TicketFieldDescriptor> NewUser() => new()
    {
        new TicketFieldDescriptor { Key = "newName", Label = "New Starter's Full Name", Type = TicketFieldType.Text, Required = true },
        new TicketFieldDescriptor { Key = "startDate", Label = "Start Date", Type = TicketFieldType.Date, Required = true },
        new TicketFieldDescriptor { Key = "jobTitle", Label = "Job Title", Type = TicketFieldType.Text, Required = true },
        new TicketFieldDescriptor
        {
            Key = "department",
            Label = "Department",
            Type = TicketFieldType.Select,
            Required = true,
            StaticOptions = TicketFieldOptionSets.FromStrings(TicketFieldOptionSets.DEPARTMENTS_ORG)
        },
        new TicketFieldDescriptor
        {
            Key = "lineManager",
            Label = "Line Manager",
            Type = TicketFieldType.Select,
            Required = true,
            OptionsSource = TicketFieldOptionsSource.ActiveUsersAll
        },
        new TicketFieldDescriptor
        {
            Key = "software",
            Label = "Software Needed",
            Type = TicketFieldType.Checks,
            StaticOptions = TicketFieldOptionSets.FromStrings(TicketFieldOptionSets.SOFTWARE)
        },
        new TicketFieldDescriptor
        {
            Key = "deviceNeeded",
            Label = "Device Needed",
            Type = TicketFieldType.Radio,
            Required = true,
            StaticOptions = TicketFieldOptionSets.NEW_DEVICES.ToList()
        },
        new TicketFieldDescriptor
        {
            Key = "details",
            Label = "Anything Else? (Optional)",
            Type = TicketFieldType.TextArea,
            Placeholder = "Desk location, phone queue, shared mailboxes",
            Rows = 3
        }
    };

    private static List<TicketFieldDescriptor> Server() => new()
    {
        new TicketFieldDescriptor
        {
            Key = "environment",
            Label = "Environment",
            Type = TicketFieldType.Radio,
            Required = true,
            StaticOptions = new List<TicketFieldOption>
            {
                new() { Value = "Production", Label = "Production", Description = "Live systems used by staff or customers. Needs manager approval." },
                new() { Value = "Test", Label = "Test", Description = "Pre-release testing." },
                new() { Value = "Development", Label = "Development", Description = "Build and prototype work." }
            }
        },
        new TicketFieldDescriptor
        {
            Key = "requestType",
            Label = "Request Type",
            Type = TicketFieldType.Select,
            Required = true,
            StaticOptions = TicketFieldOptionSets.FromStrings(new[]
            {
                "New server", "Increase CPU or memory", "Increase disk space", "Access to a server",
                "Install software", "Decommission a server", "Other"
            })
        },
        new TicketFieldDescriptor
        {
            Key = "serverName",
            Label = "Server Name, If Known",
            Type = TicketFieldType.Text,
            Placeholder = "For example TCB-TST-APP02"
        },
        new TicketFieldDescriptor
        {
            Key = "justification",
            Label = "Justification",
            Type = TicketFieldType.TextArea,
            Required = true,
            Placeholder = "Why is this needed, and what happens if we do not do it?"
        },
        new TicketFieldDescriptor
        {
            Key = "neededBy",
            Label = "Needed By (Optional)",
            Type = TicketFieldType.Date
        }
    };

    private static List<TicketFieldDescriptor> Vpn() => new()
    {
        new TicketFieldDescriptor
        {
            Key = "vpnDevice",
            Label = "Device",
            Type = TicketFieldType.Select,
            Required = true,
            Hint = "VPN only works on TopCashback laptops.",
            OptionsSource = TicketFieldOptionsSource.AffectedUserLaptops
        },
        new TicketFieldDescriptor
        {
            Key = "reason",
            Label = "Reason",
            Type = TicketFieldType.TextArea,
            Required = true,
            Placeholder = "Where you will be working and what you need to reach"
        },
        new TicketFieldDescriptor
        {
            Key = "dateFrom",
            Label = "Date Needed From",
            Type = TicketFieldType.Date,
            Required = true
        }
    };

    private static List<TicketFieldDescriptor> General() => new()
    {
        new TicketFieldDescriptor
        {
            Key = "details",
            Label = "Details",
            Type = TicketFieldType.TextArea,
            Required = true,
            Placeholder = "How can we help?"
        }
    };
}
