using Microsoft.Playwright;
using NUnit.Framework;
using Reqnroll;
using System;
using System.IO;
using System.Threading.Tasks;

namespace E2E.Tests.Hooks
{
    [Binding]
    public class Hooks
    {
        private readonly ScenarioContext _scenarioContext;
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IBrowserContext _context;
        private IPage _page;

        public Hooks(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            Console.WriteLine($"Starting scenario: {_scenarioContext.ScenarioInfo.Title}");

            try
            {
                _playwright = await Playwright.CreateAsync();

                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Headless = true, // Set to false for debugging
                    Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" }
                };

                _browser = await _playwright.Chromium.LaunchAsync(launchOptions);

                var contextOptions = new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    IgnoreHTTPSErrors = true
                };

                _context = await _browser.NewContextAsync(contextOptions);

                _page = await _context.NewPageAsync();
                _page.SetDefaultTimeout(30000);
                _page.SetDefaultNavigationTimeout(30000);

                _scenarioContext["Playwright"] = _playwright;
                _scenarioContext["Browser"] = _browser;
                _scenarioContext["Context"] = _context;
                _scenarioContext["Page"] = _page;

                Console.WriteLine("Browser initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize browser: {ex.Message}");
                throw;
            }
        }

        [AfterScenario]
        public async Task AfterScenario()
        {
            try
            {
                if (_scenarioContext.TestError != null)
                {
                    var error = _scenarioContext.TestError;
                    Console.WriteLine($"Scenario FAILED: {_scenarioContext.ScenarioInfo.Title}");
                    Console.WriteLine($"Error: {error.Message}");

                    if (_page != null)
                    {
                        var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
                        var fileName = $"FAILED_{_scenarioContext.ScenarioInfo.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        await File.WriteAllBytesAsync(fileName, screenshot);
                        Console.WriteLine($"Screenshot saved: {fileName}");
                    }
                }
                else
                {
                    Console.WriteLine($"Scenario PASSED: {_scenarioContext.ScenarioInfo.Title}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AfterScenario: {ex.Message}");
            }
            finally
            {
                if (_page != null) await _page.CloseAsync();
                if (_context != null) await _context.CloseAsync();
                if (_browser != null) await _browser.CloseAsync();
                _playwright?.Dispose();

                Console.WriteLine("Browser closed");
            }
        }
    }
}