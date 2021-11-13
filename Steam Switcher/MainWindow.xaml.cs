using Newtonsoft.Json;
using Steam_Switcher.Functions;
using Steam_Switcher.Models;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;

namespace Steam_Switcher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public ObservableCollection<AccountsModel> AccountsCollection { get; set; }
        public AccountsModel SelectedAccount { get; set; }
        private CacheWorker CacheWorker;
        private CurrentTab CurTab = CurrentTab.Information;
        private SteamGuardAccount LoginAccount;
        private ErrorLog ErrorLog;
        private Process ConfirmationProcess;
        private FolderBrowserDialog SetGameFolder;
        private OpenFileDialog DialogFinder;
        private OpenFileDialog EditDialogFinder;
        private OpenFileDialog MaFileDialog;
        private OpenFileDialog EditMaFileDialog;
        private OpenFileDialog AccountFileDialog;
        private OpenFileDialog maFile;
        private Timer AccountListChecker;
        private Timer CodeRefresher;
        private Timer EndAnimationTimer;
        private Timer IdleTimer;
        private AdvancedInformation AdvInformation;
        private AdvancedInformation SettingsInformation;
        private AdvancedInformation AI;
        private AdvHelper helper;
        private string location;
        private long SteamTimeCallback;
        private long CurrentSteamChunk;
        private bool PasswordChanged = false;
        private bool IsSSFNChoosed;
        private bool IsEditSSFNChoosed;
        private bool IsMaFileSelected;
        private bool IsEditMaFileSelected;
        private bool AccExists;
        private string SSFNFolder;
        private string ConfigFolder;
        private string SSAFolder;
        public string CacheFolder;
        private string RootDirectory;
        private int EnableNotifications;
        private LoginManager loginManager;

        public MainWindow()
        {
            InitializeComponent();

            loginManager = new LoginManager();
            CacheWorker = new CacheWorker();
            SteamTimeCallback = 0;
            CurrentSteamChunk = 0;
            AccountPanelGrid2.Margin = new Thickness(-743, 267, 0, 0);
            WindowStyle = WindowStyle.SingleBorderWindow;
            EnableNotifications = 1; // 0 - никакие, 1 - важные, 2 - все.

            SSFNFolder = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\ssfn\";
            ConfigFolder = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\config\";
            SSAFolder = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\bin\SSA\";
            RootDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\";
            CacheFolder = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\cache\";

            ErrorLog = new ErrorLog();
            LoginAccount = new SteamGuardAccount();
            AdvInformation = new AdvancedInformation(ConfigFolder + @"AdvancedInformation.lst");
            SettingsInformation = new AdvancedInformation(ConfigFolder + @"Settings.cfg");
            helper = new AdvHelper();

            CodeRefresher = new Timer
            {
                Interval = 1000,
                Enabled = true
            };
            CodeRefresher.Tick += new EventHandler(CodeRefresher_Tick);

            maFile = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = "maFile",
                Filter = "Manifest file|*.maFile"
            };

            AccountFileDialog = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = "ssa",
                Filter = "Steam Switcher Account|*.ssa"
            };

            SetGameFolder = new FolderBrowserDialog();
            DialogFinder = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Выберите SSFN файлы."
            };
            EditDialogFinder = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Выберите SSFN файлы."
            };

            MaFileDialog = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = "maFile",
                Filter = "Manifest file|*.maFile"
            };

            EditMaFileDialog = new OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = "maFile",
                Filter = "Manifest file|*.maFile"
            };

            AccountListChecker = new Timer
            {
                Enabled = true,
                Interval = 2000
            };
            AccountListChecker.Tick += new EventHandler(AccountListChecker_Tick);

            EndAnimationTimer = new Timer
            {
                Interval = 1
            };
            EndAnimationTimer.Tick += new EventHandler(EndAnimationLinesTimer_Tick);

            IdleTimer = new Timer
            {
                Interval = 1500
            };
            IdleTimer.Tick += new EventHandler(IdleTimer_Tick);

            AccountsCollection = new ObservableCollection<AccountsModel>();
            AccountList.ItemsSource = AccountsCollection;
        }

        private void AccountListChecker_Tick(object sender, EventArgs e)
        {
            if (!File.Exists(ConfigFolder + @"AdvancedInformation.lst"))
            {
                AccountListChecker.Stop();
                if (System.Windows.Forms.MessageBox.Show("К сожалению, файл 'AdvancedInformation.lst', включающий в себя список сохраненных аккаунтов по неизвестным причинам не был найден, пожалуйста, пересоздайте его вручную, либо перезапустите программу." + Environment.NewLine + Environment.NewLine + "Если вы создали файл вручную, нажмите OK. Для закрытия программы нажмите Отмена.", "Steam Switcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.OK)
                {
                    ErrorLog.Write("К сожалению, файл 'AdvancedInformation.lst', включающий в себя список сохраненных аккаунтов по неизвестным причинам не был найден. (#1: Запущена асинхронная проверка файла)");
                    AccountListChecker.Start();
                    if (File.Exists(ConfigFolder + @"AdvancedInformation.lst"))
                    {
                        UpdateAccountsCollection();
                    }
                }
                else
                {
                    ErrorLog.Write("К сожалению, файл 'AdvancedInformation.lst', включающий в себя список сохраненных аккаунтов по неизвестным причинам не был найден. (#2: Закрытие программы)");
                    Environment.Exit(0);
                }
            }
        }

        private void SelectPath(bool ReCreate)
        {
            if (ReCreate && File.Exists(ConfigFolder + "Settings.cfg"))
            {
                try
                {
                    File.Delete(ConfigFolder + "Settings.cfg");
                }
                catch (Exception EX0)
                {
                    if (System.Windows.Forms.MessageBox.Show("Ошибка: " + EX0.Message + Environment.NewLine + Environment.NewLine + "Чтобы повторить операцию, нажмите 'Повтор'. Для того, чтобы завершить работу с программой нажмите 'Отмена'.", "Steam Switcher", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                    {
                        ErrorLog.Write("(№1) Неизвестная ошибка. (#1: Выбор директории)", EX0.StackTrace);
                        SelectPath(true);
                    }
                    else
                    {
                        ErrorLog.Write("(№1) Неизвестная ошибка. (#2: Закрытие программы)", EX0.StackTrace);
                        Environment.Exit(0);
                    }
                }
            }

            if (!File.Exists(ConfigFolder + "Settings.cfg"))
            {
                if (SetGameFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (File.Exists(SetGameFolder.SelectedPath + @"\Steam.exe"))
                    {
                        try
                        {
                            SettingsInformation.Write("SteamPath", SetGameFolder.SelectedPath, "SwitcherSettings");
                            location = SetGameFolder.SelectedPath;
                        }
                        catch (Exception EX1)
                        {
                            if (System.Windows.Forms.MessageBox.Show("Ошибка: " + EX1.Message + Environment.NewLine + Environment.NewLine + "Чтобы повторить операцию, нажмите 'Повтор'. Для того, чтобы завершить работу с программой нажмите 'Отмена'.", "Steam Switcher", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                            {
                                ErrorLog.Write("(№2) Неизвестная ошибка. (#1: Выбор директории)", EX1.StackTrace);
                                SelectPath(true);
                            }
                            else
                            {
                                ErrorLog.Write("(№2) Неизвестная ошибка. (#2: Закрытие программы)", EX1.StackTrace);
                                Environment.Exit(0);
                            }
                        }
                    }
                    else
                    {
                        if (System.Windows.Forms.MessageBox.Show("Внимание! Некорректная папка с клиентом Steam, работа со свитчером не может быть продолжена, повторить попытку поиска папки Steam?", "Steam Switcher", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Retry)
                        {
                            SelectPath(false);
                            ErrorLog.Write("Выбрана некорректная папка с клиентом Steam. (#1: Выбор директории)");
                        }
                        else
                        {
                            ErrorLog.Write("Выбрана некорректная папка с клиентом Steam. (#2: Закрытие программ)");
                            File.Delete(ConfigFolder + "Settings.cfg");
                            Environment.Exit(0);
                        }
                    }
                }
                else if (System.Windows.Forms.MessageBox.Show("Для работы свитчера вы должны указать папку со Steam. Нажмите 'Повтор' чтобы повторить выбор, нажмите 'Отмена' чтобы прекратить работу с программой.", "Steam Switcher", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Retry)
                {
                    SelectPath(false);
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                if (!Directory.Exists(SettingsInformation.Read("SteamPath", "SwitcherSettings")) || !SettingsInformation.KeyExists("SteamPath", "SwitcherSettings"))
                {
                    if (System.Windows.Forms.MessageBox.Show("Ранее указанная папка с клиентом Steam более не существует, пожалуйста, укажите новый путь к Steam.", "Steam Switcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
                    {
                        ErrorLog.Write("Ранее указанная папка с клиентом Steam более не существует. (#1: Выбор папки)");
                        SelectPath(true);
                    }
                    else
                    {
                        ErrorLog.Write("Ранее указанная папка с клиентом Steam более не существует. (#2: Закрытие программы)");
                        Environment.Exit(0);
                    }
                }
                else
                {
                    location = SettingsInformation.Read("SteamPath", "SwitcherSettings");
                }
            }
        }

        private void DeleteOldSSFN()
        {
            Debug.WriteLine("[DeleteOldSSFN] Started for account: " + SelectedAccount.Login);
            if (SelectedAccount.Login != null)
            {
                if (Directory.Exists(SSFNFolder + @SelectedAccount.Login))
                {
                    DirectoryInfo LocalSSFNDirectory = new DirectoryInfo(SSFNFolder + @SelectedAccount.Login);
                    FileInfo[] SSFNFiles = LocalSSFNDirectory.GetFiles("ssfn*.*");
                    if (SSFNFiles.Length != 0)
                    {
                        string[] strArray = new string[] { "ssfn" };
                        FileInfo[] files = new DirectoryInfo(location).GetFiles("*.*", SearchOption.TopDirectoryOnly);
                        foreach (FileInfo info in files)
                        {
                            for (int i = 0; i < strArray.Length; i++)
                            {
                                if (Regex.IsMatch(info.Name, strArray[i]))
                                {
                                    File.Delete(info.FullName);
                                }
                            }
                        }

                        Debug.WriteLine("[DeleteOldSSFN] SSFN files found in account folder, processing to [ReplaceSSFN]...");
                        ReplaceSSFN();
                    }
                    else
                    {
                        Debug.WriteLine("[DeleteOldSSFN] No SSFN files found in account folder, returning...");
                    }
                }
            }
        }

        private void ReplaceSSFN()
        {
            if (Directory.Exists(SSFNFolder + @SelectedAccount.Login))
            {
                DirectoryInfo info = new DirectoryInfo(SSFNFolder + @SelectedAccount.Login);
                DirectoryInfo info2 = new DirectoryInfo(location + @"\");
                foreach (FileInfo info3 in info.GetFiles())
                {
                    info3.CopyTo(info2 + info3.Name, true);
                }
            }

            Debug.WriteLine("[ReplaceSSFN]: Replacing SSFN files for " + @SelectedAccount.Login + " done.");
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            AdvancedExtensions.Wait(300);

            if (AccountList.SelectedItem != null)
            {
                if (IsSteamRunning)
                    KillSteamProcess();
                DeleteOldSSFN();
                StartAutoLogin(true);
            }
            else
                StartAutoLogin(false);
            LoginButton.IsEnabled = true;
        }

        private bool IsSteamRunning
        {
            get
            {
                Process[] pname = Process.GetProcessesByName("Steam");
                if (pname.Length == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private void KillSteamProcess()
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c taskkill /f /im Steam.exe & /c taskkill /f /im SteamService.exe & /c taskkill /f /im steamwebhelper.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var process = Process.Start(processInfo);
            process.WaitForExit();
            process.Close();
        }

        private void StartAutoLogin(bool WithAccount = true)
        {
            if (TwoFactorCodeBox.Content.ToString().Length == 5)
            {
                string code = TwoFactorCodeBox.Content.ToString();
                loginManager.DoLogin(location, SelectedAccount, code);
            }
            else
                loginManager.DoLogin(location, SelectedAccount, null);

        }

        private void AccountList_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AccountList.Items.IsEmpty)
            {
                CodeRefresher.Start();
                UpdateInformation();
                GenerateSteamGuardCode();
            }
        }

        private bool IsMaFileExists
        {
            get
            {
                if (!File.Exists(SSAFolder + @"maFiles/" + SelectedAccount.SteamID + ".maFile"))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private void SetDirectoryImage_Click(object sender, RoutedEventArgs e)
        {
            Settings Sett = new Settings(location);
            Sett.ShowDialog();

            if (Sett.NeedToChangeSteamDir)
            {
                SelectPath(true);
            }
        }

        private async void MainForm_Loaded(object sender, RoutedEventArgs e)
        {
            CurTab = CurrentTab.Information;

            if (!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);
            if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);

            FileInfo info = new FileInfo(ConfigFolder + @"AdvancedInformation.lst");
            if (!info.Exists)
            {
                FileStream stream = info.Create();
                stream.Close();
            }
            else
            {
                if (info.Length != 0)
                {
                    UpdateAccountsCollection();
                }
            }

            AccountListChecker.Start();
            SelectPath(false);
            AccountList.SelectedIndex = 0;

            if (!AccountList.HasItems)
            {
                CopyProfileBtn.IsEnabled = false;
                OpenProfileBtn.IsEnabled = false;
                CopyTradeBtnPartner.IsEnabled = false;
                CopyTradeBtnToken.IsEnabled = false;
                CopyMailBtn.IsEnabled = false;
                CopyMailPasswordBtn.IsEnabled = false;
                SharedSecretBtn.IsEnabled = false;
                IdentitySecretBtn.IsEnabled = false;
                SteamIDBtn.IsEnabled = false;
                CodeRefresher.Enabled = false;
            }
            else
            {
                //AccountInformationTab.IsEnabled = false;
                FadeAnimation(CacheRefreshingGrid, false, 0.2);
                List<AccountsModel> accountsList = new List<AccountsModel>();
                foreach (var acc in AccountsCollection)
                    accountsList.Add(acc);
                await CacheWorker.ReloadFullCache(accountsList.ToArray());
                UpdateInformation();
                FadeAnimation(CacheRefreshingGrid, true, 0.2);
                //AccountInformationTab.IsEnabled = true;
            }
        }

        private async void UpdateAccountsCollection()
        {
            string advinformationfilepath = ConfigFolder + @"AdvancedInformation.lst";
            FileInfo fi = new FileInfo(advinformationfilepath);
            if (fi.Length != 0)
            {
                foreach (var item in helper.ReadSections(ConfigFolder + @"AdvancedInformation.lst"))
                {
                    string steamid = helper.ReadValue(item, "SteamID", advinformationfilepath);
                    string AvatarFolder = $@"{RootDirectory}cache\{steamid}.jpg";

                    if (!File.Exists(AvatarFolder))
                    {
                        FadeAnimation(CacheRefreshingGrid, false, 0.2);
                        await CacheWorker.DownloadAvatarDirectly(steamid);
                        FadeAnimation(CacheRefreshingGrid, true, 0.2);
                    }

                    AccountsCollection.Add(new AccountsModel
                    {
                        Login = item,
                        Password = helper.ReadValue(item, "Password", advinformationfilepath),
                        SteamID = steamid,
                        Shared_Secret = helper.ReadValue(item, "SharedSecret", advinformationfilepath),
                        ProfileLink = $"https://steamcommunity.com/profiles/{steamid}",
                        TradeLink = helper.ReadValue(item, "TradeLink", advinformationfilepath),
                        Mail = helper.ReadValue(item, "Mail", advinformationfilepath),
                        MailPassword = helper.ReadValue(item, "MailPassword", advinformationfilepath),
                        Note = helper.ReadValue(item, "Note", advinformationfilepath),
                        AvatarFilePath = AvatarFolder
                    });
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CloseButton.Foreground = new SolidColorBrush(Colors.Red);
            CloseButton.Opacity = 1;
        }

        private void CloseButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CloseButton.Foreground = new SolidColorBrush(Color.FromRgb(199, 0, 0));
            CloseButton.Opacity = 0.85;
        }

        private void MinimizeButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MinimizeButton.Foreground = new SolidColorBrush(Color.FromRgb(0, 185, 255));
            MinimizeButton.Opacity = 1;
        }

        private void MinimizeButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MinimizeButton.Foreground = new SolidColorBrush(Color.FromRgb(0, 151, 255));
            MinimizeButton.Opacity = 0.85;
        }

        private void FindSSFNButtonClick(object sender, RoutedEventArgs e)
        {
            if (DialogFinder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SSFNPathTextBox.Text = string.Empty;
                IsSSFNChoosed = true;
                foreach (string str in DialogFinder.SafeFileNames)
                {
                    if (str.Contains("ssfn"))
                    {
                        SSFNPathTextBox.Text = SSFNPathTextBox.Text + str + Environment.NewLine;
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Вы выбрали некорректные SSFN файлы. Пожалуйста, повторите попытку.", "Steam Switcher", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        IsSSFNChoosed = false;
                        break;
                    }
                }
            }
        }

        private void EditFindSSFNButtonClick(object sender, RoutedEventArgs e)
        {
            if (EditSSFNPathTextBox.Text.Length != 0)
            {
                DialogResult dr = System.Windows.Forms.MessageBox.Show("Данная операция заменит ваши текущие SSFN файлы, вы уверены?", "Steam Switcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dr == System.Windows.Forms.DialogResult.OK)
                {
                    if (EditDialogFinder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        IsEditSSFNChoosed = true;
                        EditSSFNPathTextBox.Text = string.Empty;
                        foreach (string strings in EditDialogFinder.SafeFileNames)
                        {
                            if (strings.Contains("ssfn"))
                            {
                                EditSSFNPathTextBox.Text = EditSSFNPathTextBox.Text + strings + Environment.NewLine;
                            }
                            else
                            {
                                System.Windows.Forms.MessageBox.Show("Вы выбрали некорректные SSFN файлы. Пожалуйста, повторите попытку.", "Steam Switcher", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                                IsEditSSFNChoosed = false;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (EditDialogFinder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    IsEditSSFNChoosed = true;
                    EditSSFNPathTextBox.Text = string.Empty;
                    foreach (string strings in EditDialogFinder.SafeFileNames)
                    {
                        if (strings.Contains("ssfn"))
                        {
                            EditSSFNPathTextBox.Text = EditSSFNPathTextBox.Text + strings + Environment.NewLine;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Вы выбрали некорректные SSFN файлы. Пожалуйста, повторите попытку.", "Steam Switcher", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                            IsEditSSFNChoosed = false;
                            break;
                        }
                    }
                }
            }
        }

        private void FadeAnimation(Grid UsedPanel, bool Visible, double Duration)
        {
            double FromParameter = 0.0;
            double ToParameter = 0.0;

            if (Visible == true)
            {
                FromParameter = 1.0;
                ToParameter = 0.0;
            }
            else
            {
                UsedPanel.Visibility = Visibility.Visible;
                FromParameter = 0.0;
                ToParameter = 1.0;
            }

            DoubleAnimation a = new DoubleAnimation
            {
                From = FromParameter,
                To = ToParameter,
                FillBehavior = FillBehavior.Stop,
                BeginTime = TimeSpan.FromSeconds(0),
                Duration = new Duration(TimeSpan.FromSeconds(Duration)) // 0.2
            };
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(a);
            Storyboard.SetTarget(a, UsedPanel);
            Storyboard.SetTargetProperty(a, new PropertyPath(OpacityProperty));
            if (Visible == true)
            {
                storyboard.Completed += delegate { UsedPanel.Visibility = System.Windows.Visibility.Hidden; };
            }
            else
            {
                storyboard.Completed += delegate { UsedPanel.Visibility = System.Windows.Visibility.Visible; };
            }
            storyboard.Begin();
        }

        private bool IsAccountExists
        {
            get
            {
                foreach (var item in AccountList.Items)
                {
                    if (item.ToString().Contains(LoginBox.Text))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
        }

        private async void ContinueBtnClick(object sender, RoutedEventArgs e)
        {
            if (!IsAccountExists)
            {
                if ((LoginBox.Text.Length != 0) && (PwdBox.Text.Length != 0) && (LoginBox.Text != "Логин*") && (PwdBox.Text != "Пароль*") && (SteamIDBox.Text.Length == 17) && (SteamIDBox.Text != "SteamID*"))
                {
                    string Login = LoginBox.Text;
                    string Password = PwdBox.Text;
                    string Email = MailBox.Text;
                    string EmailPwd = MailPwdBox.Text;
                    string Steamid = SteamIDBox.Text;
                    string Tradelink = TradeLinkBox.Text;
                    string Profilelink = $"https://steamcommunity.com/profiles/{Steamid}";
                    string SharedSecret = SharedSecretBox.Text;
                    string Note = NoteBox.Text;

                    AdvInformation.Write("Password", Password, Login);
                    AdvInformation.Write("SteamID", Steamid, Login);

                    if (MailBox.Text != "Почтовый адрес аккаунта")
                    {
                        AdvInformation.Write("Mail", Email, Login);
                    }
                    else
                    {
                        AdvInformation.Write("Mail", "", Login);
                        Email = "";
                    }

                    if (MailPwdBox.Text != "Пароль от почтового ящика")
                    {
                        AdvInformation.Write("MailPassword", EmailPwd, Login);
                    }
                    else
                    {
                        AdvInformation.Write("MailPassword", "", Login);
                        EmailPwd = "";
                    }

                    if (TradeLinkBox.Text != "Ссылка на обмен")
                    {
                        AdvInformation.Write("TradeLink", Tradelink, Login);
                    }
                    else
                    {
                        AdvInformation.Write("TradeLink", "", Login);
                        Tradelink = "";
                    }

                    if (SharedSecretBox.Text != "Секретный ключ")
                    {
                        AdvInformation.Write("SharedSecret", SharedSecret, Login);
                    }
                    else
                    {
                        AdvInformation.Write("SharedSecret", "", Login);
                        SharedSecret = "";
                    }

                    if (NoteBox.Text != "Примечание")
                    {
                        AdvInformation.Write("Note", Note, Login);
                    }
                    else
                    {
                        AdvInformation.Write("Note", "", Login);
                        Note = "";
                    }

                    Directory.CreateDirectory(SSFNFolder + Login);
                    if (IsSSFNChoosed)
                    {
                        foreach (string str in DialogFinder.FileNames)
                        {
                            if (RadioMove.IsChecked == true)
                            {
                                File.Move(str, $@"{Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)}\ssfn\{Login}\{Path.GetFileNameWithoutExtension(str)}");
                            }
                            else if (RadioCopy.IsChecked == true)
                            {
                                File.Copy(str, $@"{Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)}\ssfn\{Login}\{Path.GetFileNameWithoutExtension(str)}");
                            }
                        }
                        if (EnableNotifications == 1 || EnableNotifications == 2)
                        {
                            new ShowNotifyMessage($"Аккаунт '{Login}' успешно занесен в список и SSFN файлы были перемещены (скопированы) в нужную папку для полноценной работы свитчера с данным аккаунтом.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Information, "CopySound", 3000).Show();
                        }
                    }
                    else
                    {
                        if (EnableNotifications == 1 || EnableNotifications == 2)
                        {
                            new ShowNotifyMessage($"Аккаунт '{LoginBox.Text}' успешно занесен в список и библиотека для SSFN файлов успешно создана, однако, Вы не указали путь к SSFN файлам. Для полноценного пользования свитчером требуется, чтобы SSFN файлы были в библиотеке, которая называется идентично с логином вашего аккаунта в корневой папке программы.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Information, "CopySound", 4000).Show();
                        }
                    }

                    if (IsMaFileSelected)
                    {
                        string destma = $@"{SSAFolder}maFiles\{SteamIDBox.Text}.maFile";
                        File.Copy(MaFileDialog.FileName, destma, true);
                        MakeSSAEntry(SteamIDBox.Text);
                    }

                    await CacheWorker.DownloadAvatarDirectly(Steamid);
                    AccountsModel Item = new AccountsModel()
                    {
                        Login = Login,
                        Password = Password,
                        Mail = Email,
                        MailPassword = EmailPwd,
                        SteamID = Steamid,
                        TradeLink = Tradelink,
                        ProfileLink = Profilelink,
                        Shared_Secret = SharedSecret,
                        Note = Note,
                        AvatarFilePath = $"{CacheFolder}{Steamid}.jpg"
                    };

                    AccountsCollection.Add(Item);
                    AccountsModel[] acc = new AccountsModel[] { Item };
                    await CacheWorker.ReloadFullCache(acc);
                    UpdateInformation();

                    InformationTab.IsSelected = true;
                    CurTab = CurrentTab.Information;
                    FillAddForm();

                    if (!AccountList.Items.IsEmpty)
                    {
                        AccountList.SelectedItem = AccountList.Items[AccountList.Items.Count - 1].ToString();
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Обязательные поля 'Логин', 'Пароль', 'SteamID' не заполнены либо превышают количество символов.", "Steam Switcher", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
            else
            {
                new ShowNotifyMessage("Аккаунт '" + LoginBox.Text + "' уже есть в списке.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Error).Show();
            }
        }

        private void FillAddForm()
        {
            LoginBox.Text = "Логин*";
            PwdBox.Text = "Пароль*";
            MailBox.Text = "Почтовый адрес аккаунта";
            MailPwdBox.Text = "Пароль от почтового ящика";
            TradeLinkBox.Text = "Ссылка на обмен";
            SharedSecretBox.Text = "Секретный ключ";
            SteamIDBox.Text = "SteamID*";
            DialogFinder.FileName = null;
            SSFNPathTextBox.Text = string.Empty;
            RadioCopy.IsChecked = true;
            IsMaFileSelected = false;
            IsSSFNChoosed = false;
            MaFileDialog.FileName = null;
            MaFileTextBox.Text = ".maFile не выбран";
        }

        private void SteamIDBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SteamIDBox.Text.Equals(""))
            {
                SteamIDBox.Text = "SteamID*";
            }
        }

        private void SteamIDBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SteamIDBox.Text.Equals("SteamID*"))
            {
                SteamIDBox.Text = "";
            }
        }

        private void EditSteamIDBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditSteamIDBox.Text.Equals(""))
            {
                EditSteamIDBox.Text = "SteamID*";
            }
        }

        private void EditSteamIDBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EditSteamIDBox.Text.Equals("SteamID*"))
            {
                EditSteamIDBox.Text = "";
            }
        }

        private void AddSSAFileBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AddSSAFileBox.Text.Equals("PASTE THE TEXT HERE"))
            {
                AddSSAFileBox.Text = "";
                AddSSAFileBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                AddSSAFileBox.VerticalContentAlignment = VerticalAlignment.Top;
                AddSSAFileBox.FontSize = 10;
            }
        }

        private void AddSSAFileBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (AddSSAFileBox.Text.Equals(""))
            {
                AddSSAFileBox.Text = "PASTE THE TEXT HERE";
                AddSSAFileBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                AddSSAFileBox.VerticalContentAlignment = VerticalAlignment.Center;
                AddSSAFileBox.FontSize = 14;
            }
        }

        private void LoginBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (LoginBox.Text.Equals("Логин*"))
            {
                LoginBox.Text = "";
            }
        }

        private void PwdBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PwdBox.Text.Equals("Пароль*"))
            {
                PwdBox.Text = "";
            }
        }

        private void MailBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (MailBox.Text.Equals("Почтовый адрес аккаунта"))
            {
                MailBox.Text = "";
            }
        }

        private void MailPwdBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (MailPwdBox.Text.Equals("Пароль от почтового ящика"))
            {
                MailPwdBox.Text = "";
            }
        }

        private void TradeLinkBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TradeLinkBox.Text.Equals("Ссылка на обмен"))
            {
                TradeLinkBox.Text = "";
            }
        }

        private void LoginBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (LoginBox.Text.Equals(""))
            {
                LoginBox.Text = "Логин*";
            }
        }

        private void PwdBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PwdBox.Text.Equals(""))
            {
                PwdBox.Text = "Пароль*";
            }
        }

        private void MailBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (MailBox.Text.Equals(""))
            {
                MailBox.Text = "Почтовый адрес аккаунта";
            }
        }

        private void MailPwdBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (MailPwdBox.Text.Equals(""))
            {
                MailPwdBox.Text = "Пароль от почтового ящика";
            }
        }

        private void TradeLinkBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TradeLinkBox.Text.Equals(""))
            {
                TradeLinkBox.Text = "Ссылка на обмен";
            }
        }
        private void SharedSecretBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SharedSecretBox.Text.Equals("Секретный ключ"))
            {
                SharedSecretBox.Text = "";
            }
        }

        private void SharedSecretBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SharedSecretBox.Text.Equals(""))
            {
                SharedSecretBox.Text = "Секретный ключ";
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject(TwoFactorCodeBox.Content.ToString(), false);
            if (EnableNotifications == 2)
            {
                new ShowNotifyMessage("Код для авторизации в аккаунт '" + SelectedAccount.Login + "' скопирован: " + TwoFactorCodeBox.Content, "Steam Switcher").Show();
            }
        }

        private void EditPwdBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EditPwdBox.Text.Equals("Пароль"))
            {
                EditPwdBox.Text = "";
            }
        }

        private void EditMailBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EditMailBox.Text.Equals("Почтовый адрес аккаунта"))
            {
                EditMailBox.Text = "";
            }
        }

        private void EditMailPwdBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EditMailPwdBox.Text.Equals("Пароль от почтового ящика"))
            {
                EditMailPwdBox.Text = "";
            }
        }

        private void EditTradeLinkBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EditTradeLinkBox.Text.Equals("Ссылка на обмен"))
            {
                EditTradeLinkBox.Text = "";
            }
        }

        private void EditPwdBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditPwdBox.Text.Equals(""))
            {
                EditPwdBox.Text = "Пароль";
            }
        }

        private void EditMailBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditMailBox.Text.Equals(""))
            {
                EditMailBox.Text = "Почтовый адрес аккаунта";
            }
        }

        private void EditMailPwdBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditMailPwdBox.Text.Equals(""))
            {
                EditMailPwdBox.Text = "Пароль от почтового ящика";
            }
        }

        private void EditTradeLinkBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditTradeLinkBox.Text.Equals(""))
            {
                EditTradeLinkBox.Text = "Ссылка на обмен";
            }
        }
        private void EditSharedSecretBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EditSharedSecretBox.Text.Equals("Секретный ключ"))
            {
                EditSharedSecretBox.Text = "";
            }
        }

        private void EditSharedSecretBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditSharedSecretBox.Text.Equals(""))
            {
                EditSharedSecretBox.Text = "Секретный ключ";
            }
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private void GenerateSteamGuardCode()
        {
            if (LoginAccount.SharedSecret.Length > 0 && SelectedAccount != null && AccountList.HasItems)
            {
                TwoFactorCodeBox.Content = LoginAccount.GenerateSteamGuardCodeForTime(SteamTimeCallback);
                TwoFactorCodeBox.IsEnabled = true;
            }
            else
            {
                TwoFactorCodeBox.Content = "NO SECRET KEY";
                TwoFactorCodeBox.IsEnabled = false;
            }
        }

        private async void CodeRefresher_Tick(object sender, EventArgs e)
        {
            try
            {
                SteamTimeCallback = await TimeAligner.GetSteamTimeAsync();
                CurrentSteamChunk = SteamTimeCallback / 30L;
                int secondsUntilChange = (int)(SteamTimeCallback - (CurrentSteamChunk * 30L));
                MobileCodeProgress.Value = 30 - secondsUntilChange;
                if (secondsUntilChange == 0)
                    GenerateSteamGuardCode();
            }
            catch (Exception ex)
            {
                TwoFactorCodeBox.Content = "ERROR";
                ErrorLog.Write("Ошибка чтения кода авторизации для аккаунта.", ex.StackTrace);
                TwoFactorCodeBox.IsEnabled = false;
                CodeRefresher.Stop();
            }
        }

        private void EditContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            var SelectedAccount = AccountList.SelectedItem;

            if (EditPwdBox.Text.Length != 0 && EditSteamIDBox.Text.Length == 17 && EditSteamIDBox.Text != "SteamID*")
            {
                if (PasswordChanged)
                {
                    AdvInformation.Write("Password", EditPwdBox.Text, EditLoginBox.Text);
                }

                if (EditMailBox.Text != "Почтовый адрес аккаунта")
                {
                    AdvInformation.Write("Mail", EditMailBox.Text, EditLoginBox.Text);
                }
                else
                {
                    AdvInformation.Write("Mail", "", EditLoginBox.Text);
                }

                if (EditMailPwdBox.Text != "Пароль от почтового ящика")
                {
                    AdvInformation.Write("MailPassword", EditMailPwdBox.Text, EditLoginBox.Text);
                }
                else
                {
                    AdvInformation.Write("MailPassword", "", EditLoginBox.Text);
                }

                if (EditTradeLinkBox.Text != "Ссылка на обмен")
                {
                    AdvInformation.Write("TradeLink", EditTradeLinkBox.Text, EditLoginBox.Text);
                }
                else
                {
                    AdvInformation.Write("TradeLink", "", EditLoginBox.Text);
                }

                if (EditSharedSecretBox.Text != "Секретный ключ")
                {
                    AdvInformation.Write("SharedSecret", EditSharedSecretBox.Text, EditLoginBox.Text);
                }
                else
                {
                    AdvInformation.Write("SharedSecret", "", EditLoginBox.Text);
                }

                if (EditSteamIDBox.Text != "SteamID*")
                {
                    AdvInformation.Write("SteamID", EditSteamIDBox.Text, EditLoginBox.Text);
                }
                else
                {
                    MessageBox.Show("Необходимо ввести SteamID для продолжения.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (EditNoteBox.Text != "Примечание")
                {
                    AdvInformation.Write("Note", EditNoteBox.Text, EditLoginBox.Text);
                }
                else
                {
                    AdvInformation.Write("Note", "", EditLoginBox.Text);
                }

                if (IsEditSSFNChoosed == true)
                {
                    if (!Directory.Exists(SSFNFolder + @EditLoginBox.Text))
                    {
                        Directory.CreateDirectory(SSFNFolder + @EditLoginBox.Text);
                        foreach (string strings in EditDialogFinder.FileNames)
                        {
                            File.Copy(strings, SSFNFolder + @EditLoginBox.Text + @"\" + System.IO.Path.GetFileNameWithoutExtension(strings), true);
                        }
                    }
                    else
                    {
                        if (EditSSFNPathTextBox.Text.Length != 0)
                        {
                            DirectoryInfo di = new DirectoryInfo(SSFNFolder + @EditLoginBox.Text);
                            foreach (FileInfo file in di.GetFiles())
                            {
                                file.Delete();
                            }
                            foreach (string strings in EditDialogFinder.FileNames)
                            {
                                File.Copy(strings, SSFNFolder + @EditLoginBox.Text + @"\" + System.IO.Path.GetFileNameWithoutExtension(strings));
                            }
                        }
                    }
                }

                if (AdvInformation.Read("TradeLink", EditLoginBox.Text) != string.Empty) { CopyTradeBtnPartner.IsEnabled = true; CopyTradeBtnToken.IsEnabled = true; } else { CopyTradeBtnPartner.IsEnabled = false; CopyTradeBtnToken.IsEnabled = false; }
                if (AdvInformation.Read("Mail", EditLoginBox.Text) != string.Empty) { CopyMailBtn.IsEnabled = true; } else { CopyMailBtn.IsEnabled = false; }
                if (AdvInformation.Read("MailPassword", EditLoginBox.Text) != string.Empty) { CopyMailPasswordBtn.IsEnabled = true; } else { CopyMailPasswordBtn.IsEnabled = false; }
                if (EnableNotifications == 1 || EnableNotifications == 2)
                {
                    new ShowNotifyMessage("Информация об аккаунте " + "'" + EditLoginBox.Text + "'" + " успешно изменена.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Information, "CopySound", 2000).Show();
                }

                if (IsEditMaFileSelected)
                {
                    string destma = SSAFolder + @"maFiles\" + EditSteamIDBox.Text + ".maFile";
                    if (EditMaFileDialog.FileName != destma)
                    {
                        File.Copy(EditMaFileDialog.FileName, destma, true);
                        MakeSSAEntry(EditSteamIDBox.Text);
                    }
                }

                UpdateInformation();
                UpdateAccountsCollection();
                IsEditSSFNChoosed = false;
                IsEditMaFileSelected = false;
                PasswordChanged = false;
                CurTab = CurrentTab.Information;
                BackBtn.PerformClick();
                FadeAnimation(InfoPanelGrid, false, 0.2);
                AccountList.SelectedItem = SelectedAccount;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Обязательные поля 'Пароль' и 'SteamID' не заполнены либо превышают количество символов.", "Steam Switcher", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void MakeSSAEntry(string steamid)
        {
            Process MakeEntry = new Process();
            MakeEntry.StartInfo.Arguments = "-DirectImport:" + steamid;
            MakeEntry.StartInfo.FileName = SSAFolder + @"Steam Switcher Authenticator.exe";
            MakeEntry.Start();
        }

        private void EditPwdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PasswordChanged = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            EndAnimationTimer.Start();
            IdleTimer.Stop();
        }

        private void EndAnimationLinesTimer_Tick(object sender, EventArgs e)
        {
            if (BEffect.Radius != 20)
            {
                BEffect.Radius += 0.5;
            }
            else
            {
                EndAnimationTimer.Stop();
                Environment.Exit(0);
            }
        }

        private void PlayEndSound()
        {
            new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("Steam_Switcher.ExitSound.wav")).Play();
        }

        private void KillSSAProcesses()
        {
            string SSA = "Steam Switcher Authenticator";
            Process[] GetProcs = Process.GetProcesses();
            foreach (Process ActiveProc in GetProcs)
            {
                if (ActiveProc.ProcessName.Contains(SSA))
                {
                    ActiveProc.Kill();
                }
            }
        }

        private void StopActiveTimers()
        {
            if (AccountListChecker.Enabled) { AccountListChecker.Enabled = false; }
            if (CodeRefresher.Enabled) { CodeRefresher.Enabled = false; }
            if (EndAnimationTimer.Enabled) { EndAnimationTimer.Enabled = false; }
            if (IdleTimer.Enabled) { IdleTimer.Enabled = false; }
            Debug.WriteLine("Active timers stopped. [Function: StopActiveTimers()]");
        }

        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            StopActiveTimers();
            KillSSAProcesses();
            BlurAnimation();
        }

        private void BlurAnimation()
        {                                                                                      // List to hide on closing.
            CloseButton.Visibility = Visibility.Hidden;
            MinimizeButton.Visibility = Visibility.Hidden;
            CopyLoginBtn.Visibility = Visibility.Hidden;
            CopyPasswordBtn.Visibility = Visibility.Hidden;
            SetDirectoryImage.Visibility = Visibility.Hidden;
            BorderButtonsGrid.Visibility = Visibility.Hidden;
            MovableGrid.Visibility = Visibility.Hidden;
            SwitcherTab.Visibility = Visibility.Hidden;
            SteamIcon.Visibility = Visibility.Hidden;
            LogoLabel.Visibility = Visibility.Hidden;

            if (CurTab == CurrentTab.Information)
            {
                FadeAnimation(InfoPanelGrid, true, 0.2);
            }
            else if (CurTab == CurrentTab.Add)
            {
                FadeAnimation(AddPanelGrid, true, 0.2);
            }
            else if (CurTab == CurrentTab.Edit)
            {
                FadeAnimation(EditPanelGrid, true, 0.2);
            }
            else if (CurTab == CurrentTab.AccountInformation)
            {
                FadeAnimation(AccountInformationGrid, true, 0.2);
            }
            else if (CurTab == CurrentTab.Deactivate)
            {
                FadeAnimation(DeactivatePanel, true, 0.2);
            }
            else
            {
                System.Windows.MessageBox.Show("Выбрана недопустимая страница, работа с программой будет экстренно остановлена.", "Steam Switcher", MessageBoxButton.OK, MessageBoxImage.Error);
                ErrorLog.Write("Ошибка: Выбрана недопустимая страница. Экстренный выход.");
                Environment.Exit(0);
            }

            Thickness MessageThick = ExitMessage.Margin;
            MessageThick.Left = 285;
            MessageThick.Top = 375;
            ExitMessage.Margin = MessageThick;
            ExitMessage.Visibility = Visibility.Visible;
            IdleTimer.Start();
            PlayEndSound();
        }

        private string GetPersonaState(int state)
        {
            switch (state)
            {
                case 0:
                    SteamStatusRectangle.Stroke = Brushes.DimGray;
                    SteamStatusLabel.Foreground = Brushes.DimGray;
                    return "Не в сети";
                case 1:
                    SteamStatusRectangle.Stroke = Brushes.SkyBlue;
                    SteamStatusLabel.Foreground = Brushes.SkyBlue;
                    return "В сети";
                case 2:
                    SteamStatusRectangle.Stroke = Brushes.SkyBlue;
                    SteamStatusLabel.Foreground = Brushes.SkyBlue;
                    return "Занят";
                case 3:
                    SteamStatusRectangle.Stroke = Brushes.SkyBlue;
                    SteamStatusLabel.Foreground = Brushes.SkyBlue;
                    return "Нет на месте";
                case 4:
                    SteamStatusRectangle.Stroke = Brushes.SkyBlue;
                    SteamStatusLabel.Foreground = Brushes.SkyBlue;
                    return "Спит";
                case 5:
                    SteamStatusRectangle.Stroke = Brushes.SkyBlue;
                    SteamStatusLabel.Foreground = Brushes.SkyBlue;
                    return "Хочет обменяться";
                case 6:
                    SteamStatusRectangle.Stroke = Brushes.SkyBlue;
                    SteamStatusLabel.Foreground = Brushes.SkyBlue;
                    return "Хочет играть";
            }

            return null;
        }

        private string GetPersonaPublicity(int state)
        {
            switch (state)
            {
                case 1:
                    return "Скрытый";
                case 2:
                    return "Только для друзей";
                case 3:
                    return "Открытый";
            }

            return null;
        }

        private void LoadImage()
        {
            Image AccountDetailsImage = new Image();
            BitmapImage AccountDetailsBitmapImage = new BitmapImage();

            AvatarPanel.Children.Clear();
            AccountDetailsBitmapImage.BeginInit();
            AccountDetailsBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            AccountDetailsBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            AccountDetailsBitmapImage.UriSource = new Uri(SelectedAccount.AvatarFilePath, UriKind.Absolute);
            AccountDetailsBitmapImage.EndInit();
            AccountDetailsImage.Source = AccountDetailsBitmapImage;
            AvatarPanel.Children.Add(AccountDetailsImage);
        }

        private async void UpdateInformation()
        {
            if (AccountList.SelectedItem != null)
            {
                AccountInformationBox.Header = SelectedAccount.Login;
                LoginAccount.SharedSecret = SelectedAccount.Shared_Secret;

                if (SelectedAccount.EconomyBan == "banned" || SelectedAccount.CommunityBanned || SelectedAccount.VACBanned)
                {
                    if (SelectedAccount.EconomyBan == "banned")
                        SteamTradeBanLabel.Content = "Заблокирован в системе обмена";
                    else if (SelectedAccount.CommunityBanned)
                        SteamTradeBanLabel.Content = "Заблокирован в сообществе";
                    else
                        SteamTradeBanLabel.Content = "Игровая блокировка VAC";

                    SteamTradeBanLabel.Foreground = Brushes.Red;
                    SteamTradeBanLabel.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0)) { Opacity = 0.08 };
                    SteamTradeBanLabel.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0)) { Opacity = 0.3 };
                }
                else
                {
                    SteamTradeBanLabel.Content = "Нет блокировок";
                    SteamTradeBanLabel.Foreground = Brushes.Green;
                    SteamTradeBanLabel.Background = new SolidColorBrush(Color.FromRgb(0, 128, 0)) { Opacity = 0.08 };
                    SteamTradeBanLabel.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 128, 0)) { Opacity = 0.3 };
                }

                SteamNameLabel.Content = SelectedAccount.Nickname;
                SteamStatusLabel.Content = GetPersonaState(SelectedAccount.PersonaState);
                SteamPublicityLabel.Content = GetPersonaPublicity(SelectedAccount.PrivacyStatus);
                if (SelectedAccount.PrivacyStatus == 3)
                {
                    SteamPublicityLabel.Foreground = Brushes.Green;
                    SteamLevelLabel.Content = $"Уровень: {SelectedAccount.Level}";
                    GamesCountLabel.Content = $"Игры: {SelectedAccount.GamesCount}";
                }
                else if (SelectedAccount.PrivacyStatus == 2)
                {
                    SteamPublicityLabel.Foreground = Brushes.DarkOrange;
                    SteamLevelLabel.Content = $"Уровень: скрыто";
                    GamesCountLabel.Content = $"Игры: скрыто";
                }
                else
                {
                    SteamPublicityLabel.Foreground = Brushes.DarkRed;
                    SteamLevelLabel.Content = $"Уровень: скрыто";
                    GamesCountLabel.Content = $"Игры: скрыто";
                }

                if (SelectedAccount.AvatarFilePath.Length > 0)
                {
                    if (!File.Exists(SelectedAccount.AvatarFilePath))
                    {
                        FadeAnimation(CacheRefreshingGrid, false, 0.2);
                        List<AccountsModel> list = new List<AccountsModel> { SelectedAccount };
                        await CacheWorker.ReloadFullCache(list.ToArray());
                        FadeAnimation(CacheRefreshingGrid, true, 0.2);
                    }
                }
                else
                {
                    FadeAnimation(CacheRefreshingGrid, false, 0.2);
                    List<AccountsModel> list = new List<AccountsModel> { SelectedAccount };
                    await CacheWorker.ReloadFullCache(list.ToArray(), true);
                    FadeAnimation(CacheRefreshingGrid, true, 0.2);
                }

                LoadImage();

                if (SelectedAccount.SteamID.Length != 0)
                {
                    SteamIDBtn.Content = SelectedAccount.SteamID;
                    SteamIDBtn.IsEnabled = true;
                }
                else
                {
                    SteamIDBtn.Content = "-";
                    SteamIDBtn.IsEnabled = false;
                }

                if (IsMaFileExists)
                {
                    string ReadJSONContent = File.ReadAllText($@"{SSAFolder}maFiles\{SelectedAccount.SteamID}.maFile");
                    var mafile = JsonConvert.DeserializeObject<maFileJS>(ReadJSONContent);
                    SharedSecretBtn.IsEnabled = true;
                    IdentitySecretBtn.IsEnabled = true;
                    SharedSecretBtn.Content = mafile.shared_secret;
                    IdentitySecretBtn.Content = mafile.identity_secret;
                    OpenConfirmations.IsEnabled = true;
                    RefreshSessionBtn.IsEnabled = true;
                }
                else
                {
                    SharedSecretBtn.IsEnabled = false;
                    IdentitySecretBtn.IsEnabled = false;
                    SharedSecretBtn.Content = "-";
                    IdentitySecretBtn.Content = "-";
                    OpenConfirmations.IsEnabled = false;
                    RefreshSessionBtn.IsEnabled = false;
                }

                if (SelectedAccount.Mail == string.Empty)
                {
                    CopyMailBtn.Content = "Отсутствует";
                    CopyMailBtn.IsEnabled = false;
                }
                else
                {
                    CopyMailBtn.Content = SelectedAccount.Mail;
                    CopyMailBtn.IsEnabled = true;
                }

                if (SelectedAccount.MailPassword == string.Empty)
                {
                    CopyMailPasswordBtn.Content = "Отсутствует";
                    CopyMailPasswordBtn.IsEnabled = false;
                }
                else
                {
                    CopyMailPasswordBtn.Content = SelectedAccount.MailPassword;
                    CopyMailPasswordBtn.IsEnabled = true;
                }

                if (SelectedAccount.ProfileLink == string.Empty)
                {
                    CopyProfileBtn.Content = "Отсутствует";
                    CopyProfileBtn.IsEnabled = false;
                    OpenProfileBtn.IsEnabled = false;
                }
                else
                {
                    if (SelectedAccount.SteamID.Length != 0)
                    {
                        CopyProfileBtn.Content = SelectedAccount.SteamID;
                    }
                    else
                    {
                        CopyProfileBtn.Content = "-";
                    }

                    CopyProfileBtn.IsEnabled = true;
                    OpenProfileBtn.IsEnabled = true;
                }

                if (SelectedAccount.TradeLink == string.Empty)
                {
                    CopyTradeBtnPartner.Content = "Partner: -";
                    CopyTradeBtnToken.Content = "Token: -";
                    CopyTradeBtnPartner.IsEnabled = false;
                    CopyTradeBtnToken.IsEnabled = false;
                }
                else
                {
                    CopyTradeBtnPartner.Content = "Partner: " + GetTradeLinkPartner;
                    CopyTradeBtnToken.Content = "Token: " + GetTradeLinkToken;
                    CopyTradeBtnPartner.IsEnabled = true;
                    CopyTradeBtnToken.IsEnabled = true;
                }
            }
            else
            {
                CopyProfileBtn.IsEnabled = false;
                OpenProfileBtn.IsEnabled = false;
                CopyTradeBtnPartner.IsEnabled = false;
                CopyTradeBtnToken.IsEnabled = false;
                CopyMailBtn.IsEnabled = false;
                CopyMailPasswordBtn.IsEnabled = false;
                OpenConfirmations.IsEnabled = false;
                RefreshSessionBtn.IsEnabled = false;
                SteamIDBtn.IsEnabled = false;
                SharedSecretBtn.IsEnabled = false;
                IdentitySecretBtn.IsEnabled = false;
            }
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            FillAddForm();
        }

        private void CopyMailBtn_Click(object sender, RoutedEventArgs e)
        {
            string MailInfo = AdvInformation.Read("Mail", SelectedAccount.Login);
            if (MailInfo.Length != 0)
            {
                System.Windows.Clipboard.SetDataObject(MailInfo, false);
                if (EnableNotifications == 2)
                {
                    new ShowNotifyMessage("Электронная почта скопирована: " + MailInfo, "Steam Switcher").Show();
                }

                CopyMailBtn.IsEnabled = false;
                SetCopyMargin(CopyMailBtn, CopyAccountGrid1);
                FadeAnimation(CopyAccountGrid1, false, 0.1);
                AdvancedExtensions.Wait(500);
                FadeAnimation(CopyAccountGrid1, true, 0.1);
                CopyMailBtn.IsEnabled = true;
            }
        }

        private void CopyMailPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            string MailPass = AdvInformation.Read("MailPassword", SelectedAccount.Login);
            if (MailPass.Length != 0)
            {
                System.Windows.Clipboard.SetDataObject(MailPass, false);
                if (EnableNotifications == 2)
                {
                    new ShowNotifyMessage("Пароль от электронной почты скопирован: " + MailPass, "Steam Switcher").Show();
                }
                CopyMailPasswordBtn.IsEnabled = false;
                SetCopyMargin(CopyMailBtn, CopyAccountGrid1);
                FadeAnimation(CopyAccountGrid1, false, 0.1);
                AdvancedExtensions.Wait(500);
                FadeAnimation(CopyAccountGrid1, true, 0.1);
                CopyMailPasswordBtn.IsEnabled = true;
            }
        }

        private void CopyProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Process.Start(SelectedAccount.ProfileLink);
                CopyProfileBtn.IsEnabled = false;
                AdvancedExtensions.Wait(300);
                CopyProfileBtn.IsEnabled = true;
            }
            else
            {
                if (SelectedAccount.ProfileLink != string.Empty)
                {
                    System.Windows.Clipboard.SetDataObject(SelectedAccount.ProfileLink, false);
                    if (EnableNotifications == 2)
                    {
                        new ShowNotifyMessage($"Ссылка на профиль скопирована:\n\n{SelectedAccount.ProfileLink}", "Steam Switcher").Show();
                    }
                    CopyProfileBtn.IsEnabled = false;
                    SetCopyMargin(CopyProfileBtn, CopyAccountGrid1);
                    FadeAnimation(CopyAccountGrid1, false, 0.1);
                    AdvancedExtensions.Wait(500);
                    FadeAnimation(CopyAccountGrid1, true, 0.1);
                    CopyProfileBtn.IsEnabled = true;
                }
            }
        }

        private string GetTradeLinkPartner
        {
            get
            {
                string TradeLink = AdvInformation.Read("TradeLink", SelectedAccount.Login);
                string GetPartner = TradeLink.Between("partner=", "&");
                return GetPartner;
            }
        }

        private string GetTradeLinkToken
        {
            get
            {
                string TradeLink = AdvInformation.Read("TradeLink", SelectedAccount.Login);
                string GetToken = TradeLink.After("token=");
                return GetToken;
            }
        }

        private void CopyTradeBtn_Click(object sender, RoutedEventArgs e)
        {
            string TradeLink = AdvInformation.Read("TradeLink", SelectedAccount.Login);

            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Process.Start(TradeLink);
                CopyTradeBtnPartner.IsEnabled = false;
                CopyTradeBtnToken.IsEnabled = false;
                AdvancedExtensions.Wait(300);
                CopyTradeBtnPartner.IsEnabled = true;
                CopyTradeBtnToken.IsEnabled = true;
            }
            else
            {
                if (TradeLink.Length != 0)
                {
                    System.Windows.Clipboard.SetDataObject(TradeLink, false);
                    if (EnableNotifications == 2)
                    {
                        new ShowNotifyMessage("Ссылка на обмен скопирована:" + Environment.NewLine + Environment.NewLine + TradeLink, "Steam Switcher").Show();
                    }
                    CopyTradeBtnPartner.IsEnabled = false;
                    CopyTradeBtnToken.IsEnabled = false;
                    SetCopyMargin(CopyTradeBtnPartner, CopyAccountGrid1);
                    FadeAnimation(CopyAccountGrid1, false, 0.1);
                    AdvancedExtensions.Wait(500);
                    FadeAnimation(CopyAccountGrid1, true, 0.1);
                    CopyTradeBtnPartner.IsEnabled = true;
                    CopyTradeBtnToken.IsEnabled = true;
                }
            }
        }

        private void OpenConfirmations_Click(object sender, RoutedEventArgs e)
        {
            if (AccountList.SelectedItem != null)
            {
                OpenConfirmations.IsEnabled = false;
                AdvancedExtensions.Wait(500);
                ConfirmationProcess = new Process();
                ConfirmationProcess.StartInfo.Arguments = "-OpenOffers:" + SelectedAccount.Login;
                ConfirmationProcess.StartInfo.FileName = SSAFolder + @"Steam Switcher Authenticator.exe";
                ConfirmationProcess.Start();
                OpenConfirmations.IsEnabled = true;
            }
        }

        private void MovableGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CopyLoginBtn.IsMouseOver == false && CopyPasswordBtn.IsMouseOver == false)
            {
                DragMove();
            }
        }

        private enum CurrentTab
        {
            Information,
            AccountInformation,
            Add,
            Edit,
            Deactivate
        }


        private void DeactivateCurrentTab(CurrentTab curTab)
        {
            switch (curTab)
            {
                case CurrentTab.Information:
                    FadeAnimation(InfoPanelGrid, true, 0.2);
                    return;
                case CurrentTab.AccountInformation:
                    FadeAnimation(AccountInformationGrid, true, 0.2);
                    return;
                case CurrentTab.Add:
                    FadeAnimation(AddPanelGrid, true, 0.2);
                    return;
                case CurrentTab.Edit:
                    FadeAnimation(EditPanelGrid, true, 0.2);
                    return;
                case CurrentTab.Deactivate:
                    FadeAnimation(DeactivatePanel, true, 0.2);
                    return;
            }
        }

        private void SwitcherTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InformationTab.IsSelected)
            {
                if (CurTab != CurrentTab.Information)
                {
                    DeactivateCurrentTab(CurTab);
                    FadeAnimation(InfoPanelGrid, false, 0.2);
                }

                CurTab = CurrentTab.Information;
            }
            else if (AccountInformationTab.IsSelected)
            {
                if (CurTab != CurrentTab.AccountInformation)
                {
                    DeactivateCurrentTab(CurTab);
                    FadeAnimation(AccountInformationGrid, false, 0.2);
                }

                CurTab = CurrentTab.AccountInformation;
            }
            else if (AddTab.IsSelected)
            {
                if (CurTab != CurrentTab.Add)
                {
                    DeactivateCurrentTab(CurTab);
                    FadeAnimation(AddPanelGrid, false, 0.2);
                }

                CurTab = CurrentTab.Add;
            }
            else if (EditTab.IsSelected)
            {
                if (CurTab != CurrentTab.Edit)
                {
                    if (!AccountList.Items.IsEmpty)
                    {
                        Edit_ImagePlaceHolder.Visibility = Visibility.Visible;
                        Edit_ImagePlaceHolder1.Visibility = Visibility.Visible;
                        Edit_ImagePlaceHolder2.Visibility = Visibility.Visible;
                        Edit_ImagePlaceHolder3.Visibility = Visibility.Visible;
                        Edit_ImagePlaceHolder4.Visibility = Visibility.Visible;
                        Edit_ImagePlaceHolder5.Visibility = Visibility.Visible;
                        Edit_ImagePlaceHolder6.Visibility = Visibility.Visible;
                        Edit_ImagePlaceHolder7.Visibility = Visibility.Visible;
                        EditNoteBox.Visibility = Visibility.Visible;
                        EditLoginBox.Visibility = Visibility.Visible;
                        EditPwdBox.Visibility = Visibility.Visible;
                        EditTradeLinkBox.Visibility = Visibility.Visible;
                        EditSharedSecretBox.Visibility = Visibility.Visible;
                        EditMailBox.Visibility = Visibility.Visible;
                        EditMailPwdBox.Visibility = Visibility.Visible;
                        EditLoginIcon.Visibility = Visibility.Visible;
                        EditPwdIcon.Visibility = Visibility.Visible;
                        EditTradeLinkIcon.Visibility = Visibility.Visible;
                        EditSharedSecretIcon.Visibility = Visibility.Visible;
                        EditMailBoxIcon.Visibility = Visibility.Visible;
                        EditMailPwdIcon.Visibility = Visibility.Visible;
                        SSFNLabel2.Visibility = Visibility.Visible;
                        EditSSFNPathTextBox.Visibility = Visibility.Visible;
                        EditFindSSFNButton.Visibility = Visibility.Visible;
                        EditContinueBtn.Visibility = Visibility.Visible;
                        EditSteamIDBox.Visibility = Visibility.Visible;
                        EditSteamIDIcon.Visibility = Visibility.Visible;
                        EditMaFileTextBox.Visibility = Visibility.Visible;
                        EditSelectMaBtn.Visibility = Visibility.Visible;

                        DirectoryInfo d = new DirectoryInfo(SSFNFolder + @SelectedAccount.Login);
                        if (d.Exists)
                        {
                            FileInfo[] Files = d.GetFiles("ssfn*.*");
                            if (Files.Length != 0)
                            {
                                EditSSFNPathTextBox.Text = string.Empty;
                                foreach (FileInfo file in Files)
                                {
                                    EditSSFNPathTextBox.Text += file.Name + Environment.NewLine;
                                }
                            }
                            else
                            {
                                EditSSFNPathTextBox.Text = "";
                            }
                        }
                        else
                        {
                            EditSSFNPathTextBox.Text = "";
                        }

                        EditLoginBox.Text = SelectedAccount.Login;
                        if (SelectedAccount.SteamID != string.Empty)
                        {
                            EditSteamIDBox.Text = SelectedAccount.SteamID;
                        }
                        else { EditSteamIDBox.Text = "SteamID"; }

                        if (SelectedAccount.Password.Length != 0)
                        {
                            EditPwdBox.Text = SelectedAccount.Password;
                        }
                        if (AdvInformation.Read("Note", SelectedAccount.Login).Length != 0)
                        {
                            EditNoteBox.Text = AdvInformation.Read("Note", SelectedAccount.Login);
                        }
                        else
                        {
                            EditNoteBox.Text = "Примечание";
                        }
                        if (AdvInformation.Read("Mail", SelectedAccount.Login).Length != 0)
                        {
                            EditMailBox.Text = AdvInformation.Read("Mail", SelectedAccount.Login);
                        }
                        else
                        {
                            EditMailBox.Text = "Почтовый адрес аккаунта";
                        }
                        if (AdvInformation.Read("MailPassword", SelectedAccount.Login).Length != 0)
                        {
                            EditMailPwdBox.Text = AdvInformation.Read("MailPassword", SelectedAccount.Login);
                        }
                        else
                        {
                            EditMailPwdBox.Text = "Пароль от почтового ящика";
                        }
                        if (AdvInformation.Read("TradeLink", SelectedAccount.Login).Length != 0)
                        {
                            EditTradeLinkBox.Text = AdvInformation.Read("TradeLink", SelectedAccount.Login);
                        }
                        else
                        {
                            EditTradeLinkBox.Text = "Ссылка на обмен";
                        }
                        if (AdvInformation.Read("SharedSecret", SelectedAccount.Login).Length != 0)
                        {
                            EditSharedSecretBox.Text = AdvInformation.Read("SharedSecret", SelectedAccount.Login);
                        }
                        else
                        {
                            EditSharedSecretBox.Text = "Секретный ключ";
                        }
                        if (EditSSFNPathTextBox.Text.Length == 0)
                        {
                            EditFindSSFNButton.Content = "Выбрать...";
                        }
                        else
                        {
                            EditFindSSFNButton.Content = "Заменить...";
                        }
                        if (File.Exists(SSAFolder + @"maFiles\" + SelectedAccount.SteamID + ".maFile"))
                        {
                            EditMaFileTextBox.Text = SelectedAccount.SteamID + ".maFile";
                        }
                        else
                        {
                            EditMaFileTextBox.Text = ".maFile отсутствует";
                        }
                    }
                    else
                    {
                        Edit_ImagePlaceHolder.Visibility = Visibility.Hidden;
                        Edit_ImagePlaceHolder1.Visibility = Visibility.Hidden;
                        Edit_ImagePlaceHolder2.Visibility = Visibility.Hidden;
                        Edit_ImagePlaceHolder3.Visibility = Visibility.Hidden;
                        Edit_ImagePlaceHolder4.Visibility = Visibility.Hidden;
                        Edit_ImagePlaceHolder5.Visibility = Visibility.Hidden;
                        Edit_ImagePlaceHolder6.Visibility = Visibility.Hidden;
                        Edit_ImagePlaceHolder7.Visibility = Visibility.Hidden;
                        EditNoteBox.Visibility = Visibility.Hidden;
                        EditLoginBox.Visibility = Visibility.Hidden;
                        EditPwdBox.Visibility = Visibility.Hidden;
                        EditTradeLinkBox.Visibility = Visibility.Hidden;
                        EditSharedSecretBox.Visibility = Visibility.Hidden;
                        EditMailBox.Visibility = Visibility.Hidden;
                        EditMailPwdBox.Visibility = Visibility.Hidden;
                        EditLoginIcon.Visibility = Visibility.Hidden;
                        EditPwdIcon.Visibility = Visibility.Hidden;
                        EditTradeLinkIcon.Visibility = Visibility.Hidden;
                        EditSharedSecretIcon.Visibility = Visibility.Hidden;
                        EditMailBoxIcon.Visibility = Visibility.Hidden;
                        EditMailPwdIcon.Visibility = Visibility.Hidden;
                        SSFNLabel2.Visibility = Visibility.Hidden;
                        EditSSFNPathTextBox.Visibility = Visibility.Hidden;
                        EditFindSSFNButton.Visibility = Visibility.Hidden;
                        EditContinueBtn.Visibility = Visibility.Hidden;
                        EditSteamIDBox.Visibility = Visibility.Hidden;
                        EditSteamIDIcon.Visibility = Visibility.Hidden;
                        EditMaFileTextBox.Visibility = Visibility.Hidden;
                        EditSelectMaBtn.Visibility = Visibility.Hidden;
                    }

                    DeactivateCurrentTab(CurTab);
                    FadeAnimation(EditPanelGrid, false, 0.2);
                }

                CurTab = CurrentTab.Edit;
            }
            else if (DeactivateTab.IsSelected)
            {
                if (CurTab != CurrentTab.Deactivate)
                {
                    DeactivateLabel.Content = "Вы действительно хотите деактивировать аккаунт";
                    AccNameLabel.Content = SelectedAccount.Login + " ?";
                    ConfirmDelete.IsEnabled = true;

                    DeactivateCurrentTab(CurTab);
                    FadeAnimation(DeactivatePanel, false, 0.2);
                }

                CurTab = CurrentTab.Deactivate;
            }
        }

        private void ConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            var RemovingAccount = (AccountsModel)AccountList.SelectedItem;
            var RemovingIndex = AccountList.SelectedIndex;
            try
            {
                if (Directory.Exists(SSFNFolder + RemovingAccount.Login))
                    Directory.Delete(SSFNFolder + RemovingAccount.Login, true);

                AdvInformation.DeleteSection(RemovingAccount.Login);
                File.Delete(CacheFolder + RemovingAccount.SteamID + ".jpg");

                if (AccountList.Items.Count > 1)
                {
                    if (RemovingIndex > 0)
                        RemovingIndex -= 1;
                    else
                        RemovingIndex += 1;
                }
                else
                {
                    RemovingIndex = -1;
                }

                AccountList.SelectedIndex = RemovingIndex;
                AccountsCollection.Remove(RemovingAccount);
                new ShowNotifyMessage("Аккаунт '" + RemovingAccount.Login + "' был успешно удалён из списка.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Information).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Во время удаления аккаунта '{RemovingAccount.Login}' возникла ошибка.\n\nПричина: {ex.Message}");
            }

            BackBtn.PerformClick();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            InformationTab.IsSelected = true;
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            string filePath = SSAFolder + @"maFiles\" + SelectedAccount.SteamID + ".maFile";
            ChooseMaFileLabel.Content = $"Выберите .maFile для аккаунта: {SelectedAccount.Login}";
            FadeAnimation(ModalGrid, false, 0.2);
            ExportProcess(1, filePath);
        }

        private void SelectMaFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (maFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExportProcess(2, maFile.FileName);
            }
        }

        ///<summary>Process 1: .maFile exists and using it automatically. Process 2: .maFile doesn't exists, open grid with manual handle. Process 3: An user clicked "Skip" button.</summary>
        private void ExportProcess(int Process, string path = "")
        {
            string CurrentAccount = $"{SelectedAccount.Login}.ssa";
            AI = new AdvancedInformation(CurrentAccount);
            if (File.Exists(CurrentAccount)) { try { File.Delete(CurrentAccount); } catch { } }

            if (Process == 1)
            {
                if (SelectedAccount.SteamID != string.Empty && File.Exists(path))
                {
                    ConstructTextBlock();
                    SelectMaFileBtn.IsEnabled = false;
                    AI.Write("Login", SelectedAccount.Login, "Account");
                    AI.Write("Password", SelectedAccount.Password, "Account");
                    if (CopyMailBtn.IsEnabled) { AI.Write("EmailLogin", CopyMailBtn.Content.ToString(), "Account"); }
                    else { AI.Write("EmailLogin", "", "Account"); }
                    if (CopyMailPasswordBtn.IsEnabled) { AI.Write("EmailPassword", CopyMailPasswordBtn.Content.ToString(), "Account"); }
                    else { AI.Write("EmailPassword", "", "Account"); }
                    if (CopyTradeBtnPartner.IsEnabled || CopyTradeBtnToken.IsEnabled) { AI.Write("TradeLink", AdvInformation.Read("TradeLink", SelectedAccount.Login), "Account"); }
                    else { AI.Write("TradeLink", "", "Account"); }
                    if (LoginAccount.SharedSecret.Length != 0) { AI.Write("SharedSecret", LoginAccount.SharedSecret, "Account"); }
                    else { AI.Write("SharedSecret", "", "Account"); }
                    if (SelectedAccount.SteamID.Length != 0) { AI.Write("SteamID", SelectedAccount.SteamID, "Account"); }
                    else { AI.Write("SteamID", "", "Account"); }
                    AI.Write("JsonParameter", File.ReadAllText(path), "JSON");

                    TextBlockSSA.Text = EncryptThing(true, CurrentAccount);
                    File.WriteAllText(CurrentAccount, TextBlockSSA.Text);
                    MaFileBox.Text = SelectedAccount.SteamID + ".maFile";
                    CopySSAButton.IsEnabled = true;
                    if (EnableNotifications == 1 || EnableNotifications == 2)
                    {
                        new ShowNotifyMessage("Экспорт аккаунта '" + SelectedAccount.Login + "' завершен.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Information).Show();
                    }
                    SkipMaBtn.IsEnabled = false;
                }
            }
            else if (Process == 2)
            {
                ConstructTextBlock();
                AI.Write("Login", SelectedAccount.Login, "Account");
                AI.Write("Password", SelectedAccount.Password, "Account");
                if (CopyMailBtn.IsEnabled) { AI.Write("EmailLogin", CopyMailBtn.Content.ToString(), "Account"); }
                else { AI.Write("EmailLogin", "", "Account"); }
                if (CopyMailPasswordBtn.IsEnabled) { AI.Write("EmailPassword", CopyMailPasswordBtn.Content.ToString(), "Account"); }
                else { AI.Write("EmailPassword", "", "Account"); }
                if (CopyTradeBtnPartner.IsEnabled || CopyTradeBtnToken.IsEnabled) { AI.Write("TradeLink", AdvInformation.Read("TradeLink", SelectedAccount.Login), "Account"); }
                else { AI.Write("TradeLink", "", "Account"); }
                if (LoginAccount.SharedSecret.Length != 0) { AI.Write("SharedSecret", LoginAccount.SharedSecret, "Account"); }
                else { AI.Write("SharedSecret", "", "Account"); }
                if (SelectedAccount.SteamID.Length != 0) { AI.Write("SteamID", SelectedAccount.SteamID, "Account"); }
                else { AI.Write("SteamID", "", "Account"); }
                AI.Write("JsonParameter", File.ReadAllText(path), "JSON");

                TextBlockSSA.Text = EncryptThing(true, CurrentAccount);
                File.WriteAllText(CurrentAccount, TextBlockSSA.Text);
                MaFileBox.Text = Path.GetFileName(path);
                CopySSAButton.IsEnabled = true;
                if (EnableNotifications == 1 || EnableNotifications == 2)
                {
                    new ShowNotifyMessage("Экспорт аккаунта '" + SelectedAccount.Login + "' завершен.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Information).Show();
                }
                SkipMaBtn.IsEnabled = false;
                SelectMaFileBtn.IsEnabled = false;
            }
            else if (Process == 3)
            {
                ConstructTextBlock();
                AI.Write("Login", SelectedAccount.Login, "Account");
                AI.Write("Password", SelectedAccount.Password, "Account");
                if (CopyMailBtn.IsEnabled) { AI.Write("EmailLogin", CopyMailBtn.Content.ToString(), "Account"); }
                else { AI.Write("EmailLogin", "", "Account"); }
                if (CopyMailPasswordBtn.IsEnabled) { AI.Write("EmailPassword", CopyMailPasswordBtn.Content.ToString(), "Account"); }
                else { AI.Write("EmailPassword", "", "Account"); }
                if (CopyTradeBtnPartner.IsEnabled || CopyTradeBtnToken.IsEnabled) { AI.Write("TradeLink", AdvInformation.Read("TradeLink", SelectedAccount.Login), "Account"); }
                else { AI.Write("TradeLink", "", "Account"); }
                if (LoginAccount.SharedSecret.Length != 0) { AI.Write("SharedSecret", LoginAccount.SharedSecret, "Account"); }
                else { AI.Write("SharedSecret", "", "Account"); }
                if (SelectedAccount.SteamID.Length != 0) { AI.Write("SteamID", SelectedAccount.SteamID, "Account"); }
                else { AI.Write("SteamID", "", "Account"); }

                TextBlockSSA.Text = EncryptThing(true, CurrentAccount);
                if (EnableNotifications == 1 || EnableNotifications == 2)
                {
                    new ShowNotifyMessage("Аккаунт '" + SelectedAccount.Login + "' успешно экспортирован без .maFile", "Steam Switcher", ShowNotifyMessage.NotifyImage.Information).Show();
                }
                CopySSAButton.IsEnabled = true;
            }
        }

        private void ConstructTextBlock()
        {
            TextBlockSSA.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
            TextBlockSSA.VerticalContentAlignment = VerticalAlignment.Top;
            TextBlockSSA.FontSize = 12;
            TextBlockSSA.FontFamily = new FontFamily("Segoe UI Light");
            TextBlockSSA.FontWeight = FontWeights.Normal;
        }

        private void SkipMaBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportProcess(3);
        }

        private void CopySSAButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject(TextBlockSSA.Text, false);
            CopySSAButton.IsEnabled = false;
            AdvancedExtensions.Wait(1000);
            CopySSAButton.IsEnabled = true;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            FadeAnimation(ModalGrid, true, 0.2);
            AdvancedExtensions.Wait(500);
            maFile.FileName = "";
            MaFileBox.Text = "NO FILE SELECTED";
            TextBlockSSA.Text = "NO FILE SELECTED";
            TextBlockSSA.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            TextBlockSSA.VerticalContentAlignment = VerticalAlignment.Center;
            TextBlockSSA.FontSize = 14;
            TextBlockSSA.FontFamily = new FontFamily("Segoe UI");
            TextBlockSSA.FontWeight = FontWeights.Bold;
            CopySSAButton.IsEnabled = false;
            SkipMaBtn.IsEnabled = true;
            SelectMaFileBtn.IsEnabled = true;
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            AccountFileDialog.FileName = "";
            AddSSAFileBox.Text = "PASTE THE TEXT HERE";
            AddSSAFileBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            AddSSAFileBox.VerticalContentAlignment = VerticalAlignment.Center;
            AddSSAFileBox.FontSize = 14;
            AddSSAFileBox.FontFamily = new FontFamily("Segoe UI");
            AddSSAFileBox.FontWeight = FontWeights.Bold;
            AddChooseSSAFileLabel.Content = "Выберите .ssa файл, либо вставьте ссылку для активации аккаунта";
            AddChooseSSAFileLabel.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));
            ExecuteBtn.IsEnabled = false;
            FadeAnimation(AddModalGrid, false, 0.2);
            ImportBtn.IsEnabled = false;
        }

        private void AddCloseBtn_Click(object sender, RoutedEventArgs e)
        {
            FadeAnimation(AddModalGrid, true, 0.2);
            ImportBtn.IsEnabled = true;
        }

        private void AddSSAFileBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (AddSSAFileBox.Text != "PASTE THE TEXT HERE" && AddSSAFileBox.Text.Length > 100)
                {
                    string EncryptedText = AddSSAFileBox.Text;
                    string DecryptedText = "";
                    AccExists = false;
                    DecryptedText = DecryptThing(false, null, EncryptedText);
                    string str = "Аккаунт '" + DecryptedText.Before("Password=").After("Login=");
                    string EndStr = str.Remove(str.Length - 2);

                    if (!AccountList.Items.IsEmpty)
                    {
                        foreach (object item in AccountList.Items)
                        {
                            string[] AccountString = item.ToString().Split(new char[] { ';' });

                            if (EndStr.Contains(AccountString[0]))
                            {
                                AddChooseSSAFileLabel.Content = EndStr + "' уже присутствует в списке.";
                                AddChooseSSAFileLabel.Foreground = new SolidColorBrush(Colors.OrangeRed);
                                AccExists = true;
                                ExecuteBtn.IsEnabled = false;
                                break;
                            }
                            else
                            {
                                AddChooseSSAFileLabel.Content = EndStr + "' отсутствует в списке.";
                                AddChooseSSAFileLabel.Foreground = new SolidColorBrush(Colors.LightGreen);
                                ExecuteBtn.IsEnabled = true;
                                AccExists = false;
                            }
                        }
                    }
                    else
                    {
                        AddChooseSSAFileLabel.Content = EndStr + "' отсутствует в списке.";
                        AddChooseSSAFileLabel.Foreground = new SolidColorBrush(Colors.LightGreen);
                        ExecuteBtn.IsEnabled = true;
                        AccExists = false;
                    }
                }
                else if (AddSSAFileBox.Text.Length == 0)
                {
                    AddChooseSSAFileLabel.Content = "Выберите .ssa файл, либо вставьте ссылку для активации аккаунта";
                    AddChooseSSAFileLabel.Foreground = new SolidColorBrush(Color.FromRgb(176, 176, 176));
                    ExecuteBtn.IsEnabled = false;
                }
            }
            catch (System.Security.Cryptography.CryptographicException) { }
            catch (FormatException) { }
        }

        private void ImportProcess(bool UsingFile)
        {
            DirectoryInfo di = new DirectoryInfo($@"{SSAFolder}maFiles");
            if (!di.Exists) di.Create();
            bool IsSteamIdNotNull = false;

            try
            {
                if (!UsingFile)
                {
                    string DecryptedInformation = "";
                    DecryptedInformation = DecryptThing(false, null, AddSSAFileBox.Text);

                    StreamWriter sw = new StreamWriter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\temp_AccountInfo.ssa");
                    sw.Write(DecryptedInformation);
                    sw.Close();
                    AdvancedInformation AI = new AdvancedInformation(@"temp_AccountInfo.ssa");
                    LoginBox.Text = AI.Read("Login", "Account");

                    if (!AccExists)
                    {
                        PwdBox.Text = AI.Read("Password", "Account");
                        if (AI.Read("EmailLogin", "Account").Length > 0) { MailBox.Text = AI.Read("EmailLogin", "Account"); }
                        if (AI.Read("EmailPassword", "Account").Length > 0) { MailPwdBox.Text = AI.Read("EmailPassword", "Account"); }
                        if (AI.Read("TradeLink", "Account").Length > 0) { TradeLinkBox.Text = AI.Read("TradeLink", "Account"); }
                        if (AI.Read("SharedSecret", "Account").Length > 0) { SharedSecretBox.Text = AI.Read("SharedSecret", "Account"); }
                        if (AI.Read("SteamID", "Account").Length > 0) { SteamIDBox.Text = AI.Read("SteamID", "Account"); IsSteamIdNotNull = true; }
                        if (AI.Read("JsonParameter", "JSON").Length > 0)
                        {
                            File.WriteAllText($@"{SSAFolder}maFiles\{AI.Read("SteamID", "Account")}.maFile", AI.Read("JsonParameter", "JSON"));
                            if (IsSteamIdNotNull)
                            {
                                MakeSSAEntry(AI.Read("SteamID", "Account"));
                            }
                        }

                        AddCloseBtn.PerformClick();
                        ContinueBtn.PerformClick();
                    }
                    else
                    {
                        MessageBox.Show("Такой аккаунт уже есть в списке.", "Steam Switcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    try { File.Delete(@"temp_AccountInfo.ssa"); } catch { }
                }
                else
                {
                    DecryptThing(true, AccountFileDialog.FileName);
                    AdvancedInformation AI = new AdvancedInformation(AccountFileDialog.FileName);
                    LoginBox.Text = AI.Read("Login", "Account");

                    foreach (object item in AccountList.Items)
                    {
                        string[] GetAccountString = item.ToString().Split(new char[] { ';' });

                        if (LoginBox.Text.Equals(GetAccountString[0]))
                        {
                            AccExists = true;
                            break;
                        }
                        else
                        {
                            AccExists = false;
                        }
                    }

                    if (!AccExists)
                    {
                        PwdBox.Text = AI.Read("Password", "Account");
                        if (AI.Read("EmailLogin", "Account").Length != 0) { MailBox.Text = AI.Read("EmailLogin", "Account"); }
                        if (AI.Read("EmailPassword", "Account").Length != 0) { MailPwdBox.Text = AI.Read("EmailPassword", "Account"); }
                        if (AI.Read("TradeLink", "Account").Length != 0) { TradeLinkBox.Text = AI.Read("TradeLink", "Account"); }
                        if (AI.Read("SharedSecret", "Account").Length != 0) { SharedSecretBox.Text = AI.Read("SharedSecret", "Account"); }
                        if (AI.Read("SteamID", "Account").Length != 0) { SteamIDBox.Text = AI.Read("SteamID", "Account"); IsSteamIdNotNull = true; }
                        if (AI.Read("JsonParameter", "JSON").Length != 0)
                        {
                            File.WriteAllText($@"{SSAFolder}maFiles\{AI.Read("SteamID", "Account")}.maFile", AI.Read("JsonParameter", "JSON"));
                            if (IsSteamIdNotNull)
                            {
                                MakeSSAEntry(AI.Read("SteamID", "Account"));
                            }
                        }

                        EncryptThing(true, AccountFileDialog.FileName);
                        AddCloseBtn.PerformClick();
                        ContinueBtn.PerformClick();
                    }
                    else
                    {
                        EncryptThing(true, AccountFileDialog.FileName);
                        if (EnableNotifications == 1 || EnableNotifications == 2)
                        {
                            new ShowNotifyMessage("Аккаунт '" + LoginBox.Text + "' уже есть в списке.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Error).Show();
                        }
                    }
                }
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                if (EnableNotifications == 1 || EnableNotifications == 2)
                {
                    new ShowNotifyMessage("Выбранный вами файл был поврежден, либо вставленный код некорректен.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Error).Show();
                }

            }
            catch (FormatException)
            {
                if (EnableNotifications == 1 || EnableNotifications == 2)
                {
                    new ShowNotifyMessage("Выбранный вами файл был поврежден, либо вставленный код некорректен.", "Steam Switcher", ShowNotifyMessage.NotifyImage.Error).Show();
                }
            }
        }

        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            ImportProcess(false);
        }

        private void AddSelectSSAFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AccountFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImportProcess(true);
            }
        }

        private string EncryptThing(bool ReadWriteFile, string Path = null, string Text = null)
        {
            if (ReadWriteFile)
            {
                string str = File.ReadAllText(Path);
                string key = "&ltKb>lJyuw^%1dw5249*%$enot";
                string encryptedText = AccountFileEncryptor.EncryptTextTo3DES(str, key);
                File.WriteAllText(Path, encryptedText);
                return encryptedText;
            }
            else
            {
                string key = "&ltKb>lJyuw^%1dw5249*%$enot";
                string encryptedText = AccountFileEncryptor.EncryptTextTo3DES(Text, key);
                return encryptedText;
            }
        }

        private string DecryptThing(bool ReadWriteFile, string Path = null, string Text = null)
        {
            if (ReadWriteFile)
            {
                string str = File.ReadAllText(Path);
                string key = "&ltKb>lJyuw^%1dw5249*%$enot";
                string decryptedText = AccountFileEncryptor.DecryptTextFrom3DES(str, key);
                File.WriteAllText(Path, decryptedText);
                return decryptedText;
            }
            else
            {
                string key = "&ltKb>lJyuw^%1dw5249*%$enot";
                string decryptedText = AccountFileEncryptor.DecryptTextFrom3DES(Text, key);
                return decryptedText;
            }
        }

        private T GetVisualChild<T>(Visual parent) where T : Visual
        {
            try
            {
                T child = default(T);
                int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < numVisuals; i++)
                {
                    Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                    child = v as T;
                    if (child == null)
                    {
                        child = GetVisualChild<T>(v);
                    }
                    if (child != null)
                    {
                        break;
                    }
                }
                return child;
            }
            catch (Exception exc)
            {
                MessageBox.Show("Ошибка получения данных из таблицы функция (GetVisualChild<T>)!\n\n" + exc.Message, "Ошибка выполнения приложения", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private DataGridRow GetRow(int index, System.Windows.Controls.DataGrid dg)
        {
            try
            {
                DataGridRow row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
                if (row == null)
                {
                    dg.UpdateLayout();
                    dg.ScrollIntoView(dg.Items[index]);
                    row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);

                }
                return row;
            }
            catch (Exception exc)
            {
                MessageBox.Show("Ошибка получения данных из таблицы функция (GetRow)!\n\n" + exc.Message, "Ошибка выполнения приложения", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private System.Windows.Controls.DataGridCell GetCell(int row, int column, System.Windows.Controls.DataGrid dg)
        {
            try
            {
                DataGridRow rowContainer = GetRow(row, dg);

                if (rowContainer != null)
                {
                    System.Windows.Controls.Primitives.DataGridCellsPresenter presenter = GetVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(rowContainer);

                    System.Windows.Controls.DataGridCell cell = (System.Windows.Controls.DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                    if (cell == null)
                    {
                        dg.ScrollIntoView(rowContainer, dg.Columns[column]);
                        cell = (System.Windows.Controls.DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                    }
                    return cell;
                }
                return null;
            }
            catch (Exception exc)
            {
                MessageBox.Show("Ошибка получения данных из таблицы функция (GetCell)!\n\n" + exc.Message, "Ошибка выполнения приложения", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void ModalGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseBtn.PerformClick();
        }

        private void AddModalGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AddCloseBtn.PerformClick();
        }

        private void SelectMaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MaFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IsMaFileSelected = true;
                MaFileTextBox.Text = MaFileDialog.SafeFileName;

                try
                {
                    string ReadJSONContent = File.ReadAllText(MaFileDialog.FileName);
                    var mafile = JsonConvert.DeserializeObject<maFileJS>(ReadJSONContent);
                    if (LoginBox.Text == "Логин")
                        LoginBox.Text = mafile.account_name;
                    if (SharedSecretBox.Text == "Секретный ключ")
                        SharedSecretBox.Text = mafile.shared_secret;
                    if (SteamIDBox.Text == "SteamID")
                        SteamIDBox.Text = mafile.session.SteamID;
                }
                catch { }
            }
        }

        private void EditSelectMaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditMaFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string ReadJSONContent = File.ReadAllText(EditMaFileDialog.FileName);
                var mafile = JsonConvert.DeserializeObject<maFileJS>(ReadJSONContent);
                if (mafile.account_name != EditLoginBox.Text || mafile.session.SteamID != EditSteamIDBox.Text)
                {
                    if (MessageBox.Show("Логин либо Steam ID редактируемого аккаунта не совпадают с конечными параметрами выбранного файла." + Environment.NewLine + Environment.NewLine +
                        "Если вы продолжите, файл вашего текущего аутентификатора '" + SelectedAccount.SteamID + ".maFile' будет перезаписан, если он был указан ранее." + Environment.NewLine + Environment.NewLine + "Продолжить?", "Steam Switcher", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                    {
                        IsEditMaFileSelected = true;
                        EditMaFileTextBox.Text = EditMaFileDialog.SafeFileName;
                    }
                    else
                    {
                        EditMaFileDialog.FileName = null;
                        IsEditMaFileSelected = false;
                    }

                }
                else
                {
                    IsEditMaFileSelected = true;
                    EditMaFileTextBox.Text = EditMaFileDialog.SafeFileName;
                }
            }
        }

        private void RefreshSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshSessionBtn.IsEnabled = false;
            AccountList.IsEnabled = false;
            AdvancedExtensions.Wait(500);
            Process RefreshingStart = new Process();
            RefreshingStart.StartInfo.Arguments = "-Refresh:" + SelectedAccount.Login + "=" + SelectedAccount.Password;
            RefreshingStart.StartInfo.FileName = SSAFolder + @"Steam Switcher Authenticator.exe";
            RefreshingStart.Start();
            RefreshingStart.WaitForExit();
            //await LoginAccount.RefreshSessionAsync();
            RefreshSessionBtn.IsEnabled = true;
            AccountList.IsEnabled = true;

        }

        private void OpenProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(SelectedAccount.ProfileLink);
        }

        private void SetCopyMargin(System.Windows.Controls.Control control, Grid grid)
        {
            int CurMarginLeft = Convert.ToInt16(control.Margin.Left) - 161;
            //int CurMarginRight = Convert.ToInt16(control.Margin.Right);
            int CurMarginTop = Convert.ToInt16(control.Margin.Top) - 2;
            //int CurMarginBottom = Convert.ToInt16(control.Margin.Bottom);
            Thickness controlmargin = control.Margin;
            controlmargin.Top = CurMarginTop;
            controlmargin.Left = CurMarginLeft;
            grid.Margin = controlmargin;
        }

        private enum CurrentAnnimation
        {
            FromFirstToSecond,
            FromSecondToFirst,
            None
        }

        private void SlideAnimation(Grid control, bool Visible, double From, double To, double Duration, CurrentAnnimation CurAnim)
        {
            double FromParameter = From;
            double ToParameter = To;

            if (!Visible)
            {
                control.Visibility = Visibility.Visible;
            }

            DoubleAnimation a = new DoubleAnimation
            {
                From = FromParameter,
                To = ToParameter,
                FillBehavior = FillBehavior.HoldEnd,
                BeginTime = TimeSpan.FromSeconds(0),
                Duration = new Duration(TimeSpan.FromSeconds(Duration)) // 0.2
            };
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(a);
            Storyboard.SetTarget(a, control);
            Storyboard.SetTargetProperty(a, new PropertyPath("RenderTransform.(TranslateTransform.X)"));
            if (Visible == true)
            {
                storyboard.Completed += delegate
                {
                    control.Visibility = Visibility.Hidden;

                    if (CurAnim == CurrentAnnimation.FromFirstToSecond)
                    {
                        SlideAnimation(AccountPanelGrid2, false, -743.0, 743.0, 0.1, CurrentAnnimation.None);
                    }
                    else if (CurAnim == CurrentAnnimation.FromSecondToFirst)
                    {
                        SlideAnimation(AccountPanelGrid1, false, -743.0, 0.0, 0.1, CurrentAnnimation.None);
                    }
                };
            }
            else
            {
                storyboard.Completed += delegate
                {
                    control.Visibility = Visibility.Visible;
                };
            }
            storyboard.Begin();
        }

        private void AccountInformationBorder_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) // Mouse Wheel Up
            {
                if (AccountPanelGrid1.Visibility == Visibility.Visible)
                {
                    SlideAnimation(AccountPanelGrid1, true, 0.0, -743.0, 0.1, CurrentAnnimation.FromFirstToSecond);
                }
                else
                {
                    SlideAnimation(AccountPanelGrid2, true, 743.0, -743.0, 0.1, CurrentAnnimation.FromSecondToFirst);
                }
            }
            else if (e.Delta < 0) // Mouse Wheel Down
            {
                if (AccountPanelGrid1.Visibility == Visibility.Visible)
                {
                    SlideAnimation(AccountPanelGrid1, true, 0.0, -743.0, 0.1, CurrentAnnimation.FromFirstToSecond);
                }
                else
                {
                    SlideAnimation(AccountPanelGrid2, true, 743.0, -743.0, 0.1, CurrentAnnimation.FromSecondToFirst);
                }
            }
        }

        private void ButtonLeftPanel1_Click(object sender, RoutedEventArgs e)
        {
            SlideAnimation(AccountPanelGrid1, true, 0.0, -743.0, 0.1, CurrentAnnimation.FromFirstToSecond);
        }

        private void ButtonLeftPanel2_Click(object sender, RoutedEventArgs e)
        {
            SlideAnimation(AccountPanelGrid2, true, 743.0, -743.0, 0.1, CurrentAnnimation.FromSecondToFirst);
        }

        private void SharedSecretBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject(SharedSecretBtn.Content.ToString(), false);
            SharedSecretBtn.IsEnabled = false;
            SetCopyMargin(SharedSecretBtn, CopyAccountGrid2);
            FadeAnimation(CopyAccountGrid2, false, 0.1);
            AdvancedExtensions.Wait(500);
            FadeAnimation(CopyAccountGrid2, true, 0.1);
            SharedSecretBtn.IsEnabled = true;
        }

        private void IdentitySecretBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject(IdentitySecretBtn.Content.ToString(), false);
            IdentitySecretBtn.IsEnabled = false;
            SetCopyMargin(IdentitySecretBtn, CopyAccountGrid2);
            FadeAnimation(CopyAccountGrid2, false, 0.1);
            AdvancedExtensions.Wait(500);
            FadeAnimation(CopyAccountGrid2, true, 0.1);
            IdentitySecretBtn.IsEnabled = true;
        }

        private void SteamIDBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject(SteamIDBtn.Content.ToString(), false);
            SteamIDBtn.IsEnabled = false;
            SetCopyMargin(SteamIDBtn, CopyAccountGrid2);
            FadeAnimation(CopyAccountGrid2, false, 0.1);
            AdvancedExtensions.Wait(500);
            FadeAnimation(CopyAccountGrid2, true, 0.1);
            SteamIDBtn.IsEnabled = true;
        }

        private void CopyLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject(SelectedAccount.Login, false);
            CopyLoginBtn.IsEnabled = false;
            AdvancedExtensions.Wait(500);
            CopyLoginBtn.IsEnabled = true;
            if (EnableNotifications == 2)
                new ShowNotifyMessage("Логин скопирован в буффер обмена: " + SelectedAccount.Login, "Steam Switcher").Show();
        }

        private void CopyPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetDataObject(SelectedAccount.Password, false);
            CopyPasswordBtn.IsEnabled = false;
            AdvancedExtensions.Wait(500);
            CopyPasswordBtn.IsEnabled = true;
            if (EnableNotifications == 2)
                new ShowNotifyMessage("Пароль скопирован в буффер обмена: " + SelectedAccount.Password, "Steam Switcher").Show();
        }

        private void TwoFactorCodeBox_Click(object sender, RoutedEventArgs e)
        {
            if (TwoFactorCodeBox.Content.ToString() != "NO SECRET KEY")
            {
                System.Windows.Clipboard.SetDataObject(TwoFactorCodeBox.Content, false);
                CodeRefresher.Enabled = false;
                TwoFactorCodeBox.IsEnabled = false;
                AdvancedExtensions.Wait(500);
                CodeRefresher.Enabled = true;
                TwoFactorCodeBox.IsEnabled = true;
                if (EnableNotifications == 2)
                    new ShowNotifyMessage("Код для авторизации в аккаунт '" + SelectedAccount.Login + "' скопирован: " + TwoFactorCodeBox.Content, "Steam Switcher").Show();
            }
        }

        private async void ReloadCacheBtn_Click(object sender, RoutedEventArgs e)
        {
            ReloadCacheBtn.IsEnabled = false;
            FadeAnimation(CacheRefreshingGrid, false, 0.2);
            List<AccountsModel> accs = new List<AccountsModel>();
            foreach (var acc in AccountsCollection)
                accs.Add(acc);
            await CacheWorker.ReloadFullCache(accs.ToArray(), true);
            UpdateInformation();
            AccountList.Items.Refresh();
            FadeAnimation(CacheRefreshingGrid, true, 0.2);
            ReloadCacheBtn.IsEnabled = true;
        }

        private SteamGuardAccount currentAcc = null;
        List<SteamGuardAccount> accounts = new List<SteamGuardAccount>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo maFilesDir = new DirectoryInfo(SSAFolder + "maFiles");
            foreach (var acc in maFilesDir.GetFiles("*.maFile"))
            {
                string json = File.ReadAllText(acc.FullName);
                var account = JsonConvert.DeserializeObject<SteamGuardAccount>(json);
                accounts.Add(account); ///TODO: Сделать поиск шаред_сикрета через мафайл, вместо добавления в колонках.
            }

            foreach (var item in accounts)
            {
                if (item.AccountName == SelectedAccount.Login)
                {
                    currentAcc = item;
                }
            }

        }

        private void NoteBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (NoteBox.Text.Equals("Примечание"))
            {
                NoteBox.Text = "";
            }
        }

        private void NoteBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NoteBox.Text.Equals(""))
            {
                NoteBox.Text = "Примечание";
            }
        }

        private void EditNoteBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (EditNoteBox.Text.Equals("Примечание"))
            {
                EditNoteBox.Text = "";
            }
        }

        private void EditNoteBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditNoteBox.Text.Equals(""))
            {
                EditNoteBox.Text = "Примечание";
            }
        }

        private async void RefreshSingleAccountBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshSingleAccountBtn.IsEnabled = false;
            AccountsModel[] acc = new AccountsModel[] { SelectedAccount };
            await CacheWorker.ReloadFullCache(acc, true);
            UpdateInformation();
            AccountList.Items.Refresh();
            RefreshSingleAccountBtn.IsEnabled = true;
        }

        private void SkipCacheLoadingBtn_Click(object sender, RoutedEventArgs e)
        {
            FadeAnimation(CacheRefreshingGrid, true, 0.2);
            UpdateInformation();
        }

        private void LoginButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (LoginButton.IsEnabled)
                ToggleGlowEffect(LoginButton, true);
            else
                ToggleGlowEffect(LoginButton, false);
        }

        private void ToggleGlowEffect(object sender, bool Enable)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;

            if (Enable)
            {
                Color c = new Color
                {
                    A = 100,
                    R = 161,
                    G = 225,
                    B = 255
                };
                button.Effect = new System.Windows.Media.Effects.DropShadowEffect()
                {
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Color = c,
                    Direction = 315,
                    RenderingBias = System.Windows.Media.Effects.RenderingBias.Performance

                };
                GlowEffect.Opacity = 1;
            }
            else
            {
                button.Effect = null;
                GlowEffect.Opacity = 0;
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoginButton.PerformClick();
        }
    }

    public class maFileJS
    {
        public string shared_secret { get; set; }
        public string account_name { get; set; }
        public string identity_secret { get; set; }
        public maFileSession session { get; set; }
    }

    public class maFileSession
    {
        public string SteamID { get; set; }
    }
}