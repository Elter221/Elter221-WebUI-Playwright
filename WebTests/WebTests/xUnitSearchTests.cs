using Microsoft.Playwright;
using Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, MaxParallelThreads = 4)]
namespace WebTestsXUnit;

public class xUnitSearchTests : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IBrowserContext _context;
    private IPage _page;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        _page = await _context.NewPageAsync();

        await _page.GotoAsync("https://en.ehuniversity.lt/");
        await HandleCookieConsent();
    }

    private async Task HandleCookieConsent()
    {
        var cookieButton = await _page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
        if (cookieButton != null)
        {
            await cookieButton.ClickAsync();
            await _page.WaitForTimeoutAsync(1000);
        }
    }

    public async Task DisposeAsync()
    {
        await _context.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Theory]
    [InlineData("study programs")]
    [InlineData("admission")]
    public async Task Search_Functionality_ShouldWork(string searchTerm)
    {
        await _page.Locator("//div[@class='header-search']").ClickAsync();

        await _page.Locator("//div//input[@class='form-control']").FillAsync(searchTerm);

        await _page.Locator("//div//button[contains(text(), 'Search')]").ClickAsync();


        Assert.Contains($"/?s={searchTerm.Replace(" ", "+")}", _page.Url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var resultsLocator = _page.Locator(".search-results, .post, article");
        await Assertions.Expect(resultsLocator.First).ToBeVisibleAsync();
    }
}
