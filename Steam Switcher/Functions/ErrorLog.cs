using System;
using System.IO;

namespace Steam_Switcher.Functions
{
    class ErrorLog
    {
        public void Write(string loginfo, string stacktrace = "")
        {
            string LogFile = @"Steam Switcher.log";

            try
            {
                StreamWriter LogWriter = File.AppendText(LogFile);
                if (stacktrace.Length != 0)
                {
                    LogWriter.WriteLine("[" + DateTime.Now + "]: " + loginfo + " (" + stacktrace + ")");
                }
                else
                {
                    LogWriter.WriteLine("[" + DateTime.Now + "]: " + loginfo);
                }
                LogWriter.Close();
            }
            catch { }
        }
    }
}
