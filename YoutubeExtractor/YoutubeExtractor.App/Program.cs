using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace YoutubeExtractor.App
{
    class Program
    {
        static void Main(string[] args)
        {
            AssemblySmartResolver.Install(typeof(Program).Assembly);
            Run(args);
        }

        private static string Between(string source, string tag1, string tag2)
        {
            var n = source.IndexOf(tag1);
            if (n == -1) return "";
            var m = source.IndexOf(tag2, n + tag1.Length);
            if (m == -1) return "";
            return source.Substring(n + tag1.Length, m - n - tag1.Length);
        }

        private static bool HasAt<T>(T[] array, int index)
        {
            return index < array.Length;
        }

        private static T At<T>(T[] array, int index)
        {
            return index < array.Length ? array[index] : default(T);
        }

        private static void Run(string[] args)
        {
            switch (At(args, 0))
            {
                case "search" when HasAt(args, 1):
                    RunSearch(args[1]);
                    break;
                case "info" when HasAt(args, 1):
                    RunInfo(args[1]);
                    break;
                case "download" when HasAt(args, 1) && HasAt(args, 2):
                    RunDownload(args[1], args[2], At(args, 3), At(args, 4), At(args, 5));
                    break;
                default:
                    Console.WriteLine("app.exe download https://www.youtube.com/watch?v=XXXXXXXX output.mp4  [0|480|720|1080] [Mp4|WebM] [Aac|Unknown]");
                    Console.WriteLine("app.exe info https://www.youtube.com/watch?v=XXXXXXXX");
                    break;
            }
        }

        private static void RunInfo(string url)
        {
            var videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false);
            var filtered = videoInfos
                .Where(it => !(it.VideoType == VideoType.Unknown && it.AudioType == AudioType.Unknown && it.Resolution == 0))
                .OrderBy(it => it.VideoType)
                .ThenBy(it => it.Resolution);

            foreach (var info in filtered)
            {
                var str = $"{info.Resolution.ToString().PadLeft(6, ' ')}{info.VideoType.ToString().PadLeft(6, ' ')}{info.AudioType.ToString().PadLeft(9, ' ')}  '{info.Title}'";
                Console.WriteLine(str);
            }
        }

        private static void RunDownload(string url, string path, string resolution, string videoType, string audioType)
        {
            videoType = videoType ?? VideoType.Mp4.ToString();
            audioType = audioType ?? AudioType.Aac.ToString();
            resolution = resolution ?? 720.ToString();

            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false);

            VideoInfo video = videoInfos
                .First(info =>
                    info.VideoType.ToString() == videoType &&
                    info.AudioType.ToString() == audioType &&
                    info.Resolution.ToString() == resolution);

            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);

            var videoDownloader = new VideoDownloader(video, path);
            videoDownloader.Execute();
        }

        private static void RunSearch(string search)
        {
            var query = string.Join("+", search.ToLowerInvariant().Split(' '));
            var url = $"https://www.youtube.com/results?hl=en&search_query={WebUtility.UrlEncode(search)}";

            using (var client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                var str = client.DownloadString(url);

                var items = str.Split(new string[] { "<a href=\"/watch?v=" }, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1).ToList();

                foreach (var item in items)
                {
                    string id = Between(item, "", "\"");
                    if (id.Length > 15)
                        continue;

                    string title = Between(item, "title=\"", "\"");
                    string duration = Between(item, "> - Duration:", "<").Trim('.').PadLeft(8, ' ');
                    string user = Between(item, "href=\"/user/", "\"");
                    Console.WriteLine($"https://www.youtube.com/watch?v={id}     {duration}     [{user}]    '{title}'");
                }
            }
        }
    }
}
