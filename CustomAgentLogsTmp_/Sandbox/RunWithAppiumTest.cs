#!/usr/bin/env dotnet run
#:package Appium.WebDriver@8.0.0
#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable CS0219 // The variable is assigned but its value is never used

/*
 * Appium Test Script for PR #32939: Slider/Stepper Property Order Independence
 * 
 * Tests that Slider and Stepper controls correctly preserve Value property
 * regardless of the order in which Minimum, Maximum, and Value are set.
 */

using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Enums;

// ========== CONFIGURATION ==========

const int ISSUE_NUMBER = 32903;

// ========== DEVICE SETUP ==========

var udid = Environment.GetEnvironmentVariable("DEVICE_UDID");
if (string.IsNullOrEmpty(udid))
{
    Console.WriteLine("❌ ERROR: DEVICE_UDID environment variable not set!");
    Console.WriteLine("This should be set automatically by BuildAndRunSandbox.ps1 script.");
    Environment.Exit(1);
}

// Auto-detect platform from UDID format
string PLATFORM = udid.Contains("-") && udid.Length > 20 ? "ios" : "android";

Console.WriteLine($"═══════════════════════════════════════════════════════");
Console.WriteLine($"  Testing PR #32939: Slider/Stepper Property Order");
Console.WriteLine($"  Platform: {PLATFORM.ToUpper(System.Globalization.CultureInfo.InvariantCulture)}");
Console.WriteLine($"  Device UDID: {udid}");
Console.WriteLine($"═══════════════════════════════════════════════════════\n");

// ========== APPIUM CONNECTION ==========

var serverUri = new Uri("http://localhost:4723");
AppiumOptions options;

// ========== PLATFORM-SPECIFIC OPTIONS ==========

if (PLATFORM == "android")
{
    options = new AppiumOptions();
    options.PlatformName = "Android";
    options.AutomationName = "UIAutomator2";
    options.AddAdditionalAppiumOption("appium:appPackage", "com.microsoft.maui.sandbox");
    options.AddAdditionalAppiumOption("appium:appActivity", "com.microsoft.maui.sandbox.MainActivity");
    
    // 🚨 CRITICAL: noReset MUST be set to true for Android
    options.AddAdditionalAppiumOption("appium:noReset", true);
    
    options.AddAdditionalAppiumOption(MobileCapabilityType.Udid, udid);
    options.AddAdditionalAppiumOption("appium:newCommandTimeout", 300);
}
else if (PLATFORM == "ios")
{
    options = new AppiumOptions();
    options.PlatformName = "iOS";
    options.AutomationName = "XCUITest";
    options.AddAdditionalAppiumOption("appium:bundleId", "com.microsoft.maui.sandbox");
    options.AddAdditionalAppiumOption(MobileCapabilityType.Udid, udid);
    options.AddAdditionalAppiumOption("appium:newCommandTimeout", 300);
}
else
{
    Console.WriteLine($"❌ ERROR: Unsupported platform: {PLATFORM}");
    Environment.Exit(1);
    return;
}

// ========== CONNECT TO APPIUM ==========

Console.WriteLine("Connecting to Appium server...");

try
{
    AppiumDriver driver;
    if (PLATFORM == "android")
    {
        driver = new AndroidDriver(serverUri, options);
    }
    else
    {
        driver = new IOSDriver(serverUri, options);
    }
    
    using (driver)
    {
        Console.WriteLine("✅ Connected to Appium and launched app!\n");
    
        // Get PID for Android logcat filtering
        if (PLATFORM == "android")
        {
            try
            {
                var getPidProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {udid} shell pidof -s com.microsoft.maui.sandbox",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (getPidProcess != null)
                {
                    getPidProcess.WaitForExit();
                    var pid = getPidProcess.StandardOutput.ReadToEnd().Trim();

                    if (!string.IsNullOrEmpty(pid))
                    {
                        Console.WriteLine($"SANDBOX_APP_PID={pid}");
                        Console.WriteLine($"✅ Captured app PID: {pid}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Failed to capture PID: {ex.Message}\n");
            }
        }
    
        // Wait for app to load
        Thread.Sleep(3000);
    
        // ========== TEST LOGIC ==========
        
        Console.WriteLine("🔹 Validating initial Slider/Stepper values...\n");
        
        // Test 1: Verify initial binding values are correct
        try
        {
            var sliderValueLabel = FindElement(driver, "SliderValueLabel");
            var stepperValueLabel = FindElement(driver, "StepperValueLabel");
            
            var sliderText = sliderValueLabel.Text;
            var stepperText = stepperValueLabel.Text;
            
            Console.WriteLine($"Slider Value Label: {sliderText}");
            Console.WriteLine($"Stepper Value Label: {stepperText}");
            
            bool sliderPassed = sliderText.Contains("50");
            bool stepperPassed = stepperText.Contains("50");
            
            if (sliderPassed && stepperPassed)
            {
                Console.WriteLine("✅ Test 1 PASSED: Initial values correct (Slider=50, Stepper=50)");
            }
            else
            {
                Console.WriteLine($"❌ Test 1 FAILED: Slider={sliderText}, Stepper={stepperText} (Expected both to be 50)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test 1 FAILED: Could not find value labels - {ex.Message}");
        }
        
        Console.WriteLine();
        
        // Test 2: Programmatic order tests
        Console.WriteLine("🔹 Testing programmatic property order (Value → Min → Max)...");
        try
        {
            var btn1 = FindElement(driver, "TestValueMinMaxBtn");
            btn1.Click();
            Thread.Sleep(1000);
            
            var validationStatus = FindElement(driver, "ValidationStatus");
            var statusText = validationStatus.Text;
            Console.WriteLine($"Result: {statusText}");
            
            if (statusText.Contains("PASSED"))
            {
                Console.WriteLine("✅ Test 2a PASSED: Value→Min→Max order works");
            }
            else
            {
                Console.WriteLine($"❌ Test 2a FAILED: {statusText}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test 2a FAILED: {ex.Message}");
        }
        
        Console.WriteLine();
        
        Console.WriteLine("🔹 Testing programmatic property order (Min → Value → Max)...");
        try
        {
            var btn2 = FindElement(driver, "TestMinValueMaxBtn");
            btn2.Click();
            Thread.Sleep(1000);
            
            var validationStatus = FindElement(driver, "ValidationStatus");
            var statusText = validationStatus.Text;
            Console.WriteLine($"Result: {statusText}");
            
            if (statusText.Contains("PASSED"))
            {
                Console.WriteLine("✅ Test 2b PASSED: Min→Value→Max order works");
            }
            else
            {
                Console.WriteLine($"❌ Test 2b FAILED: {statusText}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test 2b FAILED: {ex.Message}");
        }
        
        Console.WriteLine();
        
        Console.WriteLine("🔹 Testing programmatic property order (Max → Value → Min)...");
        try
        {
            var btn3 = FindElement(driver, "TestMaxValueMinBtn");
            btn3.Click();
            Thread.Sleep(1000);
            
            var validationStatus = FindElement(driver, "ValidationStatus");
            var statusText = validationStatus.Text;
            Console.WriteLine($"Result: {statusText}");
            
            if (statusText.Contains("PASSED"))
            {
                Console.WriteLine("✅ Test 2c PASSED: Max→Value→Min order works");
            }
            else
            {
                Console.WriteLine($"❌ Test 2c FAILED: {statusText}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test 2c FAILED: {ex.Message}");
        }
        
        Console.WriteLine();
        
        // Test 3: Dynamic range changes (value preservation)
        Console.WriteLine("🔹 Testing dynamic range changes (value preservation)...");
        try
        {
            var dynamicValueLabel = FindElement(driver, "DynamicSliderValueLabel");
            Console.WriteLine($"Initial value: {dynamicValueLabel.Text}");
            
            // Shrink range to 0-10 (should clamp value to 10)
            var shrinkBtn = FindElement(driver, "ShrinkRangeBtn");
            shrinkBtn.Click();
            Thread.Sleep(1000);
            
            dynamicValueLabel = FindElement(driver, "DynamicSliderValueLabel");
            Console.WriteLine($"After shrink: {dynamicValueLabel.Text}");
            
            // Expand range back to 0-100 (should restore value to 50)
            var expandBtn = FindElement(driver, "ExpandRangeBtn");
            expandBtn.Click();
            Thread.Sleep(1000);
            
            dynamicValueLabel = FindElement(driver, "DynamicSliderValueLabel");
            var validationStatus = FindElement(driver, "ValidationStatus");
            
            Console.WriteLine($"After expand: {dynamicValueLabel.Text}");
            Console.WriteLine($"Validation: {validationStatus.Text}");
            
            if (validationStatus.Text.Contains("Value restored to 50"))
            {
                Console.WriteLine("✅ Test 3 PASSED: Value correctly restored after range expansion");
            }
            else
            {
                Console.WriteLine($"❌ Test 3 FAILED: {validationStatus.Text}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test 3 FAILED: {ex.Message}");
        }
        
        // ========== END TEST LOGIC ==========
        
        Console.WriteLine("\n" + new string('═', 55));
        Console.WriteLine("  Test completed");
        Console.WriteLine(new string('═', 55) + "\n");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: Test failed");
    Console.WriteLine($"Exception: {ex.Message}");
    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
    Environment.Exit(1);
}

// ========== HELPER METHODS ==========

IWebElement FindElement(AppiumDriver driver, string automationId)
{
    if (PLATFORM == "android")
    {
        // Try resource-id first, then XPath
        try
        {
            return driver.FindElement(MobileBy.Id(automationId));
        }
        catch
        {
            return driver.FindElement(MobileBy.XPath($"//*[@resource-id='{automationId}' or contains(@text, '{automationId}')]"));
        }
    }
    else
    {
        return driver.FindElement(MobileBy.AccessibilityId(automationId));
    }
}
