using CloudReports.Infrastructure.Persistence;

namespace CloudReports.Infrastructure.Tests.Persistence;

public sealed class UtcDateTimeConverterTests
{
    private readonly UtcDateTimeConverter _sut = new();

    [Fact]
    public void ConvertToProvider_UtcValue_IsStoredUnchanged()
    {
        var value = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(value, _sut.ConvertToProvider(value));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ConvertToProvider_NonUtcValue_Throws(DateTimeKind kind)
    {
        var value = new DateTime(2026, 10, 8, 12, 0, 0, kind);

        Assert.Throws<ArgumentException>(() => _sut.ConvertToProvider(value));
    }

    [Fact]
    public void ConvertFromProvider_MarksValueAsUtc()
    {
        var stored = new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Unspecified);

        var result = (DateTime)_sut.ConvertFromProvider(stored)!;

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(stored.Ticks, result.Ticks);
    }
}
