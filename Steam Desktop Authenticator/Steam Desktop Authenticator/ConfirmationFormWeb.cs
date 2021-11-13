using System;
using System.Windows.Forms;
using System.Diagnostics;
using CefSharp;
using CefSharp.WinForms;
using SteamAuth;
using Microsoft.Win32;

namespace Steam_Desktop_Authenticator
{
    public partial class ConfirmationFormWeb : Form
    {
        private readonly ChromiumWebBrowser browser;
        private string steamCookies;
        private SteamGuardAccount steamAccount;
        private string tradeID;
        UserActivityHook ActHook;
        bool HaveArgs = false;
        bool AllowToMove = true;

        public void GetKey(object sender, KeyEventArgs e)
        {
            if (browser.Focused || this.Focused || splitContainer1.Focused || btnRefresh.Focused)
            {
                if (e.KeyCode == Keys.F5)
                {
                    btnRefresh.PerformClick();
                }
                else if (e.KeyCode == Keys.F1)
                {
                    browser.ShowDevTools();
                }
            }
        }

        public ConfirmationFormWeb(SteamGuardAccount steamAccount, bool WithArgs)
        {
            InitializeComponent();
            ActHook = new UserActivityHook();
            ActHook.KeyDown += new KeyEventHandler(GetKey);
            ActHook.Start();

            if (WithArgs != false)
            {
                HaveArgs = true;
            }
            this.steamAccount = steamAccount;
            this.Text = steamAccount.AccountName + ": подтверждения обмена";

            CefSettings settings = new CefSettings();
            settings.PersistSessionCookies = false;
            settings.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 6P Build/XXXXX; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/47.0.2526.68 Mobile Safari/537.36";
            steamCookies = String.Format("mobileClientVersion=0 (2.1.3); mobileClient=android; steamid={0}; steamLogin={1}; steamLoginSecure={2}; Steam_Language=russian; dob=;", steamAccount.Session.SteamID.ToString(), steamAccount.Session.SteamLogin, steamAccount.Session.SteamLoginSecure);

            if (!Cef.IsInitialized)
            {
                Cef.Initialize(settings);
            }

            browser = new ChromiumWebBrowser(steamAccount.GenerateConfirmationURL())
            {
                Dock = DockStyle.Fill,
            };
            this.splitContainer1.Panel2.Controls.Add(browser);

            BrowserRequestHandler handler = new BrowserRequestHandler();
            handler.Cookies = steamCookies;
            browser.RequestHandler = handler;
            browser.AddressChanged += Browser_AddressChanged;
            browser.LoadingStateChanged += Browser_LoadingStateChanged;
        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            // This looks really ugly, but it's easier than implementing steam's steammobile:// protocol using CefSharp
            // We override the page's GetValueFromLocalURL() to pass in the keys for sending ajax requests
            if (e.IsLoading == false)
            {
                // Generate url for details
                string urlParams = steamAccount.GenerateConfirmationQueryParams("details" + tradeID);

                var script = string.Format(@"window.GetValueFromLocalURL = 
                function(url, timeout, success, error, fatal) {{            
                    console.log(url);
                    if(url.indexOf('steammobile://steamguard?op=conftag&arg1=allow') !== -1) {{
                        // send confirmation (allow)
                        success('{0}');
                    }} else if(url.indexOf('steammobile://steamguard?op=conftag&arg1=cancel') !== -1) {{
                        // send confirmation (cancel)
                        success('{1}');
                    }} else if(url.indexOf('steammobile://steamguard?op=conftag&arg1=details') !== -1) {{
                        // get details
                        success('{2}');
                    }}
                }}", steamAccount.GenerateConfirmationQueryParams("allow"), steamAccount.GenerateConfirmationQueryParams("cancel"), urlParams);
                browser.ExecuteScriptAsync(script);
            }
        }

        private void Browser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            string[] urlparts = browser.Address.Split('#');
            if (urlparts.Length > 1)
            {
                tradeID = urlparts[1].Replace("conf_", "");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            browser.Load(steamAccount.GenerateConfirmationURL());
        }

        private void ConfirmationFormWeb_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (HaveArgs)
            {
                Application.Exit();
            }
        }

        protected override void WndProc(ref Message m)
        {
            try
            {
                if (!AllowToMove)
                {
                    const int WM_SYSCOMMAND = 0x0112;
                    const int SC_MOVE = 0xF010;

                    switch (m.Msg)
                    {
                        case WM_SYSCOMMAND:
                            int command = m.WParam.ToInt32() & 0xfff0;
                            if (command == SC_MOVE)
                                return;
                            break;
                    }
                }
                base.WndProc(ref m);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unhandled Exception" + Environment.NewLine + ex.StackTrace);
                Environment.Exit(0);
            }
        }

        private void FixBtn_Click(object sender, EventArgs e)
        {
            TopMost = true;
            FixBtn.Enabled = false;
            MoveBtn.Enabled = true;
            AllowToMove = false;
        }

        private void MoveBtn_Click(object sender, EventArgs e)
        {
            TopMost = false;
            MoveBtn.Enabled = false;
            FixBtn.Enabled = true;
            AllowToMove = true;
        }
    }
}