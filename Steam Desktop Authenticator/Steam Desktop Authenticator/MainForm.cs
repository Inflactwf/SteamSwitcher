﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteamAuth;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace Steam_Desktop_Authenticator
{
    public partial class MainForm : Form
    {
        bool IsOffersList;
        string CurrentName;
        private SteamGuardAccount currentAccount = null;
        private SteamGuardAccount[] allAccounts;
        private List<string> updatedSessions = new List<string>();
        private Manifest manifest;
        private static SemaphoreSlim confirmationsSemaphore = new SemaphoreSlim(1, 1);

        private long steamTime = 0;
        private long currentSteamChunk = 0;
        private string passKey = null;
        bool ExistArgs = false;
        bool NeedToShowForm = false;
        bool RefreshSession = false;

        // Forms
        private TradePopupForm popupFrm = new TradePopupForm();

        public MainForm(string arg)
        {
            InitializeComponent();

            if (arg.Length != 0)
            {
                if (arg.Contains("-DirectImport:"))
                {
                    IsOffersList = false;
                    CurrentName = arg.Substring(14);
                }
                else if (arg.Contains("-OpenOffers:"))
                {
                    IsOffersList = true;
                    CurrentName = arg.Substring(12);
                    ExistArgs = true;
                }
                else if (arg.Contains("-Refresh:"))
                {
                    RefreshSession = true;
                    CurrentName = arg.Substring(9);
                }
                else if (arg.Contains("-ShowForm"))
                {
                    NeedToShowForm = true;
                    this.Opacity = 1;
                    this.ShowInTaskbar = true;
                }
            }
        }

        public string AccountName
        {
            get
            {
                try
                {
                    string[] GetAccountString = CurrentName.Split(new char[] { '=' });
                    return GetAccountString[0].ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public string AccountPassword
        {
            get
            {
                try
                {
                    string[] GetAccountString = CurrentName.Split(new char[] { '=' });
                    return GetAccountString[1].ToString();
                }
                catch
                {
                    return null;
                }
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams pm = base.CreateParams;
                pm.ExStyle |= 0x80;
                return pm;
            }
        }

        // Form event handlers

        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.labelVersion.Text = String.Format("v{0}", Application.ProductVersion);
            this.manifest = Manifest.GetManifest();

            // Make sure we don't show that welcome dialog again
            this.manifest.FirstRun = false;
            this.manifest.Save();

            // Tick first time manually to sync time
            timerSteamGuard_Tick(new object(), EventArgs.Empty);

            if (manifest.Encrypted)
            {
                passKey = manifest.PromptForPassKey();
                if (passKey == null)
                {
                    Application.Exit();
                }

                btnManageEncryption.Text = "Настройки безопасности";
            }
            else
            {
                btnManageEncryption.Text = "Установка безопасности";
            }

            btnManageEncryption.Enabled = manifest.Entries.Count > 0;

            loadSettings();
            loadAccountsList();

            if (NeedToShowForm == false)
            {
                if (IsOffersList)
                {
                    if (listAccounts.Items.Contains(CurrentName))
                    {
                        listAccounts.SelectedItem = CurrentName;
                        OpenOffers();
                    }
                    else
                    {
                        MessageBox.Show("В списке аккаунтов не найден '" + CurrentName + "'." + Environment.NewLine + "Пожалуйста, добавьте его в 'Steam Switcher Authenticator' (/bin/SSA) прежде, чем использовать его в Steam Switcher.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                }
                else if (RefreshSession)
                {
                    if (listAccounts.Items.Contains(AccountName))
                    {
                        listAccounts.SelectedItem = AccountName;
                        DirectRefresh(currentAccount, AccountPassword, false);
                    }
                    else
                    {
                        Application.Exit();
                    }
                }
                else
                {
                    ImportAccountForm DirImp = new ImportAccountForm(true, CurrentName);
                    DirImp.Show();
                }
            }
        }

        public void Wait(int ms)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < ms)
                Application.DoEvents();
        }

        private async void OpenOffers()
        {
            if (currentAccount == null) return;
            await RefreshAccountSession(currentAccount);

            try
            {
                ConfirmationFormWeb confirms = new ConfirmationFormWeb(currentAccount, ExistArgs);
                confirms.Show();
            }
            catch (Exception)
            {
                DialogResult res = MessageBox.Show("You are missing a dependency required to view your trade confirmations.\nWould you like to install it now?", "Trade confirmations failed to open", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    new InstallRedistribForm(true).ShowDialog();
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        // UI Button handlers

        private void btnSteamLogin_Click(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            loginForm.ShowDialog();
            this.loadAccountsList();
        }

        private void btnManageEncryption_Click(object sender, EventArgs e)
        {
            if (manifest.Encrypted)
            {
                InputForm currentPassKeyForm = new InputForm("Enter current passkey", true);
                currentPassKeyForm.ShowDialog();

                if (currentPassKeyForm.Canceled)
                {
                    return;
                }

                string curPassKey = currentPassKeyForm.txtBox.Text;

                InputForm changePassKeyForm = new InputForm("Enter new passkey, or leave blank to remove encryption.");
                changePassKeyForm.ShowDialog();

                if (changePassKeyForm.Canceled && !string.IsNullOrEmpty(changePassKeyForm.txtBox.Text))
                {
                    return;
                }

                InputForm changePassKeyForm2 = new InputForm("Confirm new passkey, or leave blank to remove encryption.");
                changePassKeyForm2.ShowDialog();

                if (changePassKeyForm2.Canceled && !string.IsNullOrEmpty(changePassKeyForm.txtBox.Text))
                {
                    return;
                }

                string newPassKey = changePassKeyForm.txtBox.Text;
                string confirmPassKey = changePassKeyForm2.txtBox.Text;

                if (newPassKey != confirmPassKey)
                {
                    MessageBox.Show("Passkeys do not match.");
                    return;
                }

                if (newPassKey.Length == 0)
                {
                    newPassKey = null;
                }

                string action = newPassKey == null ? "remove" : "change";
                if (!manifest.ChangeEncryptionKey(curPassKey, newPassKey))
                {
                    MessageBox.Show("Unable to " + action + " passkey.");
                }
                else
                {
                    MessageBox.Show("Passkey successfully " + action + "d.");
                    this.loadAccountsList();
                }
            }
            else
            {
                passKey = manifest.PromptSetupPassKey();
                this.loadAccountsList();
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(txtLoginToken.Text);
        }


        // Tool strip menu handlers

        private void menuQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void menuRemoveAccountFromManifest_Click(object sender, EventArgs e)
        {
            if (manifest.Encrypted)
            {
                MessageBox.Show("You cannot remove accounts from the manifest file while it is encrypted.", "Remove from manifest", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                DialogResult res = MessageBox.Show("This will remove the selected account from the manifest file.\nUse this to move a maFile to another computer.\nThis will NOT delete your maFile.", "Remove from manifest", MessageBoxButtons.OKCancel);
                if (res == DialogResult.OK)
                {
                    manifest.RemoveAccount(currentAccount, false);
                    MessageBox.Show("Account removed from manifest.\nYou can now move its maFile to another computer and import it using the File menu.", "Remove from manifest");
                    loadAccountsList();
                }
            }
        }

        private void menuLoginAgain_Click(object sender, EventArgs e)
        {
            this.PromptRefreshLogin(currentAccount);
        }

        private void menuImportMaFile_Click(object sender, EventArgs e)
        {
            ImportAccountForm currentImport_maFile_Form = new ImportAccountForm();
            currentImport_maFile_Form.ShowDialog();
            loadAccountsList();
        }

        private void menuImportAndroid_Click(object sender, EventArgs e)
        {
            new PhoneExtractForm().ShowDialog();
        }

        private void menuSettings_Click(object sender, EventArgs e)
        {
            manifest = Manifest.GetManifest(true);
            loadSettings();
        }

        private void menuDeactivateAuthenticator_Click(object sender, EventArgs e)
        {
            if (currentAccount == null) return;

            DialogResult res = MessageBox.Show("Would you like to remove Steam Guard completely?\nYes - Remove Steam Guard completely.\nNo - Switch back to Email authentication.", "Remove Steam Guard", MessageBoxButtons.YesNoCancel);
            int scheme = 0;
            if (res == DialogResult.Yes)
            {
                scheme = 2;
            }
            else if (res == DialogResult.No)
            {
                scheme = 1;
            }
            else if (res == DialogResult.Cancel)
            {
                scheme = 0;
            }

            if (scheme != 0)
            {
                string confCode = currentAccount.GenerateSteamGuardCode();
                InputForm confirmationDialog = new InputForm(String.Format("Removing Steam Guard from {0}. Enter this confirmation code: {1}", currentAccount.AccountName, confCode));
                confirmationDialog.ShowDialog();

                if (confirmationDialog.Canceled)
                {
                    return;
                }

                string enteredCode = confirmationDialog.txtBox.Text.ToUpper();
                if (enteredCode != confCode)
                {
                    MessageBox.Show("Confirmation codes do not match. Steam Guard not removed.");
                    return;
                }

                bool success = currentAccount.DeactivateAuthenticator(scheme);
                if (success)
                {
                    MessageBox.Show(String.Format("Steam Guard {0}. maFile will be deleted after hitting okay. If you need to make a backup, now's the time.", (scheme == 2 ? "removed completely" : "switched to emails")));
                    this.manifest.RemoveAccount(currentAccount);
                    this.loadAccountsList();
                }
                else
                {
                    MessageBox.Show("Steam Guard failed to deactivate.");
                }
            }
            else
            {
                MessageBox.Show("Steam Guard was not removed. No action was taken.");
            }
        }

        private async void menuRefreshSession_Click(object sender, EventArgs e)
        {
            bool status = await RefreshAccountSession(currentAccount);
            if (status == true)
            {
                MessageBox.Show("Your session has been refreshed.", "Session refresh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                manifest.SaveAccount(currentAccount, manifest.Encrypted, passKey);
            }
            else
            {
                MessageBox.Show("Failed to refresh your session.\nTry again soon.", "Session refresh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Tray menu handlers
        private void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            trayRestore_Click(sender, EventArgs.Empty);
        }

        private void trayRestore_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void trayQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trayCopySteamGuard_Click(object sender, EventArgs e)
        {
            if (txtLoginToken.Text != "")
            {
                Clipboard.SetText(txtLoginToken.Text);
            }
        }

        private void trayAccountList_SelectedIndexChanged(object sender, EventArgs e)
        {
            listAccounts.SelectedIndex = trayAccountList.SelectedIndex;
        }


        // Misc UI handlers
        private void listAccounts_SelectedValueChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < allAccounts.Length; i++)
            {
                SteamGuardAccount account = allAccounts[i];
                if (account.AccountName == (string)listAccounts.Items[listAccounts.SelectedIndex])
                {
                    trayAccountList.Text = account.AccountName;
                    currentAccount = account;
                    loadAccountInfo();
                    break;
                }
            }
        }

        private void txtAccSearch_TextChanged(object sender, EventArgs e)
        {
            List<string> names = new List<string>(getAllNames());
            names = names.FindAll(new Predicate<string>(IsFilter));

            listAccounts.Items.Clear();
            listAccounts.Items.AddRange(names.ToArray());

            trayAccountList.Items.Clear();
            trayAccountList.Items.AddRange(names.ToArray());
        }


        // Timers

        private async void timerSteamGuard_Tick(object sender, EventArgs e)
        {
            lblStatus.Text = "Aligning time with Steam...";
            steamTime = await TimeAligner.GetSteamTimeAsync();
            lblStatus.Text = "";

            currentSteamChunk = steamTime / 30L;
            int secondsUntilChange = (int)(steamTime - (currentSteamChunk * 30L));

            loadAccountInfo();
            if (currentAccount != null)
            {
                pbTimeout.Value = 30 - secondsUntilChange;
            }
        }

        private async void timerTradesPopup_Tick(object sender, EventArgs e)
        {
            if (currentAccount == null || popupFrm.Visible) return;
            if (!confirmationsSemaphore.Wait(0))
            {
                return; //Only one thread may access this critical section at once. Mutex is a bad choice here because it'll cause a pileup of threads.
            }

            List<Confirmation> confs = new List<Confirmation>();
            Dictionary<SteamGuardAccount, List<Confirmation>> autoAcceptConfirmations = new Dictionary<SteamGuardAccount, List<Confirmation>>();

            SteamGuardAccount[] accs = manifest.CheckAllAccounts ? allAccounts : new SteamGuardAccount[] { currentAccount };

            try
            {
                lblStatus.Text = "Checking confirmations...";

                foreach (var acc in accs)
                {
                    try
                    {
                        Confirmation[] tmp = await currentAccount.FetchConfirmationsAsync();
                        foreach (var conf in tmp)
                        {
                            if ((conf.ConfType == Confirmation.ConfirmationType.MarketSellTransaction && manifest.AutoConfirmMarketTransactions) ||
                                (conf.ConfType == Confirmation.ConfirmationType.Trade && manifest.AutoConfirmTrades))
                            {
                                if (!autoAcceptConfirmations.ContainsKey(acc))
                                    autoAcceptConfirmations[acc] = new List<Confirmation>();
                                autoAcceptConfirmations[acc].Add(conf);
                            }
                            else
                                confs.Add(conf);
                        }
                    }
                    catch (SteamGuardAccount.WGTokenInvalidException)
                    {
                        lblStatus.Text = "Refreshing session";
                        await currentAccount.RefreshSessionAsync(); //Don't save it to the HDD, of course. We'd need their encryption passkey again.
                        lblStatus.Text = "";
                    }
                    catch (SteamGuardAccount.WGTokenExpiredException)
                    {
                        //Prompt to relogin
                        PromptRefreshLogin(currentAccount);
                        break; //Don't bombard a user with login refresh requests if they have multiple accounts. Give them a few seconds to disable the autocheck option if they want.
                    }
                    catch (WebException)
                    {

                    }
                }

                lblStatus.Text = "";

                if (confs.Count > 0)
                {
                    popupFrm.Confirmations = confs.ToArray();
                    popupFrm.Popup();
                }
                if (autoAcceptConfirmations.Count > 0)
                {
                    foreach (var acc in autoAcceptConfirmations.Keys)
                    {
                        var confirmations = autoAcceptConfirmations[acc].ToArray();
                        acc.AcceptMultipleConfirmations(confirmations);
                    }
                }
            }
            catch (SteamGuardAccount.WGTokenInvalidException)
            {
                lblStatus.Text = "";
            }

            confirmationsSemaphore.Release();
        }

        // Other methods

        /// <summary>
        /// Refresh this account's session data using their OAuth Token
        /// </summary>
        /// <param name="account">The account to refresh</param>
        /// <param name="attemptRefreshLogin">Whether or not to prompt the user to re-login if their OAuth token is expired.</param>
        /// <returns></returns>
        private async Task<bool> RefreshAccountSession(SteamGuardAccount account, bool attemptRefreshLogin = true)
        {
            if (account == null) return false;

            try
            {
                bool refreshed = await account.RefreshSessionAsync();
                return refreshed; //No exception thrown means that we either successfully refreshed the session or there was a different issue preventing us from doing so.
            }
            catch (SteamGuardAccount.WGTokenExpiredException)
            {
                if (!attemptRefreshLogin) return false;

                PromptRefreshLogin(account);

                return await RefreshAccountSession(account, false);
            }
        }

        /// <summary>
        /// Display a login form to the user to refresh their OAuth Token
        /// </summary>
        /// <param name="account">The account to refresh</param>
        private void PromptRefreshLogin(SteamGuardAccount account)
        {
            var loginForm = new LoginForm(LoginForm.LoginType.Refresh, account);
            loginForm.ShowDialog();
        }

        /// <summary>
        /// Display a login form to the user to refresh their OAuth Token
        /// </summary>
        /// <param name="account">The account to refresh</param>
        private void DirectRefresh(SteamGuardAccount account, string pwd, bool visible)
        {
            var loginForm = new LoginForm(LoginForm.LoginType.Refresh, account, pwd, visible);
            loginForm.ShowDialog();
        }

        /// <summary>
        /// Load UI with the current account info, this is run every second
        /// </summary>
        private void loadAccountInfo()
        {
            if (currentAccount != null && steamTime != 0)
            {
                popupFrm.Account = currentAccount;
                txtLoginToken.Text = currentAccount.GenerateSteamGuardCodeForTime(steamTime);
            }
        }

        /// <summary>
        /// Decrypts files and populates list UI with accounts
        /// </summary>
        private void loadAccountsList()
        {
            currentAccount = null;

            listAccounts.Items.Clear();
            listAccounts.SelectedIndex = -1;

            trayAccountList.Items.Clear();
            trayAccountList.SelectedIndex = -1;

            allAccounts = manifest.GetAllAccounts(passKey);

            if (allAccounts.Length > 0)
            {
                for (int i = 0; i < allAccounts.Length; i++)
                {
                    SteamGuardAccount account = allAccounts[i];
                    listAccounts.Items.Add(account.AccountName);
                    trayAccountList.Items.Add(account.AccountName);
                }

                listAccounts.SelectedIndex = 0;
                trayAccountList.SelectedIndex = 0;
            }
            menuDeactivateAuthenticator.Enabled = allAccounts.Length > 0;
        }

        private void listAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                {
                    int to = listAccounts.SelectedIndex - (e.KeyCode == Keys.Up ? 1 : -1);
                    manifest.MoveEntry(listAccounts.SelectedIndex, to);
                    loadAccountsList();
                }
                return;
            }

            if (!IsKeyAChar(e.KeyCode) && !IsKeyADigit(e.KeyCode))
            {
                return;
            }

            txtAccSearch.Focus();
            txtAccSearch.Text = e.KeyCode.ToString();
            txtAccSearch.SelectionStart = 1;
        }

        private static bool IsKeyAChar(Keys key)
        {
            return key >= Keys.A && key <= Keys.Z;
        }

        private static bool IsKeyADigit(Keys key)
        {
            return (key >= Keys.D0 && key <= Keys.D9) || (key >= Keys.NumPad0 && key <= Keys.NumPad9);
        }

        private bool IsFilter(string f)
        {
            if (txtAccSearch.Text.StartsWith("~"))
            {
                try
                {
                    return Regex.IsMatch(f, txtAccSearch.Text);
                }
                catch (Exception)
                {
                    return true;
                }

            }
            else
            {
                return f.Contains(txtAccSearch.Text);
            }
        }

        private string[] getAllNames()
        {
            string[] itemArray = new string[allAccounts.Length];
            for (int i = 0; i < itemArray.Length; i++)
            {
                itemArray[i] = allAccounts[i].AccountName;
            }
            return itemArray;
        }

        private void loadSettings()
        {
            if (NeedToShowForm)
            {
                timerTradesPopup.Enabled = false;
            }
            else
            {
                if (IsOffersList)
                {
                    timerTradesPopup.Enabled = false;
                }
                else
                {
                    timerTradesPopup.Enabled = true;
                    timerTradesPopup.Interval = 15000;
                }
            }
        }

        // Logic for version checking
        private Version newVersion = null;
        private Version currentVersion = null;
        private string updateUrl = null;
        private bool startupUpdateCheck = true;

        private void compareVersions()
        {
            if (newVersion > currentVersion)
            {
                DialogResult updateDialog = MessageBox.Show(String.Format("A new version is available! Would you like to download it now?\nYou will update from version {0} to {1}", Application.ProductVersion, newVersion.ToString()), "New Version", MessageBoxButtons.YesNo);
                if (updateDialog == DialogResult.Yes)
                {
                    Process.Start(updateUrl);
                }
            }
            else
            {
                if (!startupUpdateCheck)
                {
                    MessageBox.Show(String.Format("You are using the latest version: {0}", Application.ProductVersion));
                }
            }

            newVersion = null; // Check the api again next time they check for updates
            startupUpdateCheck = false; // Set when it's done checking on startup
        }

        private void UpdateClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                dynamic resultObject = JsonConvert.DeserializeObject(e.Result);
                newVersion = new Version(resultObject.tag_name.Value);
                currentVersion = new Version(Application.ProductVersion);
                updateUrl = resultObject.assets.First.browser_download_url.Value;
                compareVersions();
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to check for updates.");
            }
        }
    }
}
