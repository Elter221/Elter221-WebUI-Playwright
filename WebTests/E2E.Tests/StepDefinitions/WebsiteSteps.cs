using Microsoft.Playwright;
using NUnit.Framework;
using Reqnroll;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[Binding]
public class WebsiteSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage _page;
    private IBrowserContext _context;
    private int _defaultTimeout = 30000;

    private const string HomeUrl = "https://en.ehuniversity.lt/";
    private const string AboutUrl = "https://en.ehuniversity.lt/about/";
    private const string ContactUrl = "https://en.ehu.lt/contact/";
    private const string LithuanianUrl = "https://lt.ehuniversity.lt/";

    public WebsiteSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _page = _scenarioContext.Get<IPage>("Page");
        _context = _scenarioContext.Get<IBrowserContext>("Context");
    }

    [Given(@"I have opened the web browser")]
    public void GivenIHaveOpenedTheWebBrowser()
    {
        Assert.That(_page, Is.Not.Null, "Page should be initialized");
    }

    [Given(@"I am on the EHU homepage")]
    public async Task GivenIAmOnTheEHUHomepage()
    {
        var options = new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = _defaultTimeout
        };

        await _page.GotoAsync(HomeUrl, options);
        await HandleCookieConsent();

        await WaitForElementAsync("text=About", 10000);

        Console.WriteLine($"Navigated to homepage: {HomeUrl}");
    }

    [Given(@"I am on the EHU homepage in English")]
    public async Task GivenIAmOnTheEHUHomepageInEnglish()
    {
        await GivenIAmOnTheEHUHomepage();
    }

    [Given(@"I navigate to the contact page")]
    public async Task GivenINavigateToTheContactPage()
    {
        var options = new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = _defaultTimeout
        };

        await _page.GotoAsync(ContactUrl, options);
        await HandleCookieConsent();

        await WaitForElementAsync("text=franciskscarynacr@gmail.com", 10000);

        Console.WriteLine($"Navigated to contact page: {ContactUrl}");
    }

    [Given(@"I start on the EHU homepage")]
    public async Task GivenIStartOnTheEHUHomepage()
    {
        await GivenIAmOnTheEHUHomepage();
    }

    [When(@"I click on the ""(.*)"" link")]
    public async Task WhenIClickOnTheLink(string linkText)
    {
        try
        {
            ILocator link = null;

            link = _page.Locator($"text={linkText}").First;

            if (await link.CountAsync() == 0)
            {
                link = _page.Locator($"//a[contains(text(), '{linkText}')]").First;
            }

            if (await link.CountAsync() == 0)
            {                link = _page.Locator($"//a[translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{linkText.ToLower()}']").First;
            }

            await link.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await link.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            Console.WriteLine($"Clicked on link: {linkText}");
        }
        catch (Exception ex)
        {

            await TakeScreenshotAsync($"ClickLink_{linkText}");
            Assert.Fail($"Failed to click on '{linkText}': {ex.Message}");
        }
    }

    [When(@"I search for ""(.*)""")]
    public async Task WhenISearchFor(string searchTerm)
    {
        try
        {
            var searchTrigger = _page.Locator("//div[@class='header-search']").First;
            await searchTrigger.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await searchTrigger.ClickAsync();
            await _page.WaitForTimeoutAsync(500);

            var searchInput = _page.Locator("//div//input[@class='form-control']").First;
            await searchInput.FillAsync(searchTerm);

            var searchButton = _page.Locator("//div//button[contains(text(), 'Search')]").First;
            await searchButton.ClickAsync();

            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await _page.WaitForTimeoutAsync(2000);

            Console.WriteLine($"Searched for: {searchTerm}");
        }
        catch (Exception ex)
        {
            await TakeScreenshotAsync($"Search_{searchTerm}");
            Assert.Fail($"Search failed for '{searchTerm}': {ex.Message}");
        }
    }

    [When(@"I switch the language to Lithuanian")]
    public async Task WhenISwitchTheLanguageToLithuanian()
    {
        try
        {
            var langSwitch = _page.Locator("//ul[@class='language-switcher']").First;
            await langSwitch.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await langSwitch.ClickAsync();

            await _page.WaitForTimeoutAsync(1000);

            var ltLocator = langSwitch.Locator("//li//a[contains(text(),'lt')]").First;
            await ltLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await ltLocator.ClickAsync();

            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            Console.WriteLine("Switched language to Lithuanian");
        }
        catch (Exception ex)
        {
            await TakeScreenshotAsync("LanguageSwitch");
            Assert.Fail($"Language switch failed: {ex.Message}");
        }
    }

    [When(@"I navigate through all major sections")]
    public async Task WhenINavigateThroughAllMajorSections()
    {
        try
        {
            Console.WriteLine("Starting complete user journey...");

            await WhenIClickOnTheLink("About");
            await ThenIShouldBeRedirectedToTheAboutPage();
            await ThenThePageTitleShouldContain("About");
            await ThenTheMainHeaderShouldBe("About");

            await _page.GoBackAsync();
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await WaitForElementAsync("text=About", 5000);

            await WhenISearchFor("study programs");
            await ThenIShouldSeeSearchResultsFor("study programs");

            await _page.GoBackAsync();
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await GivenINavigateToTheContactPage();
            await ThenIShouldSeeAllRequiredContactInformation();

            Console.WriteLine("Completed navigation through all major sections");
        }
        catch (Exception ex)
        {
            await TakeScreenshotAsync("CompleteJourney");
            Assert.Fail($"Complete journey failed: {ex.Message}");
        }
    }

    [Then(@"I should be redirected to the About page")]
    public async Task ThenIShouldBeRedirectedToTheAboutPage()
    {
        try
        {
            await _page.WaitForURLAsync(AboutUrl, new PageWaitForURLOptions
            {
                Timeout = 10000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            var currentUrl = _page.Url;
            Assert.That(currentUrl, Is.EqualTo(AboutUrl),
                $"Expected URL: {AboutUrl}, Actual: {currentUrl}");

            Console.WriteLine($"Verified on About page: {currentUrl}");
        }
        catch (TimeoutException)
        {
            await TakeScreenshotAsync("AboutPageRedirect");
            Assert.Fail($"Timeout waiting for About page. Current URL: {_page.Url}");
        }
    }

    [Then(@"the page title should contain ""(.*)""")]
    public async Task ThenThePageTitleShouldContain(string expectedText)
    {
        var title = await _page.TitleAsync();
        Assert.That(title, Contains.Substring(expectedText),
            $"Page title should contain '{expectedText}', but got: {title}");
        Console.WriteLine($"Page title verified: {title}");
    }

    [Then(@"the main header should be ""(.*)""")]
    public async Task ThenTheMainHeaderShouldBe(string expectedHeader)
    {
        try
        {
            var header = await _page.Locator("h1").First.InnerTextAsync(new LocatorInnerTextOptions { Timeout = 5000 });
            Assert.That(header.Trim(), Is.EqualTo(expectedHeader),
                $"Main header should be '{expectedHeader}', but got: {header}");
            Console.WriteLine($"Main header verified: {header}");
        }
        catch (Exception ex)
        {
            await TakeScreenshotAsync($"Header_{expectedHeader}");
            Assert.Fail($"Failed to get header text: {ex.Message}");
        }
    }

    [Then(@"I should see search results for ""(.*)""")]
    public async Task ThenIShouldSeeSearchResultsFor(string searchTerm)
    {
        var expectedUrlPart = $"/?s={searchTerm.Replace(" ", "+")}";
        var currentUrl = _page.Url;

        Assert.That(currentUrl.Contains(expectedUrlPart), Is.True,
            $"URL should contain '{expectedUrlPart}', but got: {currentUrl}");

        await _page.WaitForTimeoutAsync(2000);
        var pageContent = await _page.TextContentAsync("body");

        Assert.That(string.IsNullOrWhiteSpace(pageContent), Is.False,
            "Search results page should have content");

        if (pageContent.Contains("No results found") || pageContent.Contains("No posts found"))
        {
            Console.WriteLine("Search returned no results (expected for some terms)");
        }
        else
        {
            Console.WriteLine($"Search results verified for: {searchTerm}");
        }
    }

    [Then(@"I should be on the Lithuanian version of the website")]
    public async Task ThenIShouldBeOnTheLithuanianVersionOfTheWebsite()
    {
        try
        {
            await _page.WaitForURLAsync(LithuanianUrl, new PageWaitForURLOptions
            {
                Timeout = 10000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            Assert.That(_page.Url, Is.EqualTo(LithuanianUrl),
                $"Expected URL: {LithuanianUrl}, Actual: {_page.Url}");

            var bodyText = await _page.TextContentAsync("body");
            var lithuanianWords = new[] { "apie", "studijos", "kontaktai", "naujienos" };
            var hasLithuanian = lithuanianWords.Any(word =>
                bodyText.Contains(word, StringComparison.OrdinalIgnoreCase));

            Assert.That(hasLithuanian, Is.True, "Page should contain Lithuanian text");
            Console.WriteLine("Verified Lithuanian version of website");
        }
        catch (TimeoutException)
        {
            await TakeScreenshotAsync("LithuanianVersion");
            Assert.Fail($"Timeout waiting for Lithuanian page. Current URL: {_page.Url}");
        }
    }

    [Then(@"I should see all required contact information")]
    public async Task ThenIShouldSeeAllRequiredContactInformation()
    {
        var pageText = await _page.TextContentAsync("body");

        var checks = new List<(string Name, string Value, bool Passed)>
        {
            ("Email", "franciskscarynacr@gmail.com", pageText.Contains("franciskscarynacr@gmail.com")),
            ("Phone (LT)", "+370 68 771365", pageText.Contains("+370 68 771365")),
            ("Phone (BY)", "+375 29 5781488", pageText.Contains("+375 29 5781488"))
        };

        var failedChecks = checks.Where(c => !c.Passed).ToList();

        if (failedChecks.Any())
        {
            var failedMessages = string.Join(", ", failedChecks.Select(c => $"{c.Name} ({c.Value})"));
            await TakeScreenshotAsync("ContactInfoMissing");
            Assert.Fail($"Missing contact information: {failedMessages}");
        }

        Console.WriteLine("All required contact information verified");
    }

    [Then(@"I should see social media links")]
    public async Task ThenIShouldSeeSocialMediaLinks()
    {
        var pageText = await _page.TextContentAsync("body");
        var socialMedia = new[] { "Facebook", "Telegram", "VK" };
        var foundCount = socialMedia.Count(sm => pageText.Contains(sm));

        var socialLinks = _page.Locator("a[href*='facebook'], a[href*='telegram'], a[href*='vk.com']");
        var linkCount = await socialLinks.CountAsync();

        Assert.That(foundCount >= 2 || linkCount > 0, Is.True,
            $"Should find at least 2 social media platforms or links. Text found: {foundCount}, Links found: {linkCount}");

        Console.WriteLine("Social media links verified");
    }

    [Then(@"I should have completed a full user journey")]
    public void ThenIShouldHaveCompletedAFullUserJourney()
    {
        Assert.That(_page.Url.Contains("contact"), Is.True,
            $"Should be on contact page at the end of journey, but on: {_page.Url}");

        Console.WriteLine("Complete user journey test passed!");
    }

    #region Helper Methods

    private async Task HandleCookieConsent()
    {
        try
        {
            // Try multiple selectors for cookie consent
            var cookieSelectors = new[]
            {
                ".cc-btn.cc-dismiss",
                ".cc-btn.cc-allow",
                ".cc-btn",
                "button:has-text('Accept')",
                "button:has-text('OK')",
                "button:has-text('I agree')"
            };

            foreach (var selector in cookieSelectors)
            {
                var cookieButton = _page.Locator(selector).First;
                if (await cookieButton.CountAsync() > 0)
                {
                    await cookieButton.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                    await _page.WaitForTimeoutAsync(500);
                    Console.WriteLine("Cookie consent handled");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cookie consent handling failed (non-critical): {ex.Message}");
        }
    }

    private async Task WaitForElementAsync(string selector, int timeoutMs = 10000)
    {
        try
        {
            var element = _page.Locator(selector).First;
            await element.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
        }
        catch (Exception ex)
        {
            throw new Exception($"Element not found: {selector}. Error: {ex.Message}");
        }
    }

    private async Task TakeScreenshotAsync(string name)
    {
        try
        {
            var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true
            });

            var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            await File.WriteAllBytesAsync(fileName, screenshot);
            Console.WriteLine($"Screenshot saved: {fileName}");

            TestContext.AddTestAttachment(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to take screenshot: {ex.Message}");
        }
    }

    #endregion
}

