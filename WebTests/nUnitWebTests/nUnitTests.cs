using Microsoft.Playwright;
using NUnit.Framework;
using CategoryAttribute = NUnit.Framework.CategoryAttribute;

namespace WebTestsNUnit;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class nUnitTests
{
    private IPlaywright _playwright;
    private IBrowser _browser;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    private async Task<IPage> CreateNewPageAsync()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("https://en.ehuniversity.lt/");
        await HandleCookieConsent(page);

        return page;
    }

    private async Task HandleCookieConsent(IPage page)
    {
        var cookieButton = await page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
        if (cookieButton != null)
        {
            await cookieButton.ClickAsync();
            await page.WaitForTimeoutAsync(1000);
        }
    }


    [Test]
    [Category("Navigation")]
    public async Task AboutPage_Navigation_ShouldWork()
    {
        var page = await CreateNewPageAsync();

        try
        {
            var aboutLink = page.Locator("//a[contains(text(), 'About')]").First;

            await aboutLink.WaitForAsync();
            await aboutLink.ClickAsync();

            Assert.That(page.Url, Is.EqualTo("https://en.ehuniversity.lt/about/"));
            Assert.That(await page.TitleAsync(), Does.Contain("About"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Test]
    [TestCase("study programs")]
    [TestCase("admission")]
    public async Task Search_Functionality_ShouldWork(string searchTerm)
    {
        var page = await CreateNewPageAsync();

        try
        {
            await page.Locator("//div[@class='header-search']").ClickAsync();

            await page.Locator("//div//input[@class='form-control']").FillAsync(searchTerm);
            await page.Locator("//div//button[contains(text(), 'Search')]").ClickAsync();

            Assert.That(page.Url, Does.Contain($"/?s={searchTerm.Replace(" ", "+")}"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var resultsLocator = page.Locator(".search-results, .post, article");
            await Assertions.Expect(resultsLocator.First).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Test]
    [Category("Localization")]
    public async Task LanguageSwitch_ToLithuanian_ShouldWork()
    {

        var page = await CreateNewPageAsync();

        try
        {
            var langSwitch = page.Locator("//ul[@class='language-switcher']").First;

            var ltLocator = langSwitch.Locator("//li//a[contains(text(),'lt')]").First;
            await langSwitch.ClickAsync();
            await ltLocator.WaitForAsync();
            await ltLocator.ClickAsync();

            Assert.That(page.Url, Is.EqualTo("https://lt.ehuniversity.lt/"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Test]
    [Category("Contact")]
    public async Task ContactPage_Information_ShouldBeCorrect()
    {
        var page = await CreateNewPageAsync();
        try
        {
            await page.GotoAsync("https://en.ehu.lt/contact/");

            var emailLocator = page.Locator("//li[strong[contains(text(),'E-mail')]]//a");
            Assert.That(await emailLocator.IsVisibleAsync());
            var emailText = await emailLocator.InnerTextAsync();
            Assert.That(emailText, Is.EqualTo("franciskscarynacr@gmail.com"));

            var phoneLtLocator = page.Locator("//li[strong[contains(text(),'Phone')] and strong[contains(text(),'LT)')]]");
            Assert.That(await phoneLtLocator.IsVisibleAsync());
            var phoneLtText = await phoneLtLocator.InnerTextAsync();
            Assert.That(phoneLtText, Does.Contain("+370 68 771365"));

            var phoneByLocator = page.Locator("//li[strong[contains(text(),'Phone (')]]");
            Assert.That(await phoneByLocator.IsVisibleAsync());
            var phoneByText = await phoneByLocator.InnerTextAsync();
            Assert.That(phoneByText, Does.Contain("+375 29 5781488"));

            var sNLocator = page.Locator("//li[strong[contains(text(), 'Join us in the social networks')]]");
            Assert.That(await sNLocator.IsVisibleAsync());
            var sNText = await sNLocator.InnerTextAsync();
            Assert.AreEqual("Join us in the social networks: Facebook Telegram VK", sNText);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}