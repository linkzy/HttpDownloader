using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace HttpDownloader
{
    public class HttpDownloader : WebClient
    {
        private string _url;

        public HttpDownloader(CookieContainer container, string url)
        {
            _url = url;

            //Set "Fake" Headers
            this.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.56 Safari/536.5");
            this.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            this.Headers.Add("Accept-Encoding", "identity");
            this.Headers.Add("Accept-Language", "en-US");
            this.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
            this.Headers.Add("ContentType", "text/html; charset=utf-8");
            this.Headers.Add("Referer", "https://www.bing.com/");

            //Make a request to the main domain to get a cookie
            var uri = new Uri(url);
            var baseUri = uri.GetLeftPart(System.UriPartial.Authority);

            var request = (HttpWebRequest)WebRequest.Create(baseUri);
            request.Method = "GET";
            request.ContentType = "text/html; charset=utf-8";

            container = request.CookieContainer = new CookieContainer();

            var response = request.GetResponse();
            response.Close();
            this.CookieContainer = container;

            //Following requests should use base domain as reffer
            this.Headers.Add("Referer", baseUri);
        }

        public HttpDownloader(string url) : this(new CookieContainer(), url)
        { }

        public CookieContainer CookieContainer { get; private set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            request.CookieContainer = CookieContainer;
            return request;
        }

        public string GetPageHtml()
        {
            var response = (HttpWebResponse)this.GetWebResponse(_url);
            var html = ProcessContent(response);
            return html;
        }

        private HttpWebResponse GetWebResponse(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = CookieContainer;
            return (HttpWebResponse)this.GetWebResponse(request);
        }

        private string ProcessContent(HttpWebResponse response)
        {
            SetEncodingFromHeader(response);

            Stream s = response.GetResponseStream();

            if (response.ContentEncoding != null && response.ContentEncoding.ToLower().Contains("gzip"))
                s = new GZipStream(s, CompressionMode.Decompress);
            else if (response.ContentEncoding != null && response.ContentEncoding.ToLower().Contains("deflate"))
                s = new DeflateStream(s, CompressionMode.Decompress);

            MemoryStream memStream = new MemoryStream();
            int bytesRead;
            byte[] buffer = new byte[0x1000];
            for (bytesRead = s.Read(buffer, 0, buffer.Length); bytesRead > 0; bytesRead = s.Read(buffer, 0, buffer.Length))
            {
                memStream.Write(buffer, 0, bytesRead);
            }
            s.Close();
            string html;
            memStream.Position = 0;
            using (StreamReader r = new StreamReader(memStream, Encoding))
            {
                html = r.ReadToEnd().Trim();
                html = CheckMetaCharSetAndReEncode(memStream, html);
            }

            return html;
        }

        private void SetEncodingFromHeader(HttpWebResponse response)
        {
            string charset = null;
            if (string.IsNullOrEmpty(response.CharacterSet))
            {
                Match m = Regex.Match(response.ContentType, @";\s*charset\s*=\s*(?<charset>.*)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    charset = m.Groups["charset"].Value.Trim(new[] { '\'', '"' });
                }
            }
            else
            {
                charset = response.CharacterSet;
            }
            if (!string.IsNullOrEmpty(charset))
            {
                try
                {
                    Encoding = Encoding.GetEncoding(charset);
                }
                catch (ArgumentException)
                {
                }
            }
        }

        private string CheckMetaCharSetAndReEncode(Stream memStream, string html)
        {
            Match m = new Regex(@"<meta\s+.*?charset\s*=\s*""?(?<charset>[A-Za-z0-9_-]+)""?", RegexOptions.Singleline | RegexOptions.IgnoreCase).Match(html);
            if (m.Success)
            {
                string charset = m.Groups["charset"].Value.ToLower() ?? "iso-8859-1";
                if ((charset == "unicode") || (charset == "utf-16"))
                {
                    charset = "utf-8";
                }

                try
                {
                    Encoding metaEncoding = Encoding.GetEncoding(charset);
                    if (Encoding != metaEncoding)
                    {
                        memStream.Position = 0L;
                        StreamReader recodeReader = new StreamReader(memStream, metaEncoding);
                        html = recodeReader.ReadToEnd().Trim();
                        recodeReader.Close();
                    }
                }
                catch (ArgumentException)
                {
                }
            }

            return html;
        }
    }
}
