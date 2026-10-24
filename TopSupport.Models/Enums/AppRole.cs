namespace TopSupport.Models.Enums;

/// <summary>
/// The three roles in TopSupport, mirroring S.role / PAGES[name].roles in the original
/// topsupport-app.js demo: end-user portal, service desk agent, and administrator.
/// </summary>
public enum AppRole
{
    User,
    Agent,
    Admin
}
