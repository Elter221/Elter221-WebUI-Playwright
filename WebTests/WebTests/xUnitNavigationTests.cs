using Microsoft.Playwright;
using Serilog;
using Shouldly;
using Xunit;

namespace WebTestsXUnit;

public class xUnitNavigationTests : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowserContext _context;
    private IBrowser _browser;
    private IPage _page;
    private string _testRunId;
    private string _baseFolder;

    public async Task InitializeAsync()
    {
        var currentDrive = Path.GetPathRoot(Directory.GetCurrentDirectory());
        _baseFolder = Path.Combine(currentDrive, "TestAutomationResults");

        Directory.CreateDirectory(_baseFolder);
        Directory.CreateDirectory(Path.Combine(_baseFolder, "logs"));
        Directory.CreateDirectory(Path.Combine(_baseFolder, "screenshots"));

        _testRunId = Guid.NewGuid().ToString("N").Substring(0, 8);

        Log.Logger = new LoggerConfiguration()
            .Enrich.WithThreadId()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(Path.Combine(_baseFolder, "logs", $"test-{DateTime.Now:yyyyMMdd}.log"),
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 1,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] ({ThreadId}) [Run:{TestRunId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Test started: {TestRunId}", _testRunId);
        Log.Information("Storage location: {BaseFolder}", _baseFolder);

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

        Log.Information("Navigating to homepage");

        await _page.GotoAsync("https://en.ehuniversity.lt/");
        await HandleCookieConsent();
    }

    private async Task HandleCookieConsent()
    {
        Log.Warning("Checking for cookie consent");
        var cookieButton = await _page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
        if (cookieButton != null)
        {
            await cookieButton.ClickAsync();
            await _page.WaitForTimeoutAsync(1000);
            Log.Debug("Cookie consent handled");
        }
    }

    public async Task DisposeAsync()
    {
        Log.Information("Test completed: {TestRunId}", _testRunId);

        await _context.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
        await Log.CloseAndFlushAsync();
    }

    [Fact]
    [Trait("Category", "Navigation")]
    public async Task AboutPage_Navigation_ShouldWork()
    {
        Log.Information("Starting test: AboutPage_Navigation_ShouldWork");

        try
        {
            Log.Debug("Finding and clicking About link");
            var aboutLink = _page.Locator("//a[contains(text(), 'About')]").First;
            await aboutLink.WaitForAsync();
            await aboutLink.ClickAsync();
            Log.Information("Clicked About link");

            Log.Debug("Validating page URL and title");
            _page.Url.ShouldBe("https://en.ehuniversity.lt/about/");
            (await _page.TitleAsync()).ShouldContain("About");

            Log.Information("Test passed: AboutPage_Navigation_ShouldWork");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Test failed: AboutPage_Navigation_ShouldWork");
            await CaptureScreenshot("AboutPage_Failure");
            throw;
        }
    }

    [Fact]
    [Trait("Category", "Localization")]
    public async Task LanguageSwitch_ToLithuanian_ShouldWork()
    {
        Log.Information("Starting test: LanguageSwitch_ToLithuanian_ShouldWork");

        try
        {
            Log.Debug("Finding language switcher");
            var langSwitch = _page.Locator("//ul[@class='language-switcher']").First;
            var ltLocator = langSwitch.Locator("//li//a[contains(text(),'lt')]").First;

            Log.Debug("Clicking language switcher and Lithuanian option");
            await langSwitch.ClickAsync();
            await ltLocator.WaitForAsync();
            await ltLocator.ClickAsync();
            Log.Information("Language switch to Lithuanian completed");

            Log.Debug("Validating Lithuanian URL");
            _page.Url.ShouldBe("https://lt.ehuniversity.lt/");

            Log.Information("Test passed: LanguageSwitch_ToLithuanian_ShouldWork");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Test failed: LanguageSwitch_ToLithuanian_ShouldWork");
            await CaptureScreenshot("LanguageSwitch_Failure");
            throw;
        }
    }

    [Fact]
    [Trait("Category", "Contact")]
    public async Task ContactPage_Information_ShouldBeCorrect()
    {
        Log.Information("Starting test: ContactPage_Information_ShouldBeCorrect");

        try
        {
            Log.Debug("Navigating to contact page");
            await _page.GotoAsync("https://en.ehu.lt/contact/");
            Log.Information("Landed on contact page");



            Log.Debug("Checking email information");
            var emailLocator = _page.Locator("//li[strong[contains(text(),'E-mail')]]//a");
            (await emailLocator.IsVisibleAsync()).ShouldBeTrue();
            var emailText = await emailLocator.InnerTextAsync();
            emailText.ShouldBe("franciskscarynacr@gmail.com");
            Log.Information("Email validation passed: {Email}", emailText);



            Log.Debug("Checking Lithuanian phone");
            var phoneLtLocator = _page.Locator("//li[strong[contains(text(),'Phone')] and strong[contains(text(),'LT)')]]");
            (await phoneLtLocator.IsVisibleAsync()).ShouldBeTrue();
            var phoneLtText = await phoneLtLocator.InnerTextAsync();
            phoneLtText.ShouldContain("+370 68 771365");
            Log.Information("Lithuanian phone validation passed: {Phone}", phoneLtText);



            Log.Debug("Checking Belarus phone");
            var phoneByLocator = _page.Locator("//li[strong[contains(text(),'Phone (')]]");
            (await phoneByLocator.IsVisibleAsync()).ShouldBeTrue();
            var phoneByText = await phoneByLocator.InnerTextAsync();
            phoneByText.ShouldContain("+375 29 5781488");
            Log.Information("Belarus phone validation passed: {Phone}", phoneByText);



            Log.Debug("Checking social networks information");
            var sNLocator = _page.Locator("//li[strong[contains(text(), 'Join us in the social networks')]]");
            (await sNLocator.IsVisibleAsync()).ShouldBeTrue();
            var sNText = await sNLocator.InnerTextAsync();
            sNText.ShouldBe("Join us in the social networks: Facebook Telegram VK");
            Log.Information("Social networks validation passed: {Text}", sNText);

            Log.Information("Test passed: ContactPage_Information_ShouldBeCorrect");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Test failed: ContactPage_Information_ShouldBeCorrect");
            await CaptureScreenshot("ContactPage_Failure");
            throw;
        }
    }

    [Fact]
    [Trait("Category", "Navigation")]
    public async Task AboutPage_Navigation_ShouldntWork()
    {
        Log.Information("Starting test: AboutPage_Navigation_ShouldWork");

        try
        {
            Log.Debug("Finding and clicking About link");
            var aboutLink = _page.Locator("//a[contains(text(), 'About')]").First;
            await aboutLink.WaitForAsync();
            await aboutLink.ClickAsync();
            Log.Information("Clicked About link");

            Log.Debug("Validating page URL and title");
            _page.Url.ShouldBe("https://en.ehuniversity.lt/about/");
            (await _page.TitleAsync()).ShouldContain("BiliBobola");

            Log.Information("Test passed: AboutPage_Navigation_ShouldWork");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Test failed: AboutPage_Navigation_ShouldWork");
            await CaptureScreenshot("AboutPage_Failure");
            throw;
        }
    }

    private async Task CaptureScreenshot(string scenario)
    {
        try
        {
            var screenshotPath = Path.Combine(_baseFolder, "screenshots", $"{scenario}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            Log.Information("Screenshot saved: {Path}", screenshotPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to capture screenshot");
        }
    }
}
