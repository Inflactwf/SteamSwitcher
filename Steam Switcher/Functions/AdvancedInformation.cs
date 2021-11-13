namespace Steam_Switcher.Functions
{
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class AdvancedInformation
    {
        private string EXE = Assembly.GetExecutingAssembly().GetName().Name;
        private string Path;

        public AdvancedInformation(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? (EXE + ".ini")).FullName.ToString();
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? EXE);
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
        public bool KeyExists(string Key, string Section = null)
        {
            return (Read(Key, Section).Length > 0);
        }

        public string Read(string Key, string Section = null)
        {
            StringBuilder retVal = new StringBuilder(0xFFFF);
            GetPrivateProfileString(Section ?? EXE, Key, "", retVal, 0xFFFF, Path);
            return retVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);
    }
}

