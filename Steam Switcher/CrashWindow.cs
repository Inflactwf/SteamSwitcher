using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Steam_Switcher
{
    public partial class CrashWindow : Form
    {
        int TimeLeft;
        int OriginalFormHeight;
        int StretchFormHeight;
        int StretchControlHeight;
        public bool DebugMode;
        string ExceptionMessage;
        FileStream fsToDump;

        public CrashWindow(string Message)
        {
            InitializeComponent();
            DebugMode = false;
            TimeLeft = 3;
            OriginalFormHeight = 313;
            StretchFormHeight = 225;
            StretchControlHeight = 166;
            Height = StretchFormHeight;
            ControlsPositionOnStart();
            LoadingGrid.Enabled = false;
            LoadingGrid.Visible = false;
            LoadingGrid.BackColor = Color.FromArgb(0, 0, 0, 0);
            LoadingImg.Visible = false;
            ExitTimer.Enabled = false;
            ExceptionMessage = Message;
            ExceptionLabel.Text = ExceptionMessage;
            ExceptionLabel.Height = 80;
        }

        public void ControlsPositionOnStart()
        {
            ExitBtn.Location = new Point(ExitBtn.Location.X, ExitBtn.Location.Y - StretchControlHeight + 80);
            MakeDmpBtn.Location = new Point(MakeDmpBtn.Location.X, MakeDmpBtn.Location.Y - StretchControlHeight + 80);
            TipLabel.Location = new Point(TipLabel.Location.X, TipLabel.Location.Y - StretchControlHeight);
            ExceptionLabel.Location = new Point(TipLabel.Location.X, TipLabel.Location.Y + 50);
        }

        private void MakeDmpBtn_Click(object sender, EventArgs e)
        {
            MakeDmpBtn.Enabled = false;
            AnimationTimer.Enabled = true;
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void DumpWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DumpBar.PerformStep();
            string FileToDump = Application.StartupPath + @"\CrashDump " + "[" + DateTime.Now.ToLongDateString() + "]-" + "[" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "].dmp";

            fsToDump = null;
            if (File.Exists(FileToDump))
            {
                fsToDump = File.Open(FileToDump, FileMode.Append);
            }
            else
            {
                fsToDump = File.Create(FileToDump);
            }

            DumpBar.PerformStep();
        }

        private void DumpWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            WriteDumpWorker.RunWorkerAsync();
        }

        private void ExitTimer_Tick(object sender, EventArgs e)
        {
            if (TimeLeft > 0)
            {
                TimeLeft = TimeLeft - 1;
                TimerLabel.Text = "Отчёт успешно создан. Завершение работы... (" + TimeLeft.ToString() + ")";
            }
            else
            {
                ExitTimer.Stop();
                TimerLabel.Text = "Отчёт успешно создан. Завершение работы... (0)";
                ExitBtn.PerformClick();
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (Height < OriginalFormHeight)
            {
                Height += 5;
            }
            else
            {
                AnimationTimer.Stop();
                ExceptionLabel.Visible = false;
                ExitBtn.Location = new Point(ExitBtn.Location.X, ExitBtn.Location.Y + 90);
                MakeDmpBtn.Location = new Point(MakeDmpBtn.Location.X, MakeDmpBtn.Location.Y + 90);
                TipLabel.Location = new Point(TipLabel.Location.X, TipLabel.Location.Y + StretchControlHeight);
                LoadingGrid.BackColor = Color.FromArgb(99, 0, 0, 0);
                LoadingGrid.Enabled = true;
                LoadingGrid.Visible = true;
                LoadingImg.Visible = true;
                DumpTimer.Start();
                DumpBar.PerformStep();
            }
        }

        private void DebugBtn_Click(object sender, EventArgs e)
        {
            DebugMode = true;
            Close();
        }

        private void DumpTimer_Tick(object sender, EventArgs e)
        {
            DumpTimer.Stop();
            DumpWorker.RunWorkerAsync();
        }

        internal enum MINIDUMP_TYPE
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        [DllImport("dbghelp.dll")]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            int ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallackParam);

        private void WriteDumpWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DumpBar.PerformStep();
            Process thisProcess = Process.GetCurrentProcess();
            MiniDumpWriteDump(thisProcess.Handle, thisProcess.Id,
    fsToDump.SafeFileHandle.DangerousGetHandle(), MINIDUMP_TYPE.MiniDumpWithFullMemory,
    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        private void WriteDumpWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DumpBar.PerformStep();
            LoadingImg.Image = Properties.Resources.DoneIcon;
            TimerLabel.Visible = true;
            ExitTimer.Start();
            fsToDump.Close();
        }
    }
}
