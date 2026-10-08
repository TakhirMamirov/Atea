using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using CloudReports.Web.Controllers;

namespace CloudReports.Web.Tests.Controllers;

public sealed class FetchLogsControllerTests
{
    [Fact]
    public async Task GetLogs_PassesFiltersToQueryService()
    {
        var service = Substitute.For<IFetchLogQueryService>();
        var expected = new PagedResult<FetchLogDto>([], 0, 3, 25);
        service.GetLogsAsync(Arg.Any<FetchLogQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
        var sut = new FetchLogsController(service);

        var result = await sut.GetLogs("Rome", false, TestContext.Current.CancellationToken, page: 3, pageSize: 25);

        Assert.Same(expected, result.Value);
        await service.Received(1).GetLogsAsync(new FetchLogQuery(3, 25, "Rome", false), Arg.Any<CancellationToken>());
    }
}
