using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WoongConnector.Tools
{
    public class AccountCheck
    {
        public static bool CheckAccount(string url, string username, string password, out string message)
        {
            message = "";
            var sha1Pass = CalculateSHA1(password, Encoding.ASCII).ToLower();
            var request = $"{url}?username={username}&password={sha1Pass}";

            try
            {
                var response = GetString(request);
                var success = (response.Substring(0, 1) == "1");

                if (!success)
                    message = response.Substring(2, response.Length - 2);

                return success;
            }

            catch (WebException e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Calculates SHA1 hash
        /// </summary>
        /// <param name="text">input string</param>
        /// <param name="enc">Character encoding</param>
        /// <returns>SHA1 hash</returns>
        private static string CalculateSHA1(string text, Encoding enc)
        {
            var buffer = enc.GetBytes(text);
            SHA1CryptoServiceProvider cryptoTransformToSHA1 = new SHA1CryptoServiceProvider();
            var hash = BitConverter.ToString(cryptoTransformToSHA1.ComputeHash(buffer)).Replace("-", "");

            return hash;
        }

        private static string GetString(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Proxy = null;

            WebResponse myResponse = request.GetResponse();
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.ASCII);

            var result = sr.ReadToEnd();

            sr.Close();
            myResponse.Close();

            return result;
        }
    }
}
