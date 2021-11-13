using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Steam_Switcher
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        bool ChangeDir = false;
        string SSAFolder;

        public Settings(string DirectoryPath)
        {
            InitializeComponent();
            CurrentPathBox.Text = DirectoryPath;
            SSAFolder = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\bin\SSA\";
        }

        private void ShowSDABtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(SSAFolder + @"Steam Switcher Authenticator.exe", "-ShowForm");
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangePathBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangeDir = true;
            Close();
        }

        public bool NeedToChangeSteamDir => ChangeDir;
    }
}
