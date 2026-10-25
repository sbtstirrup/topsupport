using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tickets;

/// <summary>Thrown by ITicketService.CreateTicketAsync when ITicketFormValidator rejects the request. Mapped to 400 by the Api.</summary>
public class TicketValidationException : Exception
{
    public TicketFormValidationResult ValidationResult { get; }

    public TicketValidationException(TicketFormValidationResult validationResult)
        : base("The ticket form has validation errors.")
    {
        ValidationResult = validationResult;
    }
}

/// <summary>Thrown when an action that requires an open ticket (e.g. a user reply) is attempted on a Closed ticket. Mapped to 400 by the Api.</summary>
public class TicketClosedException : Exception
{
    public TicketClosedException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when a ticket changed under the caller (e.g. another agent claimed it first). Mapped to 409 by the Api.</summary>
public class TicketConflictException : Exception
{
    public TicketConflictException(string message) : base(message)
    {
    }
}

/// <summary>Thrown when an attachment exceeds the 10MB per-file cap. Mapped to 400 by the Api.</summary>
public class AttachmentTooLargeException : Exception
{
    public AttachmentTooLargeException(string message) : base(message)
    {
    }
}
