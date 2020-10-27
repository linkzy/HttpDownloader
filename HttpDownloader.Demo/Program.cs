using System;

namespace HttpDownloader.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpDownloader _downloader = new HttpDownloader("https://www.google.com");
            Console.WriteLine(_downloader.GetPageHtml());
        }
    }
}
