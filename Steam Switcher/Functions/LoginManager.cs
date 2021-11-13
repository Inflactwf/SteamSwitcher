using Steam_Switcher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Steam_Switcher.Functions
{
    public class LoginManager
    {
        private Process SteamProcess = new Process();

        public bool IsSteamProcessAlive
        {
            get
            {
                try
                {
                    if (!SteamProcess.HasExited)
                        return true;
                    else return false;
                }
                catch { return false; }
            }
        }

        public bool KillProcess()
        {
            try
            {
                if (SteamProcess != null)
                    SteamProcess.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        public static IEnumerable<IntPtr> FindWindowsWithText(string titleText)
        {
            return FindWindows(delegate (IntPtr wnd, IntPtr param)
            {
                return GetWindowText(wnd).Contains(titleText);
            });
        }

        public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                if (filter(wnd, param))
                {
                    windows.Add(wnd);
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return string.Empty;
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public void DoLogin(string location, AccountsModel account = null, string mobileGuardCode = null)
        {
            if (IsSteamProcessAlive)
                KillProcess();

            SteamProcess.StartInfo.FileName = location + @"\Steam.exe";

            if (account != null)
            {
                SteamProcess.StartInfo.Arguments = $"-login {account.Login} {account.Password}";
                SteamProcess.Start();
                if (mobileGuardCode != null)
                {
                    Debug.WriteLine("Autofill started...");
                    if (!SteamProcess.HasExited)
                    {
                        Timer loginTimer = new Timer();
                        loginTimer.Interval = 100;
                        loginTimer.Tick += (sender, e) =>
                        {
                            loginTimer.Stop();
                            var handles = FindWindowsWithText("Steam Guard");
                            if (handles.Count() > 0)
                            {
                                handles.AsParallel().ForAll(x =>
                                {
                                    if (SetForegroundWindow(x))
                                    {
                                        SendKeys.SendWait(mobileGuardCode);
                                        SendKeys.SendWait("{ENTER}");
                                    }
                                });

                                AdvancedExtensions.Wait(2000);
                                var getHandles = FindWindowsWithText("Steam Guard");
                                if (getHandles.Count() == 0)
                                    loginTimer.Dispose();
                                else
                                    loginTimer.Start();
                            }
                            else
                                loginTimer.Start();
                        };
                        loginTimer.Start();
                    }
                }
            }
            else
            {
                SteamProcess.StartInfo.Arguments = string.Empty;
                SteamProcess.Start();
            }
        }
    }
}
