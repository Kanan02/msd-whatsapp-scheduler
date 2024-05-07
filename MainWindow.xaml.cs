using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using System.Windows;
using msd_whatsapp_scheduler.Models;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace msd_whatsapp_scheduler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ScheduledMessage> ScheduledMessages { get; set; } = new ObservableCollection<ScheduledMessage>();
        private IWebDriver driver;
        private string defaultContactName = "TestChannel";
        private string cookieFilePath = "whatsappCookies.txt";

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebDriver();
            DataContext = this;
            txtYouTubeURL.TextChanged += UpdateMessagePreview;
            txtRBTCode.TextChanged += UpdateMessagePreview;
        }

        private void InitializeWebDriver()
        {
            driver = new ChromeDriver();
            driver.Navigate().GoToUrl("https://web.whatsapp.com");
            channelName.Text = "TestChannel";
            if (File.Exists(cookieFilePath))
            {
                try
                {
                    LoadCookies();
                    driver.Navigate().Refresh(); // Refresh page to apply cookies
                    if (IsLoggedIn())
                    {
                        MessageBox.Show("Session restored. No need to scan QR code.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to restore session: {ex.Message}");
                }
            }

            MessageBox.Show("Please scan the QR code and then click OK to continue.");
            SaveCookies(); // Save cookies after successful QR scan
        }
        private void UpdateMessagePreview(object sender, EventArgs e)
        {
            // Constructing the message
            string youtubeUrl = txtYouTubeURL.Text;
            string rbtCode = txtRBTCode.Text;
            txtPreview.Text = $"Check out this video! {youtubeUrl} Use this RBT code: {rbtCode}";
        }

        private void btnSchedule_Click(object sender, RoutedEventArgs e)
        {
            DateTime scheduleDateTime = dateTimePicker.Value ?? DateTime.Now; // Safety check in case the picker is null
            string message = txtPreview.Text; // Use the edited message from the preview box

            // Adding the message to the schedule list
            ScheduledMessages.Add(new ScheduledMessage { ContactName = channelName.Text, Message = message, ScheduleDateTime = scheduleDateTime });
            Task.Run(() => CheckForScheduledMessages());
            MessageBox.Show($"Message was scheduled on {scheduleDateTime}!","Success",MessageBoxButton.OK,MessageBoxImage.Information );
        }

        private async Task CheckForScheduledMessages()
        {
            while (ScheduledMessages.Count > 0)
            {
                foreach (var msg in ScheduledMessages.ToArray())
                {
                    if (DateTime.Now >= msg.ScheduleDateTime)
                    {
                        SendMessageToChannel(msg.ContactName, msg.Message);
                        ScheduledMessages.Remove(msg);
                    }
                }
                await Task.Delay(1000);
            }
        }


        private bool IsLoggedIn()
        {
            // Check for an element that is only visible when logged in (e.g., the search input box)
            try
            {
                return driver.FindElement(By.CssSelector("input[type='text'][title='Search or start new chat']")) != null;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private void SaveCookies()
        {
            var cookies = driver.Manage().Cookies.AllCookies;
            using (var fileStream = File.CreateText(cookieFilePath))
            {
                foreach (var cookie in cookies)
                {
                    fileStream.WriteLine($"{cookie.Name} {cookie.Value} {cookie.Domain} {cookie.Path} {cookie.Expiry} {cookie.Secure}");
                }
            }
        }

        private void LoadCookies()
        {
            using (var fileStream = File.OpenRead(cookieFilePath))
            using (var reader = new StreamReader(fileStream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var data = line.Split(' ');
                    var cookie = new Cookie(data[0], data[1], data[2], data[3], Convert.ToDateTime(data[4]));
                    driver.Manage().Cookies.AddCookie(cookie);
                }
            }
        }

        private void SendMessage(string contactName, string message)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                IWebElement searchBox;

                try
                {
                    //searchBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//span[@title='" + contactName + "']")));
                    searchBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//span[@title='" + contactName + "']")));
                        //*[@id="app"]/div/div[2]/div[2]/div[1]/span/div/span/div/div/div/div[1]/div/div/div[2]/div[2]/div/div[1]/p
                        //*[@id="app"]/div/div[2]/div[2]/div[1]/span/div/span/div/div/div/div[2]/div[1]/div/div/div/div/div/div/div[1]/div[2]/span
                }
                catch
                {
                    Console.WriteLine("Search Contact, Using Search Box");
                    searchBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id='side']/div[1]/div/div[2]/div[2]/div/div[1]/p")));
                    searchBox.Click();
                    searchBox.SendKeys(contactName);
                    Thread.Sleep(5000); // Wait for contact to appear
                    searchBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//span[@title='" + contactName + "']")));
                }

                searchBox.Click();
                Thread.Sleep(3000); // Wait for chat to open

                // Find the message input box and type the message
                IWebElement content = driver.SwitchTo().ActiveElement();
                content.SendKeys(message);
              //  Thread.Sleep(2000); //Wait for youtube link to add icon 
                content.SendKeys(Keys.Return);
                Thread.Sleep(1000); // Ensure message is sent
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }
        private void SendMessageToChannel(string channelName, string message)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

                // Focus the search bar
                IWebElement searchBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id='app']/div/div[2]/div[2]/div[1]/span/div/span/div/div/div/div[1]/div/div/div[2]/div[2]/div/div[1]/p")));
                searchBox.Click();
                searchBox.Clear();
                searchBox.SendKeys(channelName);
                Thread.Sleep(2000); // Allow time for the search results to appear

                // Select the channel from the search results
                IWebElement channel = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id='app']/div/div[2]/div[2]/div[1]/span/div/span/div/div/div/div[2]/div[1]/div/div/div/div/div/div/div[1]/div[2]/span")));
                channel.Click();
                Thread.Sleep(3000); // Wait for the channel to open

                // Find the message input box and type the message
                IWebElement content = driver.SwitchTo().ActiveElement();
                content.SendKeys(message);
                Thread.Sleep(5000);
                content.SendKeys(Keys.Return);
                Thread.Sleep(1000); // Ensure message is sent
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

    }
}
