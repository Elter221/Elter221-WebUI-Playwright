using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using AventStack.ExtentReports.Reporter.Configuration;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using nUnitWebTests;
using System.Diagnostics;
using CategoryAttribute = NUnit.Framework.CategoryAttribute;

namespace WebTestsNUnit;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class nUnitTests
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IBrowserContext _browserContext;
    private SearchPage _searchPageModel;

    // Extent Reports variables
    private static ExtentReports _extent;
    private ExtentTest _test;
    private static DateTime _suiteStartTime;
    private static Stopwatch _suiteStopwatch;
    private static readonly List<TestResultInfo> _testResults = new();

    // Per-test variables
    private Stopwatch _testStopwatch;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        _suiteStartTime = DateTime.Now;
        _suiteStopwatch = Stopwatch.StartNew();

        // Initialize Extent Reports
        InitializeExtentReports();

        _playwright = await Playwright.CreateAsync();
        _browser = await SingletonBrowser.OpenBrowser(_playwright);
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        await SingletonBrowser.CloseBrowser();
        _playwright?.Dispose();

        // Generate final report
        GenerateExtentReport();
    }

    [SetUp]
    public void TestSetup()
    {
        // Start test timer
        _testStopwatch = Stopwatch.StartNew();

        // Create test in Extent Reports
        var testName = TestContext.CurrentContext.Test.Name;
        var description = TestContext.CurrentContext.Test.Properties.Get("Description")?.ToString() ?? "";

        _test = _extent.CreateTest(testName, description);

        var category = TestContext.CurrentContext.Test.Properties["Category"]?.ToString();
        if (!string.IsNullOrEmpty(category))
        {
            _test.AssignCategory(category);
        }

        // Log test start
        _test.Info($"Test started at: {DateTime.Now:HH:mm:ss}");
    }

    [TearDown]
    public void TestTeardown()
    {
        try
        {
            // Stop test timer
            _testStopwatch.Stop();
            var testDuration = _testStopwatch.ElapsedMilliseconds;

            var testName = TestContext.CurrentContext.Test.Name;
            var status = TestContext.CurrentContext.Result.Outcome.Status;
            var category = TestContext.CurrentContext.Test.Properties["Category"]?.ToString() ?? "Uncategorized";

            // Log test result
            switch (status)
            {
                case TestStatus.Passed:
                    _test.Pass($"Test PASSED in {testDuration}ms");
                    _testResults.Add(new TestResultInfo
                    {
                        TestName = testName,
                        Status = "Pass",
                        DurationMs = testDuration,
                        Category = category
                    });
                    break;

                case TestStatus.Failed:
                    var errorMessage = TestContext.CurrentContext.Result.Message;
                    var stackTrace = TestContext.CurrentContext.Result.StackTrace;

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        _test.Fail($"Test FAILED in {testDuration}ms: {errorMessage}");
                    }
                    else
                    {
                        _test.Fail($"Test FAILED in {testDuration}ms");
                    }

                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        _test.Fail($"Stack Trace: {stackTrace}");
                    }

                    _testResults.Add(new TestResultInfo
                    {
                        TestName = testName,
                        Status = "Fail",
                        DurationMs = testDuration,
                        Category = category,
                        ErrorMessage = errorMessage
                    });
                    break;

                case TestStatus.Skipped:
                case TestStatus.Inconclusive:
                    var skipReason = TestContext.CurrentContext.Result.Message ?? "No reason provided";
                    _test.Skip($"Test SKIPPED in {testDuration}ms: {skipReason}");

                    _testResults.Add(new TestResultInfo
                    {
                        TestName = testName,
                        Status = "Skip",
                        DurationMs = testDuration,
                        Category = category,
                        ErrorMessage = skipReason
                    });
                    break;

                default:
                    _test.Warning($"Test ended with unexpected status: {status}");
                    break;
            }

            _test.Info($"Test ended at: {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in TestTeardown: {ex.Message}");
        }
    }

    private void InitializeExtentReports()
    {
        try
        {
            // Create report directory
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var reportDir = Path.Combine(basePath, "TestReports");
            Directory.CreateDirectory(reportDir);

            var reportPath = Path.Combine(reportDir, $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");

            // HTML Reporter
            var htmlReporter = new ExtentHtmlReporter(reportPath);
            htmlReporter.Config.DocumentTitle = "EHU University Test Report";
            htmlReporter.Config.ReportName = "Test Execution Report";
            htmlReporter.Config.Theme = Theme.Standard;

            _extent = new ExtentReports();
            _extent.AttachReporter(htmlReporter);

            // Add system information
            _extent.AddSystemInfo("Environment", "Test");
            _extent.AddSystemInfo("Test Framework", "NUnit");
            _extent.AddSystemInfo("Browser", "Chromium");
            _extent.AddSystemInfo("Execution Date", DateTime.Now.ToString("yyyy-MM-dd"));
            _extent.AddSystemInfo("Machine Name", Environment.MachineName);
            _extent.AddSystemInfo("User Name", Environment.UserName);

            Console.WriteLine($"Extent Reports initialized. Report will be saved to: {reportPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Extent Reports: {ex.Message}");
        }
    }

    private void GenerateExtentReport()
    {
        try
        {
            _suiteStopwatch.Stop();

            // Create suite summary
            var suiteTest = _extent.CreateTest("Test Suite Summary")
                .Info($"Suite Start Time: {_suiteStartTime:yyyy-MM-dd HH:mm:ss}")
                .Info($"Suite End Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .Info($"Total Suite Duration: {_suiteStopwatch.ElapsedMilliseconds}ms");

            // Add statistics
            var passed = _testResults.Count(r => r.Status == "Pass");
            var failed = _testResults.Count(r => r.Status == "Fail");
            var skipped = _testResults.Count(r => r.Status == "Skip");

            suiteTest.Info($"Total Tests: {_testResults.Count}");
            suiteTest.Info($"Passed: {passed}");
            suiteTest.Info($"Failed: {failed}");
            suiteTest.Info($"Skipped: {skipped}");

            // Calculate total test execution time
            var totalTestTime = _testResults.Sum(r => r.DurationMs);
            suiteTest.Info($"Total Test Execution Time: {totalTestTime}ms");

            // Create a summary table
            var tableHtml = @"<div style='margin: 20px; padding: 15px; border: 1px solid #ddd; background-color: #f9f9f9;'>
                    <h3 style='color: #333;'>📊 Test Execution Summary</h3>
                    <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
                        <thead>
                            <tr style='background-color: #4CAF50; color: white;'>
                                <th style='padding: 8px; text-align: left;'>Test Name</th>
                                <th style='padding: 8px; text-align: left;'>Status</th>
                                <th style='padding: 8px; text-align: left;'>Duration</th>
                                <th style='padding: 8px; text-align: left;'>Category</th>
                            </tr>
                        </thead>
                        <tbody>";

            foreach (var result in _testResults)
            {
                var statusColor = result.Status switch
                {
                    "Pass" => "green",
                    "Fail" => "red",
                    "Skip" => "orange",
                    _ => "gray"
                };

                var statusIcon = result.Status switch
                {
                    "Pass" => "✓",
                    "Fail" => "✗",
                    "Skip" => "⚠",
                    _ => "?"
                };

                tableHtml += $@"
                        <tr style='border-bottom: 1px solid #ddd;'>
                            <td style='padding: 8px;'>{result.TestName}</td>
                            <td style='padding: 8px; color: {statusColor}; font-weight: bold;'>{statusIcon} {result.Status}</td>
                            <td style='padding: 8px;'>{result.DurationMs} ms</td>
                            <td style='padding: 8px;'>{result.Category}</td>
                        </tr>";
            }

            tableHtml += @"</tbody></table></div>";

            suiteTest.Info(tableHtml);

            // Flush the report
            _extent.Flush();

            Console.WriteLine($"\n📊 Extent Report generated successfully!");
            Console.WriteLine($"⏱️  Total Suite Time: {_suiteStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"📁 Report location: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestReports")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating Extent report: {ex.Message}");
        }
    }

    private async Task<IPage> CreateNewPageAsync()
    {
        _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await _browserContext.NewPageAsync();
        await page.GotoAsync("https://en.ehuniversity.lt/");
        await HandleCookieConsent(page);

        _searchPageModel = new SearchPage(page);

        return page;
    }

    private async Task HandleCookieConsent(IPage page)
    {
        var cookieButton = await page.QuerySelectorAsync(".cc-btn.cc-dismiss, .cc-btn.cc-allow, .cc-btn");
        if (cookieButton != null)
        {
            await cookieButton.ClickAsync();
            await page.WaitForTimeoutAsync(1000);
            _test.Info("Cookie consent handled");
        }
    }

    [Test]
    [Category("Navigation")]
    [Property("Description", "Verify About page navigation")]
    public async Task AboutPage_Navigation_ShouldWork()
    {
        _test.Info("Starting About page navigation test");

        var page = await CreateNewPageAsync();

        try
        {
            _test.Info("Finding About link");
            var aboutLink = page.Locator("//a[contains(text(), 'About')]").First;

            await aboutLink.WaitForAsync();
            await aboutLink.ClickAsync();
            _test.Pass("Clicked About link");

            _test.Info("Validating URL");
            Assert.That(page.Url, Is.EqualTo("https://en.ehuniversity.lt/about/"));
            _test.Pass($"URL is correct: {page.Url}");

            _test.Info("Validating page title");
            var title = await page.TitleAsync();
            Assert.That(title, Does.Contain("About"));
            _test.Pass($"Title contains 'About': {title}");
        }
        catch (Exception ex)
        {
            _test.Fail($"Test failed: {ex.Message}");
            await CaptureScreenshot(page, "AboutPage_Failure");
            throw;
        }
        finally
        {
            await page.CloseAsync();
            await _browserContext.CloseAsync();
        }
    }

    [Test]
    [TestCase("study programs")]
    [TestCase("admission")]
    [Property("Description", "Test search functionality with different terms")]
    public async Task Search_Functionality_ShouldWork(string searchTerm)
    {
        _test.Info($"Starting search test with term: '{searchTerm}'");

        var page = await CreateNewPageAsync();

        try
        {
            await _searchPageModel.SearchProgramAsync(searchTerm);
            _test.Info($"Searched for: {searchTerm}");

            Assert.That(page.Url, Does.Contain($"/?s={searchTerm.Replace(" ", "+")}"));
            _test.Pass($"URL contains search term: {page.Url}");

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var resultsLocator = page.Locator(".search-results, .post, article");
            await Assertions.Expect(resultsLocator.First).ToBeVisibleAsync();
            _test.Pass("Search results are visible");
        }
        catch (Exception ex)
        {
            _test.Fail($"Search test failed: {ex.Message}");
            await CaptureScreenshot(page, "Search_Failure");
            throw;
        }
        finally
        {
            await page.CloseAsync();
            await _browserContext.CloseAsync();
        }
    }

    [Test]
    [Category("Localization")]
    [Property("Description", "Test language switching to Lithuanian")]
    public async Task LanguageSwitch_ToLithuanian_ShouldWork()
    {
        _test.Info("Starting language switch test");

        var skipReason = "Test is inconclusive - Language switcher element may have changed";
        _test.Skip(skipReason);

        Assert.Inconclusive(skipReason);

        //var page = await CreateNewPageAsync();

        //try
        //{
        //    var langSwitch = page.Locator("//ul[@class='language-switcher']").First;

        //    var ltLocator = langSwitch.Locator("//li//a[contains(text(),'lt')]").First;
        //    await langSwitch.ClickAsync();
        //    await ltLocator.WaitForAsync();
        //    await ltLocator.ClickAsync();

        //    Assert.That(page.Url, Is.EqualTo("https://lt.ehuniversity.lt/"));
        //}
        //finally
        //{
        //    await page.CloseAsync();
        //}
    }

    [Test]
    [Category("Contact")]
    [Property("Description", "Verify contact page information")]
    public async Task ContactPage_Information_ShouldBeCorrect()
    {
        _test.Info("Starting contact page test");

        var page = await CreateNewPageAsync();
        try
        {
            await page.GotoAsync("https://en.ehu.lt/contact/");
            _test.Info("Navigated to contact page");

            _test.Info("Validating email");
            var emailLocator = page.Locator("//li[strong[contains(text(),'E-mail')]]//a");
            Assert.That(await emailLocator.IsVisibleAsync(), "Email element should be visible");
            var emailText = await emailLocator.InnerTextAsync();

            Assert.That(emailText, Is.EqualTo("wrong-email@gmail.com"),
                $"Expected: wrong-email@gmail.com, Actual: {emailText}");

            _test.Pass($"Email validated: {emailText}");

            //var phoneLtLocator = page.Locator("//li[strong[contains(text(),'Phone')] and strong[contains(text(),'LT)')]]");
            //Assert.That(await phoneLtLocator.IsVisibleAsync());
            //var phoneLtText = await phoneLtLocator.InnerTextAsync();
            //Assert.That(phoneLtText, Does.Contain("+370 68 771365"));

            //var phoneByLocator = page.Locator("//li[strong[contains(text(),'Phone (')]]");
            //Assert.That(await phoneByLocator.IsVisibleAsync());
            //var phoneByText = await phoneByLocator.InnerTextAsync();
            //Assert.That(phoneByText, Does.Contain("+375 29 5781488"));

            //var sNLocator = page.Locator("//li[strong[contains(text(), 'Join us in the social networks')]]");
            //Assert.That(await sNLocator.IsVisibleAsync());
            //var sNText = await sNLocator.InnerTextAsync();
            //Assert.That("Join us in the social networks: Facebook Telegram VK", Does.Contain(sNText));

        }
        catch (Exception ex)
        {
            _test.Fail($"Contact page test failed: {ex.Message}");
            await CaptureScreenshot(page, "ContactPage_Failure");
            throw;
        }
        finally
        {
            await page.CloseAsync();
            await _browserContext.CloseAsync();
        }
    }

    private async Task CaptureScreenshot(IPage page, string scenario)
    {
        try
        {
            var screenshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            Directory.CreateDirectory(screenshotDir);

            var screenshotPath = Path.Combine(screenshotDir, $"{scenario}_{DateTime.Now:yyyyMMddHHmmss}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });

            _test.Info($"Screenshot saved: {screenshotPath}");
            _test.AddScreenCaptureFromPath(screenshotPath, $"{scenario} Screenshot");
        }
        catch (Exception ex)
        {
            _test.Warning($"Failed to capture screenshot: {ex.Message}");
        }
    }
}