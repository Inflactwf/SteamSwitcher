using System;
using System.Security.Cryptography;
using System.Text;

namespace Steam_Switcher.Functions
{
    class AccountFileEncryptor
    {
        private static TripleDES Create3DES(string key)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            TripleDES des = new TripleDESCryptoServiceProvider
            {
                Key = md5.ComputeHash(Encoding.Unicode.GetBytes(key))
            };
            des.IV = new byte[des.BlockSize / 8];
            return des;
        }

        public static string EncryptTextTo3DES(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException("plainText");
            }

            TripleDES des = Create3DES(key);
            ICryptoTransform ct = des.CreateEncryptor();
            byte[] input = Encoding.Unicode.GetBytes(plainText);
            byte[] resArr = ct.TransformFinalBlock(input, 0, input.Length);
            string result = Convert.ToBase64String(resArr);
            return result;
        }

        public static string DecryptTextFrom3DES(string cypherText, string key)
        {
            if (string.IsNullOrEmpty(cypherText))
            {
                throw new ArgumentNullException("cypherText");
            }

            byte[] b = Convert.FromBase64String(cypherText);
            TripleDES des = Create3DES(key);
            ICryptoTransform ct = des.CreateDecryptor();
            byte[] output = ct.TransformFinalBlock(b, 0, b.Length);
            return Encoding.Unicode.GetString(output);
        }
    }
}
