using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;

namespace WebAPITests.Helpers
{
    public static class ReportHelper
    {
        internal static ExtentReports _extent;
        private static ExtentTest _test;

        public static void InitializeReport()
        {
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            var htmlReporter = new ExtentSparkReporter(reportPath);

            htmlReporter.Config.DocumentTitle = "Books API Test Report";
            htmlReporter.Config.ReportName = "Books API Test Execution Report";

            _extent = new ExtentReports();
            _extent.AttachReporter(htmlReporter);

            _extent.AddSystemInfo("Environment", "QA");
            _extent.AddSystemInfo("API", "Books API");
            _extent.AddSystemInfo("Test Framework", "xUnit");
        }

        public static ExtentTest CreateTest(string testName, string description = "")
        {
            _test = _extent.CreateTest(testName, description);
            return _test;
        }

        public static ExtentTest GetCurrentTest()
        {
            return _test;
        }

        public static void LogInfo(string message)
        {
            _test.Info(message);
        }

        public static void LogPass(string message)
        {
            _test.Pass(message);
        }

        public static void LogFail(string message)
        {
            _test.Fail(message);
        }

        public static void LogWarning(string message)
        {
            _test.Warning(message);
        }

        public static void AddScreenshot(string base64Image)
        {
            _test.AddScreenCaptureFromBase64String(base64Image);
        }

        public static void FlushReport()
        {
            if (_extent != null)
            {
                _extent.Flush();

                Console.WriteLine($"ExtentReport has been generated!");

                var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
                if (Directory.Exists(reportsDir))
                {
                    var reports = Directory.GetFiles(reportsDir, "*.html");
                    if (reports.Length > 0)
                    {
                        var latestReport = reports.OrderByDescending(f => f).First();
                        Console.WriteLine($"Report location: {latestReport}");
                    }
                }
            }
        }
    }
}