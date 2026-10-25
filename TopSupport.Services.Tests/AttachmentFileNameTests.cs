using TopSupport.Models.Tickets;

namespace TopSupport.Services.Tests;

public class AttachmentFileNameTests
{
    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData(@"C:\Users\owen\Desktop\report.pdf", "report.pdf")]
    [InlineData("../../etc/passwd.txt", "passwd.txt")]
    [InlineData("  quarterly   \t figures .xlsx", "quarterly figures.xlsx")]
    [InlineData("bad<>:\"|?*name.txt", "badname.txt")]
    [InlineData(".txt", "file.txt")]
    [InlineData("", "file")]
    [InlineData(null, "file")]
    public void Name_is_cleaned(string? input, string expected)
    {
        Assert.Equal(expected, TicketAttachmentRules.SanitizeFileName(input));
    }

    [Fact]
    public void Bidi_overrides_and_zero_width_characters_are_removed()
    {
        // "invoice<U+202E>fdp.exe" would display as "invoiceexe.pdf".
        Assert.Equal("invoicefdp.exe", TicketAttachmentRules.SanitizeFileName("invoice\u202Efdp.exe"));
        Assert.Equal("notes.txt", TicketAttachmentRules.SanitizeFileName("no\u200Btes\u2066.txt"));
    }

    [Fact]
    public void Long_name_is_cut_to_the_limit_keeping_its_extension()
    {
        var name = TicketAttachmentRules.SanitizeFileName(new string('a', 300) + ".docx");

        Assert.Equal(TicketAttachmentRules.MaxFileNameLength, name.Length);
        Assert.EndsWith("a.docx", name);
    }

    [Fact]
    public void Surrogate_pair_is_not_split_when_cutting()
    {
        var name = TicketAttachmentRules.SanitizeFileName(new string('a', 194) + "😀😀😀.txt");

        Assert.False(char.IsHighSurrogate(name[^5]));
        Assert.EndsWith(".txt", name);
    }

    [Theory]
    [InlineData("photo.PNG", "image/png")]
    [InlineData("setup.exe", null)]
    [InlineData("noextension", null)]
    public void Stored_type_comes_from_the_allow_listed_extension(string fileName, string? expected)
    {
        Assert.Equal(expected, TicketAttachmentRules.ContentTypeFor(fileName));
    }
}
