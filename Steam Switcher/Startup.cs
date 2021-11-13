using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Steam_Switcher
{
    class Startup
    {
        [STAThread()]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]

        public static void Main()
        {
            if (PriorProcess() != null)
            {
                return;
            }

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public static Process PriorProcess()
        {
            Process curr = Process.GetCurrentProcess();
            Process[] procs = Process.GetProcessesByName(curr.ProcessName);
            foreach (Process p in procs)
            {
                if ((p.Id != curr.Id) &&
                    (p.MainModule.FileName == curr.MainModule.FileName))
                {
                    SetForegroundWindow(p.MainWindowHandle);
                    return p;
                }
            }
            return null;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //    ErrorLog ErrorLog = new ErrorLog();
            //    ErrorLog.Write("Steam Switcher Exception Report", e.ExceptionObject.ToString());
            //    CrashWindow CW = new CrashWindow(e.ExceptionObject.ToString());
            //    Thread.Sleep(1000);
            //    CW.ShowDialog();
            //    if (!CW.DebugMode)
            //    {
            //        Environment.Exit(0);
            //    }

            string errorMessage = "Произошла непредвиденная ошибка при работе с программой.\n\n" +
        "Ошибка: " + e.ExceptionObject;
            MessageBox.Show(errorMessage, "Unhandled Exception Occured", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
        }
    }
}
