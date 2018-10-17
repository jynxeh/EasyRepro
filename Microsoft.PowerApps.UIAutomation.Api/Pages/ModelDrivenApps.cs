﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Dynamics365.UIAutomation.Browser;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.PowerApps.UIAutomation.Api
{

    /// <summary>
    ///  The Home page.
    ///  </summary>
    public class ModelDrivenApps
        : AppPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Home"/> class.
        /// </summary>
        /// <param name="browser">The browser.</param>
        public ModelDrivenApps(InteractiveBrowser browser)
            : base(browser)
        {
        }

        public BrowserCommandResult<bool> SelectGridRecord(int index, int thinkTime = Constants.DefaultThinkTime)
        {
            Browser.ThinkTime(thinkTime);

            return this.Execute(GetOptions("Select Grid Record"), driver =>
            {
                return true;
            });
        }
        public BrowserCommandResult<bool> SelectGridRecord(string name, int thinkTime = Constants.DefaultThinkTime)
        {
            Browser.ThinkTime(thinkTime);

            return this.Execute(GetOptions("Select Grid Record"), driver =>
            {
                //Need to click the <div>, not the <a>.  Selenium FindElements By.XPath misbehaved when trying to break into rows and cells
                //Get a collection of cells and find the cell with the record name
                var cells = driver.FindElements(By.XPath(Elements.Xpath[Reference.ModelDrivenApps.CellsContainer]));
                var cell = cells.FirstOrDefault(c => c.Text.Equals(name, StringComparison.OrdinalIgnoreCase));

                if(cell == null)
                    throw new InvalidOperationException($"No record with the name '{name}' exists in the grid.");

                cell.Click(true);


                /*bool found = false;

                foreach (var cell in cells)
                {
                    if (cell.Text.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        cell.Click(true);
                        break;
                    }
                }

                if(!found)
                    throw new InvalidOperationException($"No record with the name '{name}' exists in the grid.");
                    */
                return true;
            });
        }

        public BrowserCommandResult<bool> MoreCommands(string solutionName, string commandName, int thinkTime = Constants.DefaultThinkTime)
        {
            return this.Execute(GetOptions("Click More Commands Button"), driver =>
            {
                MoreCommands(solutionName, commandName, "", thinkTime);
                return true;
            });
        }
        public BrowserCommandResult<bool> MoreCommands(string solutionName, string commandName, string subButton = "", int thinkTime = Constants.DefaultThinkTime)
        {
            Browser.ThinkTime(thinkTime);

            return this.Execute(GetOptions("Click More Commands Button"), driver =>
            {
                ClickMoreCommandsButton(solutionName, commandName, subButton, false);
                return true;
            });
        }

        public BrowserCommandResult<bool> WaitForProcessingToComplete(string solutionName)
        {
            return this.Execute(GetOptions("Wait For Processing To Complete"), driver =>
            {
                string currentStatus = GetCurrentStatus(solutionName);

                if (currentStatus.Contains("running", StringComparison.OrdinalIgnoreCase))
                    WaitUntilStatusChanges(solutionName, currentStatus, 3600);

                return true;
            });
        }

        public BrowserCommandResult<bool> VerifyButtonIsClickable(string solutionName, string commandName, string subButton, bool throwExceptionIfVisible, int thinkTime = Constants.DefaultThinkTime)
        {
            Browser.ThinkTime(thinkTime);

            return this.Execute(GetOptions("Verify Button is Clickable"), driver =>
            {
                bool isDisabled = IsButtonDisabled(solutionName, commandName, subButton);

                if (throwExceptionIfVisible && !isDisabled)
                    throw new InvalidOperationException($"SubButton '{subButton}' should not be visible.");

                return true;
            });
        }

        internal bool ClickMoreCommandsButton(string solutionName, string commandName, string subButton = "", bool throwExceptionIfVisible = false)
        {
            var driver = Browser.Driver;

            //Need to click the <div>, not the <a>.  Selenium FindElements By.XPath misbehaved when trying to break into rows and cells
            //Get a collection of cells and find the cell with the record name
            var cells = driver.FindElements(By.XPath(Elements.Xpath[Reference.ModelDrivenApps.CellsContainer]));
            var cell = cells.FirstOrDefault(c => c.Text.Equals(solutionName, StringComparison.OrdinalIgnoreCase));

            if (cell == null)
                throw new InvalidOperationException($"No record with the name '{solutionName}' exists in the grid.");

            //Click on the More Commands menu
            var moreCommandsButton = cell.FindElement(By.XPath(Elements.Xpath[Reference.ModelDrivenApps.MoreCommandsButton]));
            moreCommandsButton.Click(true);

            //First Command button
            var moreCommandsContainer = driver.FindElement(By.XPath(Elements.Xpath[Reference.ModelDrivenApps.MoreCommandsContainer]));
            var buttons = moreCommandsContainer.FindElements(By.TagName("button"));
            var button = buttons.FirstOrDefault(b => b.Text.Contains(commandName, StringComparison.OrdinalIgnoreCase));

            if (button == null)
                throw new InvalidOperationException($"No command with the name '{commandName}' exists inside of Commandbar.");

            button.Click(true);

            Browser.ThinkTime(1500);

            //Sub Command Button
            if (!string.IsNullOrEmpty(subButton))
            {
                //found = false;
                var subButtonContainer = driver.FindElements(By.XPath(Elements.Xpath[Reference.ModelDrivenApps.SubButtonContainer]));
                var subButtons = subButtonContainer[1].FindElements(By.TagName("button"));

                var sButton = subButtons.FirstOrDefault(b => b.Text.Contains(subButton, StringComparison.OrdinalIgnoreCase));

                if (sButton == null)
                    throw new InvalidOperationException($"No subButton with the name '{subButton}' exists inside of the More Commands menu.");

                //Is the button visible?
                bool isDisabled;
                var currentVisibleStatus = sButton.GetAttribute("aria-disabled");
                bool.TryParse(currentVisibleStatus, out isDisabled);

                if (!isDisabled)
                    sButton.Click(true);
            }

            return true;
        }

        internal bool IsButtonDisabled(string solutionName, string commandName, string subButton = "")
        {
            var driver = Browser.Driver;
            bool isDisabled = true;

            ClickMoreCommandsButton(solutionName, commandName, "");

            var subButtonContainer = driver.FindElements(By.XPath(Elements.Xpath[Reference.ModelDrivenApps.SubButtonContainer]));

            if (subButtonContainer.Count == 0)
                Console.WriteLine("SubButton container is empty");

            var subButtons = subButtonContainer[1].FindElements(By.TagName("button"));
            var sButton = subButtons.FirstOrDefault(b => b.Text.Equals(subButton, StringComparison.OrdinalIgnoreCase));

            bool.TryParse(sButton.GetAttribute("aria-disabled"), out isDisabled);

            ClickMoreCommandsButton(solutionName, commandName, "");

            return isDisabled;
        }

        internal bool WaitUntilStatusChanges(string solutionName, string currentStatus, int maxWaitTimeInSeconds)
        {
            var driver = Browser.Driver;
            bool state = false;
            try
            {
                //Poll every half second to see if UCI is idle
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(500));
                wait.Until(d =>
                {
                    try
                    {
                        string thisStatus = GetCurrentStatus(solutionName);
                        //This is specific to DownloadResults - looking specifically for a status that begins with Results...
                        if (!thisStatus.Equals(currentStatus) && thisStatus.Contains("Results", StringComparison.OrdinalIgnoreCase))
                            state = true;
                    }
                    catch (TimeoutException)
                    {

                    }
                    catch (NullReferenceException)
                    {

                    }

                    return state;
                });
            }
            catch (Exception)
            {

            }

            return state;
        }
        internal string GetCurrentStatus(string solutionName)
        {
            var driver = Browser.Driver;
            //Retrieve current status.  XPath was misbehaving trying to traverse rows and columns so we have to cheat.
            var solutionNames = driver.FindElements(By.XPath(Elements.Xpath[Reference.CommandBar.GridSolutionNameColumn]));
            var solutionStatuses = driver.FindElements(By.XPath(Elements.Xpath[Reference.CommandBar.GridSolutionStatusColumn]));

            int rowNumber = -1;
            int cnt = 0;

            foreach (var row in solutionNames)
            {
                if (row.Text.Contains(solutionName, StringComparison.OrdinalIgnoreCase))
                {
                    rowNumber = cnt;
                    break;
                }

                cnt++;
            }

            if (rowNumber == -1)
                Console.WriteLine("Could not find status for this solution");
            return solutionStatuses[rowNumber].Text;
        }

        public BrowserCommandResult<bool> DownloadResults(string solutionName, int thinkTime = Constants.DefaultThinkTime)
        {
            Browser.ThinkTime(thinkTime);

            return this.Execute(GetOptions("Download Results"), driver =>
            {


                string currentStatus = GetCurrentStatus(solutionName);

                //Download results if/when complete
                if (currentStatus.Contains("Results", StringComparison.OrdinalIgnoreCase))
                {
                    //Click off the current record and back onto this one before downloading results
                    ClickMoreCommandsButton(solutionName, "Solution Checker", "Download last results");
                }

                return true;
            });
        }
    }
}

