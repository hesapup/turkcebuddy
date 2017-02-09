using System;
using System.Net;
using System.Threading.Tasks;

namespace AramBuddy.MainCore.Common
{
    public static class Weeb
    {
        public static async Task<string> ReadString(string url)
        {
            string result;

            try
            {
                var uri = new Uri(url);

                using (var webclient = new WebClient())
                {
                    result = await webclient.DownloadStringTaskAsync(uri);
                }
            }
            catch (Exception ex)
            {
                result = null;
            }

            return result;
        }
    }
}
