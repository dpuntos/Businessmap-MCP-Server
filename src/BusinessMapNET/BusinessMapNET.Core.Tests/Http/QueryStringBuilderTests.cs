using BusinessMapNET.Core.Http;
using Xunit;

namespace BusinessMapNET.Core.Tests.Http;

public sealed class QueryStringBuilderTests
{
    [Fact]
    public void Build_WithNoParameters_ReturnsEmptyString()
    {
        var result = new QueryStringBuilder().Build();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Build_WithSingleParameter_PrependsQuestionMark()
    {
        var result = new QueryStringBuilder()
            .Add("page", 2)
            .Build();

        Assert.Equal("?page=2", result);
    }

    [Fact]
    public void Build_WithMultipleParameters_JoinsWithAmpersand()
    {
        var result = new QueryStringBuilder()
            .Add("page", 1)
            .Add("per_page", 25)
            .Build();

        Assert.Equal("?page=1&per_page=25", result);
    }

    [Fact]
    public void Add_NullOrEmptyStringValue_IsOmitted()
    {
        var result = new QueryStringBuilder()
            .Add("a", (string?)null)
            .Add("b", string.Empty)
            .Add("c", "value")
            .Build();

        Assert.Equal("?c=value", result);
    }

    [Fact]
    public void Add_NullNullableValues_AreOmitted()
    {
        var result = new QueryStringBuilder()
            .Add("i", (int?)null)
            .Add("b", (bool?)null)
            .Build();

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(true, "?flag=1")]
    [InlineData(false, "?flag=0")]
    public void Add_Boolean_SerializesAsOneOrZero(bool value, string expected)
    {
        var result = new QueryStringBuilder()
            .Add("flag", value)
            .Build();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Add_IntegerCollection_SerializesAsCommaSeparatedValues()
    {
        var result = new QueryStringBuilder()
            .Add("board_ids", new[] { 1, 2, 3 })
            .Build();

        Assert.Equal("?board_ids=1%2C2%2C3", result);
    }

    [Fact]
    public void Add_EmptyIntegerCollection_IsOmitted()
    {
        var result = new QueryStringBuilder()
            .Add("board_ids", Array.Empty<int>())
            .Build();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Add_NullIntegerCollection_IsOmitted()
    {
        var result = new QueryStringBuilder()
            .Add("board_ids", (IEnumerable<int>?)null)
            .Build();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Add_StringCollection_SkipsNullAndEmptyEntries()
    {
        var result = new QueryStringBuilder()
            .Add("tags", new[] { "a", "", "b" })
            .Build();

        Assert.Equal("?tags=a%2Cb", result);
    }

    [Fact]
    public void Build_EscapesKeysAndValues()
    {
        var result = new QueryStringBuilder()
            .Add("na me", "a&b=c")
            .Build();

        Assert.Equal("?na%20me=a%26b%3Dc", result);
    }

    [Fact]
    public void ToString_ReturnsSameAsBuild()
    {
        var builder = new QueryStringBuilder().Add("page", 1);

        Assert.Equal(builder.Build(), builder.ToString());
    }
}
