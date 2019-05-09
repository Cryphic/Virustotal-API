using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Windows.Forms;
using System.Threading;
namespace FNWS
{
    public static class WbRequest
    {
   
        public static string Login(string username, string password, Uri url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            // POST metodi
            request.Method = "POST";
            request.UserAgent = "FNWS";
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            request.CachePolicy = noCachePolicy;
            // Luodaan POST Data ja muutetaan se Bytes muotoon.
            string postData = "username=" + username + "&password=" + password;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // ContentType webrequestiin
            request.ContentType = "application/x-www-form-urlencoded";
            // ContentLength pituus WebRequestiin.
            request.ContentLength = byteArray.Length;
            request.AllowAutoRedirect = false;
            // Hae request stream
            Stream dataStream = request.GetRequestStream();
            //Kirjoitetaan data streamiin
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Suljetaan Stream objekti
            dataStream.Close();
            // Hae vastaus
            WebResponse response = request.GetResponse();
            // Hae stream jossa servun data.
            dataStream = response.GetResponseStream();
            // Avaa stream streamreaderilla.
            StreamReader reader = new StreamReader(dataStream ?? throw new InvalidOperationException());
            // Read the content.
            string responseFromServer = reader.ReadToEnd();


            reader.Close();
            response.Close();

            return responseFromServer;
        }
        public static string URLRequest(string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    return client.DownloadString(new Uri(url));
                }
            }
            catch (WebException ex)
            {
                MessageBox.Show(@"Virhe:
" + ex.Message, "Virhe", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return null;
            }
        }
    }
}