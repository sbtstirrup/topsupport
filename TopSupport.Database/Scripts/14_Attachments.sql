/*
    14_Attachments.sql
    Attachment hardening. The download path (AttachmentsController) used to find which ticket a
    file belongs to by loading every one of the caller's tickets; this looks it up directly so the
    Api can load that one ticket and apply the end-user visibility rules to it.

    - usp_TicketAttachment_GetTicketId: the ticket an attachment belongs to (no row when the
      attachment doesn't exist). Metadata only -- never selects Content.
*/
USE TopSupport;
GO

CREATE OR ALTER PROCEDURE dbo.usp_TicketAttachment_GetTicketId
    @AttachmentId NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TicketId
    FROM dbo.TicketAttachments
    WHERE Id = @AttachmentId;
END
GO
