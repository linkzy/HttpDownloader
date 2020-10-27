# HttpDownloader
The HttpDownloader derives from the WebClient class. It serves as a helper to download web pages.

The original code was found in [here](https://stackoverflow.com/questions/2700638/characters-in-string-changed-after-downloading-html-from-the-internet). To suit my needs i added a few modifications.

# Usage
````c#
HttpDownloader _downloader = new HttpDownloader("https://www.google.com");
string html = _downloader.GetPageHtml();
````

