using System;
using System.Threading.Tasks;
using System.Windows;

namespace Steam_Switcher
{
    static class AdvancedExtensions
    {
        /// <summary>
        /// Get string value between [first] a and [last] b.
        /// </summary>
        public static string Between(this string value, string a, string b)
        {
            int posA = value.IndexOf(a);
            int posB = value.LastIndexOf(b);
            if (posA == -1)
            {
                return "";
            }
            if (posB == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= posB)
            {
                return "";
            }
            return value.Substring(adjustedPosA, posB - adjustedPosA);
        }

        /// <summary>
        /// Get string value after [first] a.
        /// </summary>
        public static string Before(this string value, string a)
        {
            int posA = value.IndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            return value.Substring(0, posA);
        }

        /// <summary>
        /// Get string value after [last] a.
        /// </summary>
        public static string After(this string value, string a)
        {
            int posA = value.LastIndexOf(a);
            if (posA == -1)
            {
                return "";
            }
            int adjustedPosA = posA + a.Length;
            if (adjustedPosA >= value.Length)
            {
                return "";
            }
            return value.Substring(adjustedPosA);
        }

        public static void PerformClick(this System.Windows.Controls.Button btn)
        {
            btn.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        }

        public static void PerformClick(this System.Windows.Controls.Image btn)
        {
            btn.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Image.MouseDownEvent));
        }

        public static string ToUTF8(this string text)
        {
            return System.Text.Encoding.UTF8.GetString(System.Text.Encoding.Default.GetBytes(text));
        }

        public static string ToASCII(this string text)
        {
            return System.Text.Encoding.ASCII.GetString(System.Text.Encoding.Default.GetBytes(text));
        }

        public static void Wait(int ms)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < ms)
            {
                System.Windows.Forms.Application.DoEvents();
            }
        }
    }
}
