using TopSupport.Blazor.Services;

namespace TopSupport.Blazor.Tests;

public class LocalUrlTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/my-tickets")]
    [InlineData("/tickets/T060017?created=1")]
    public void Paths_in_this_app_are_local(string url)
    {
        Assert.True(LocalUrl.IsLocal(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("https://evil.example/")]
    [InlineData("evil.example")]
    [InlineData("/ok\r\nSet-Cookie: x=1")]
    [InlineData("/\tevil")]
    public void Anything_that_could_leave_the_app_is_not(string? url)
    {
        Assert.False(LocalUrl.IsLocal(url));
    }
}

public class HtmlTextTests
{
    [Fact]
    public void Plain_text_is_encoded_never_rendered_as_markup()
    {
        Assert.Equal("<p>&lt;script&gt;alert(1)&lt;/script&gt; &amp; more</p>", HtmlText.FromText("<script>alert(1)</script> & more"));
    }

    [Fact]
    public void Blank_lines_make_paragraphs_and_single_newlines_breaks()
    {
        Assert.Equal("<p>Hi Owen,</p><p>Line one<br>Line two</p>", HtmlText.FromText("Hi Owen,\r\n\r\n\r\nLine one\nLine two"));
    }

    [Fact]
    public void Null_is_an_empty_paragraph()
    {
        Assert.Equal("<p></p>", HtmlText.FromText(null));
    }
}

public class NameFormatTests
{
    [Theory]
    [InlineData("Nadia Hussain", "NH")]
    [InlineData("  owen   pryce  ", "OP")]
    [InlineData("Mary Jane Watson", "MJ")]
    [InlineData("Cher", "C")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    public void Initials(string? name, string expected)
    {
        Assert.Equal(expected, NameFormat.Initials(name));
    }

    [Theory]
    [InlineData("Nadia Hussain", "N. Hussain")]
    [InlineData("Mary Jane Watson", "M. Watson")]
    [InlineData(" Cher ", "Cher")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Short(string? name, string expected)
    {
        Assert.Equal(expected, NameFormat.Short(name));
    }
}

public class DurationFormatTests
{
    [Theory]
    [InlineData(45, "45m")]
    [InlineData(200, "3h 20m")]
    [InlineData(600, "10h")]
    [InlineData(26 * 60 + 30, "26h")]
    [InlineData((3 * 24 + 4) * 60, "3d 4h")]
    public void Duration(int minutes, string expected)
    {
        Assert.Equal(expected, DurationFormat.Duration(TimeSpan.FromMinutes(minutes)));
    }

    [Theory]
    [InlineData(null, "—")]
    [InlineData(0.6666, "40m")]
    [InlineData(2.5, "2.5h")]
    public void Hours(double? hours, string expected)
    {
        Assert.Equal(expected, DurationFormat.Hours(hours));
    }

    [Fact]
    public void Date_uses_the_demo_format_in_local_time()
    {
        var local = new DateTimeOffset(2026, 3, 5, 9, 30, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 3, 5, 9, 30, 0)));

        Assert.Equal("05/Mar/2026", DurationFormat.Date(local));
        Assert.Equal("05/Mar/2026, 09:30", DurationFormat.DateTime(local));
    }
}
