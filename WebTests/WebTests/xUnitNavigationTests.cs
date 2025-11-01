using Microsoft.Playwright;
using Xunit;

namespace WebTestsXUnit;

public class xUnitNavigationTests : IAsyncLifetime
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

    [Fact]
    [Trait("Category", "Navigation")]
    public async Task AboutPage_Navigation_ShouldWork()
    {
        var aboutLink = _page.Locator("//a[contains(text(), 'About')]").First;


        await aboutLink.WaitForAsync();
        await aboutLink.ClickAsync();


        Assert.Equal("https://en.ehuniversity.lt/about/", _page.Url);
        Assert.Contains("About", await _page.TitleAsync());
    }

    [Fact]
    [Trait("Category", "Localization")]
    public async Task LanguageSwitch_ToLithuanian_ShouldWork()
    {
        var langSwitch = _page.Locator("//ul[@class='language-switcher']").First;


        var ltLocator = langSwitch.Locator("//li//a[contains(text(),'lt')]").First;
        await langSwitch.ClickAsync();
        await ltLocator.WaitForAsync();
        await ltLocator.ClickAsync();

        Assert.Equal("https://lt.ehuniversity.lt/", _page.Url);
    }

    [Fact]
    [Trait("Category", "Contact")]
    public async Task ContactPage_Information_ShouldBeCorrect()
    {
        await _page.GotoAsync("https://en.ehu.lt/contact/");

        var emailLocator = _page.Locator("//li[strong[contains(text(),'E-mail')]]//a");
        Assert.True(await emailLocator.IsVisibleAsync());
        var emailText = await emailLocator.InnerTextAsync();
        Assert.Equal("franciskscarynacr@gmail.com", emailText);

        var phoneLtLocator = _page.Locator("//li[strong[contains(text(),'Phone')] and strong[contains(text(),'LT)')]]");
        Assert.True(await phoneLtLocator.IsVisibleAsync());
        var phoneLtText = await phoneLtLocator.InnerTextAsync();
        Assert.Contains("+370 68 771365", phoneLtText);

        var phoneByLocator = _page.Locator("//li[strong[contains(text(),'Phone (')]]");
        Assert.True(await phoneByLocator.IsVisibleAsync());
        var phoneByText = await phoneByLocator.InnerTextAsync();
        Assert.Contains("+375 29 5781488", phoneByText);

        var sNLocator = _page.Locator("//li[strong[contains(text(), 'Join us in the social networks')]]");
        Assert.True(await sNLocator.IsVisibleAsync());
        var sNText = await sNLocator.InnerTextAsync();
        Assert.Equal("Join us in the social networks: Facebook Telegram VK", sNText);
    }
}
