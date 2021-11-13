using System;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;

namespace Steam_Switcher
{
    /// <summary>
    /// Shows the notification window with text.
    /// </summary>
    public partial class ShowNotifyMessage : Window
    {
        Timer AnimationTimer;
        string CallSound;

        public ShowNotifyMessage(string NotificationText, string CaptionText = "Оповещение", NotifyImage NotifyIcon = NotifyImage.None, string SoundName = "CopySound", int ShowTime = 2000)
        {
            InitializeComponent();
            AnimationTimer = new Timer
            {
                Interval = ShowTime,
                Enabled = false
            };

            if (NotifyIcon == NotifyImage.Error)
            {
                IconImage.Visibility = Visibility.Visible;
                NotifyCaptionText.Margin = new Thickness(48, 15, 0, 0);
                IconImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/ErrorImg.png"));
            }
            if (NotifyIcon == NotifyImage.Information)
            {
                IconImage.Visibility = Visibility.Visible;
                NotifyCaptionText.Margin = new Thickness(48, 15, 0, 0);
                IconImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/InfoImg.png"));
            }
            if (NotifyIcon == NotifyImage.Warning)
            {
                IconImage.Visibility = Visibility.Visible;
                NotifyCaptionText.Margin = new Thickness(48, 15, 0, 0);
                IconImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/WarningImg.png"));
            }
            if (NotifyIcon == NotifyImage.None)
            {
                IconImage.Visibility = Visibility.Hidden;
                NotifyCaptionText.Margin = new Thickness(26, 15, 0, 0);
                IconImage.Source = null;
            }

            AnimationTimer.Tick += new EventHandler(AnimationTimer_Tick);
            NotifyText.Text = NotificationText;
            NotifyCaptionText.Content = CaptionText;
            CallSound = SoundName;
            CopyPlaySound();
        }


        public enum NotifyImage
        {
            Information,
            Warning,
            Error,
            None
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (NotifyCaptionText.IsMouseOver || NotifyText.IsMouseOver || MainGrid.IsMouseOver || FrameBorder.IsMouseOver || MainWindow.IsMouseOver)
            {
                AnimationTimer.Interval = 4000;
            }
            else
            {
                FadeAnimation(true);
            }
        }

        private void FadeAnimation(bool Visible)
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
                Visibility = Visibility.Visible;
                FromParameter = 0.0;
                ToParameter = 1.0;
            }

            DoubleAnimation a = new DoubleAnimation
            {
                From = FromParameter,
                To = ToParameter,
                FillBehavior = FillBehavior.Stop,
                BeginTime = TimeSpan.FromSeconds(0),
                Duration = new Duration(TimeSpan.FromSeconds(0.2))
            };
            Storyboard storyboard = new Storyboard();

            storyboard.Children.Add(a);
            Storyboard.SetTarget(a, this);
            Storyboard.SetTargetProperty(a, new PropertyPath(OpacityProperty));
            if (Visible == true)
            {
                storyboard.Completed += delegate { Close(); };
            }
            else
            {
                storyboard.Completed += delegate { Visibility = System.Windows.Visibility.Visible; };
            }
            storyboard.Begin();
        }

        public void CopyPlaySound()
        {
            new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("Steam_Switcher." + CallSound + ".wav")).Play();
        }

        private void fadeCompleted(object sender, EventArgs e)
        {
            Close();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            FadeAnimation(false);
            AnimationTimer.Start();

            NotifyText.Height = Double.NaN;
            int oneline = 17;
            int Lines = NotifyText.LineCount;
            int NeedPixels = Lines * oneline;
            Height += NeedPixels - 50;
            MainGrid.Height += NeedPixels - 50;
            FrameBorder.Height += NeedPixels - 50;
            Left = Screen.PrimaryScreen.WorkingArea.Width - Width - 10;
            Top = Screen.PrimaryScreen.WorkingArea.Height - Height - 10;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            FadeAnimation(true);
        }

        private void CloseButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CloseButton.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 127, 199));
            CloseButton.Opacity = 1;
        }

        private void CloseButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CloseButton.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 100, 199));
            CloseButton.Opacity = 0.85;
        }

        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                int tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int IntSetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        private void MainGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FadeAnimation(true);
        }

        private void NotifyText_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FadeAnimation(true);
        }
    }
}