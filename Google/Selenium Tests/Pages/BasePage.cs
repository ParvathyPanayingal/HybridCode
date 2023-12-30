﻿using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Selenium_Tests.Pages
{
    public class BasePage
    {
        protected IWebDriver Driver;
        protected ExtentReports Extent;
        protected ExtentTest Test;
        private ExtentSparkReporter SparkReporter;

        private Dictionary<string, string> Properties;
        protected string? currdir;
        protected string url;

        //overloaded constructors
        protected BasePage()
        {
            
            currdir = Directory.GetParent(@"../../../")?.FullName;
        }

        public BasePage(IWebDriver driver)
        {
            Driver = driver;
        }

        //common page methods
        public void ReadConfigSettings()
        {
            Properties = new Dictionary<string, string>();
            string fileName = currdir + "/configsettings/config.properties";
            string[] lines = File.ReadAllLines(fileName);

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && line.Contains('='))
                {
                    string[] parts = line.Split('=');
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    Properties[key] = value;
                }
            }
        }

        protected static void ScrollIntoView(IWebDriver driver, IWebElement? element)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }
        
        protected void TakeScreenShot()
        {
            ITakesScreenshot iss = (ITakesScreenshot)Driver;
            Screenshot ss = iss.GetScreenshot();

            string? filepath = currdir + "/Screenshots/ss_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

            ss.SaveAsFile(filepath);

        }

        protected void LogTestResult(string testName,string type, ExtentTest Test, string result, string? errorMessage = null)//type can be information,pass or fail
        {
            if(type.ToLower().Equals("info"))
            {
                Log.Information(result);
                Test?.Info(result);
            }
            else if(type.ToLower().Equals("pass")&& errorMessage==null)
            {
                Log.Information(testName + "Passed");
                Log.Information("________________________________________");
                Test?.Pass(result);
            }
            else
            {
                Log.Error($"Test failed for {testName}. \n Exception: \n {errorMessage}");
                Log.Information("_________________________________");
                var screenshot=((ITakesScreenshot)Driver).GetScreenshot().AsBase64EncodedString;
            Test?.AddScreenCaptureFromBase64String(screenshot);
                Test?.Fail(result);
            }

             
        }
        protected void InitializeBrowser()
        {

            ReadConfigSettings();
            if (Properties["browser"].ToLower() == "chrome")
            {
                Driver = new ChromeDriver();
            }
            else if (Properties["browser"].ToLower() == "edge")
            {
                Driver = new EdgeDriver();
            }
            url = Properties["baseUrl"];
            Driver.Url = url;
            Driver.Manage().Window.Maximize();

        }

        [OneTimeSetUp]
        public void Setup()
        {
            InitializeBrowser();

            //configure serilog
            string? logfilepath = currdir + "/Logs/log_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(logfilepath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

            //configure extent report
            Extent = new ExtentReports();
            SparkReporter = new ExtentSparkReporter(currdir + "/Reports/report-"
                + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html");

            Extent?.AttachReporter(SparkReporter);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Driver?.Quit();
            Extent.Flush();
            Log.CloseAndFlush();
        }

        protected static void WaitForElementToBeClicked(IWebElement? element,string elementName)
        {
            if(element != null)
            {
                DefaultWait<IWebElement> fluentWait = new DefaultWait<IWebElement>(element);
                fluentWait.Timeout = TimeSpan.FromSeconds(5);
                    fluentWait.PollingInterval = TimeSpan.FromMilliseconds(50);
                    fluentWait.IgnoreExceptionTypes(typeof(NoSuchElementException));
                fluentWait.Message = "Element - " + elementName+" -not found or"+"not clickable";

                fluentWait.Until(x => x!=  null && x.Displayed && x.Enabled);
            }
        }

    }

    
}
