﻿using System;
using System.Globalization;
using System.Windows;
using Shared;
using Shared.Data;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Application;
using Timer = System.Threading.Timer;

namespace Retrospection
{
    /// <summary>
    /// Interaction logic for MiniRetrospectionWindow.xaml
    /// </summary>
    public partial class MiniRetrospectionWindow : Window
    {
        private readonly System.Windows.Forms.WebBrowser _webBrowser;
        private string _currentPage;
        private Timer _closeMiniRetrospectionTimer;


        public MiniRetrospectionWindow()
        {
            InitializeComponent();
            _webBrowser = (wbWinForms.Child as System.Windows.Forms.WebBrowser);
        }

        /// <summary>
        /// override ShowDialog method to place it on the bottom right corner
        /// of the developer's screen
        /// </summary>
        /// <returns></returns>
        public new bool? ShowDialog()
        {
            const int windowWidth = 460; //this.ActualWidth;
            const int windowHeight = 300; //this.ActualHeight;

            this.Topmost = true;
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;
            //this.Owner = Application.Current.MainWindow;

            //this.Closed += this.DailyProductivityPopUp_OnClosed;

            this.Left = SystemParameters.PrimaryScreenWidth - windowWidth;
            var top = SystemParameters.PrimaryScreenHeight - windowHeight;

            foreach (Window window in Application.Current.Windows)
            {
                var windowName = window.GetType().Name;

                if (!windowName.Equals("MiniRetrospectionWindow") || window == this) continue;
                window.Topmost = true;
                top = window.Top - windowHeight;
            }

            this.Top = top;

            StartFadeTimer();
            return base.ShowDialog();
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            //var stream = OnStats();
            //todo: path issues: http://stackoverflow.com/questions/27661986/include-static-js-and-css-webbrowser-control
            //webBrowser.NavigateToString(stream);

#if !DEBUG
    webBrowser.ScriptErrorsSuppressed = true;
#endif

            //_webBrowser.Navigating += (o, ex) =>
            //{
            //    ShowLoading(true);
            //};

            _webBrowser.Navigated += (o, ex) =>
            {
                //ShowLoading(false);

#if DEBUG
                _webBrowser.Document.Window.Error += (w, we) =>
                {
                    we.Handled = true;
                    Logger.WriteToConsole(string.Format(CultureInfo.InvariantCulture, "# URL:{1}, LN: {0}, ERROR: {2}",
                        we.LineNumber, we.Url, we.Description));
                };
#endif
            };

            _webBrowser.IsWebBrowserContextMenuEnabled = false;
            _webBrowser.ObjectForScripting = new ObjectForScriptingHelper();
                // allows to use javascript to call functions in this class
            _webBrowser.WebBrowserShortcutsEnabled = false;
            _webBrowser.AllowWebBrowserDrop = false;


            // load default page
            WebBrowserNavigateTo(Handler.GetInstance().GetMiniDashboard());
        }

        public void StartFadeTimer()
        {
            _closeMiniRetrospectionTimer = new Timer(new TimerCallback(TimerTick), // callback
                            null,  // no idea
                            20000, // start immediately after 10 seconds
                            Timeout.Infinite); // interval
        }

        private void TimerTick(object state)
        {
            this.Dispatcher.Invoke((MethodInvoker) delegate
            {
                //close the form on the forms thread
                this.Close();
            });
                
            if (_closeMiniRetrospectionTimer != null)
            {
                _closeMiniRetrospectionTimer.Dispose();
                _closeMiniRetrospectionTimer = null;
            }
        }

        /// <summary>
        /// Navigate the browser to the url in case the web browser is live, 
        /// the url is ready and the same is not the same
        /// </summary>
        /// <param name="url"></param>
        /// <param name="navigateEnforced"></param>
        private void WebBrowserNavigateTo(string url, bool navigateEnforced = false)
        {
            if (_webBrowser == null || url == null) return;

            if (_currentPage != url || navigateEnforced == true)
            {
                _currentPage = url;
                _webBrowser.Navigate(url);
                Database.GetInstance().LogInfo("Mini-Retrospection, navigated to: " + url);
            }
        }

        private void SeeDetails_Clicked(object sender, RoutedEventArgs e)
        {
            Handler.GetInstance().OpenRetrospection();
            Close();
        }
    }
}
