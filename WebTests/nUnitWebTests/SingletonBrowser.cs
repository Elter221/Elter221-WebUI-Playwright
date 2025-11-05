using Microsoft.Playwright;

namespace nUnitWebTests;

public class SingletonBrowser
{
    private static IBrowser _browser;

    private SingletonBrowser()
    {
    }

    public async static Task<IBrowser> OpenBrowser(IPlaywright playwright)
    {
        if (_browser is null)
        {
            _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }

        return _browser;
    }

    public static async Task CloseBrowser()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }
    }
}
