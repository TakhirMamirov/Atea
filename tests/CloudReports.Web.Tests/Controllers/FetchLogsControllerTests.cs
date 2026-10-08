using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using CloudReports.Web.Controllers;
using CloudReports.Web.Models;

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
        var request = new FetchLogsRequest { City = "Rome", IsSuccess = false, Page = 3, PageSize = 25 };

        var result = await sut.GetLogs(request, TestContext.Current.CancellationToken);

        Assert.Same(expected, result.Value);
        await service.Received(1).GetLogsAsync(new FetchLogQuery(3, 25, "Rome", false), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void FetchLogsRequest_Defaults_MatchFirstPageWithDefaultSize()
    {
        Assert.Equal(new FetchLogQuery(1, FetchLogQuery.DefaultPageSize), new FetchLogsRequest().ToQuery());
    }
}
