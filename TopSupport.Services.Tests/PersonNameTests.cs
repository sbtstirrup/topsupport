using TopSupport.Models.People;

namespace TopSupport.Services.Tests;

/// <summary>
/// PersonName.Short writes the timeline's "Assigned to J. Moore", and the worker matches that exact
/// text to email the assigned agent, so its output is pinned here.
/// </summary>
public class PersonNameTests
{
    [Theory]
    [InlineData("Jamie Moore", "J. Moore")]
    [InlineData("  Nadia   Hussain ", "N. Hussain")]
    [InlineData("Mary Jane Watson", "M. Watson")]
    [InlineData("Cher", "Cher")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Short(string? name, string expected)
    {
        Assert.Equal(expected, PersonName.Short(name));
    }
}
