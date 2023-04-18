using System.Net;
using System.Text;

namespace Tron.Wallet.Recycle {
    public static class HttpClientHelper {
        public static string Get(string url, int timeout = 12000) {
#pragma warning disable SYSLIB0014 // 类型或成员已过时
            var resp = Get((HttpWebRequest)WebRequest.Create(url), timeout);
#pragma warning restore SYSLIB0014 // 类型或成员已过时
            using (var s = resp.GetResponseStream()) {
                using (var sr = new StreamReader(s)) {
                    return sr.ReadToEnd();
                }
            }
        }

        private static HttpWebResponse Get(HttpWebRequest req, int timeout = 12000) {
            req.Method = "GET";
            req.ContentType = "application/json";
            req.Timeout = timeout;
            req.Accept = "application/json";
            req.Headers.Set("TRON-PRO-API-KEY", "faffed07-99b4-41a4-9c30-a614674fdbb4");
            return (HttpWebResponse)req.GetResponse();
        }

        public static string Post(string url, string requestBody, Encoding encoding, int timeout = 12000) {
#pragma warning disable SYSLIB0014 // 类型或成员已过时
            var resp = Post((HttpWebRequest)WebRequest.Create(url), requestBody, encoding, timeout);
#pragma warning restore SYSLIB0014 // 类型或成员已过时

            using (var s = resp.GetResponseStream())
            using (var sr = new StreamReader(s)) {
                return sr.ReadToEnd();
            }
        }

        private static HttpWebResponse Post(HttpWebRequest req, string requestBody, Encoding encoding, int timeout = 12000) {
            var bs = encoding.GetBytes(requestBody);

            req.Method = "POST";
            req.ContentType = "application/json";
            req.ContentLength = bs.Length;
            req.Timeout = timeout;
            req.Accept = "application/json";
            req.Headers.Set("TRON-PRO-API-KEY", "faffed07-99b4-41a4-9c30-a614674fdbb4");
            using (var s = req.GetRequestStream()) {
                s.Write(bs, 0, bs.Length);
            }

            return (HttpWebResponse)req.GetResponse();
        }
    }
}
