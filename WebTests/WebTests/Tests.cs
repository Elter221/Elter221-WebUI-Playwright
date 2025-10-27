using Microsoft.Playwright;
using Xunit;

namespace WebTests
{
    public class Tests
    {
        [Fact]
        static async Task TestCase1()
        {
            using var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync();

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync("https://en.ehuniversity.lt/");

            var cookieButton = await page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
            if (cookieButton != null)
            {
                await cookieButton.ClickAsync();
            }

            var aboutLock = page.Locator("//a[contains(text(), 'About')]").First;

            await aboutLock.WaitForAsync();

            await aboutLock.ClickAsync();

            Assert.Equal("https://en.ehuniversity.lt/about/", page.Url);
            Assert.Contains("About", await page.TitleAsync());

        }

        [Fact]
        static async Task TestCase2()
        {
            using var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync();

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync("https://en.ehuniversity.lt/");

            var cookieButton = await page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
            if (cookieButton != null)
            {
                await cookieButton.ClickAsync();
            }

            await page.Locator("//div[@class='header-search']").ClickAsync();

            await page.Locator("//div//input[@class='form-control']").FillAsync("study programs");

            await page.Locator("//div//button[contains(text(), 'Search')]").ClickAsync();

            Assert.Contains("/?s=study+programs", page.Url);
        }

        [Fact]
        public async Task TestCase3()
        {
            using var pw = await Playwright.CreateAsync();
            await using var browser = await pw.Chromium.LaunchAsync();

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.GotoAsync("https://en.ehu.lt/");

            var cookieButton = await page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
            if (cookieButton != null)
            {
                await cookieButton.ClickAsync();
            }

            var langSwitch = page.Locator("//ul[@class='language-switcher']").First;
            var ltLocator = langSwitch.Locator("//li//a[contains(text(),'lt')]").First;
            await langSwitch.ClickAsync();
            await ltLocator.WaitForAsync();
            await ltLocator.ClickAsync();

            Assert.Equal(@"https://lt.ehuniversity.lt/", page.Url);
        }

        [Fact]
        public async Task TestCase4()
        {
            using var pw = await Playwright.CreateAsync();
            await using var browser = await pw.Chromium.LaunchAsync();

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.GotoAsync("https://en.ehu.lt/contact/");

            var cookieButton = await page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
            if (cookieButton != null)
            {
                await cookieButton.ClickAsync();
            }

            var emailLocator = page.Locator("//li[strong[contains(text(),'E-mail')]]//a");
            Assert.True(await emailLocator.IsVisibleAsync());
            var emailText = await emailLocator.InnerTextAsync();
            Assert.Equal("franciskscarynacr@gmail.com", emailText);

            var phoneLtLocator = page.Locator("//li[strong[contains(text(),'Phone')] and strong[contains(text(),'LT)')]]");
            Assert.True(await phoneLtLocator.IsVisibleAsync());
            var phoneLtText = await phoneLtLocator.InnerTextAsync();
            Assert.Contains("+370 68 771365", phoneLtText);

            var phoneByLocator = page.Locator("//li[strong[contains(text(),'Phone (')]]");
            Assert.True(await phoneByLocator.IsVisibleAsync());
            var phoneByText = await phoneByLocator.InnerTextAsync();
            Assert.Contains("+375 29 5781488", phoneByText);

            var sNLocator = page.Locator("//li[strong[contains(text(), 'Join us in the social networks')]]");
            Assert.True(await sNLocator.IsVisibleAsync());
            var sNText = await sNLocator.InnerTextAsync();
            Assert.Equal("Join us in the social networks: Facebook Telegram VK", sNText);
        }
    }
}
