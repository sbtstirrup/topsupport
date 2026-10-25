using TopSupport.Models.People;

namespace TopSupport.Models.Admin;

/// <summary>An organisation card on PAGES.orgs: the org plus its member and open-ticket counts.</summary>
public class OrgListItemDto : OrgDto
{
    /// <summary>Users (not deleted) who belong to the organisation.</summary>
    public int MemberCount { get; set; }

    /// <summary>Active tickets raised by a member (requester only, as on the demo's card).</summary>
    public int OpenTicketCount { get; set; }
}

/// <summary>Create/edit organisation form (MODALS.orgedit / S.forms.oe).</summary>
public class SaveOrgRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
