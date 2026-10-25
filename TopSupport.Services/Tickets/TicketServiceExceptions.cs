namespace TopSupport.Services.Tickets;

/// <summary>
/// Someone else changed the ticket between reading and writing it (e.g. two agents claiming it at
/// once). Mapped to 409 by the Api so the caller can refresh and try again.
/// </summary>
public class TicketConflictException : Exception
{
    public TicketConflictException(string message) : base(message)
    {
    }
}
