using Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace YoutubeExtractor.App
{
    [AppOptions(Description = "youtube toolset")]
    class Options
    {
        [AppOptions(FullKeys = new[] { "help" }, ShortKeys = new[] { "h" })]
        public bool Help { get; set; }

        //----------------------

        [AppOptions(FullKeys = new[] { "search" }, Description = "Search string")]
        public string Search { get; set; }

        //----------------------

        [AppOptions(FullKeys = new[] { "info" }, Description = "Dispay info")]
        public string Info { get; set; }

        //----------------------

        [AppOptions(FullKeys = new[] { "download"}, Description = "Download video")]
        public string Download { get; set; }

        [AppOptions(FullKeys = new[] { "output" }, ShortKeys = new[] { "o" }, Description = "Optional. Output download file")]
        public string Output { get; set; }

        [AppOptions(FullKeys = new[] { "resolution" }, ShortKeys = new[] { "r" }, Description = "Optional. Preferred download resolution")]
        public int Resolution { get; set; } = 720;

        [AppOptions(FullKeys = new[] { "video-type" }, ShortKeys = new[] { "vt" }, Description = "Optional. Preferred download video type (Mp4|WebM)")]
        public string VideoType { get; set; }

        [AppOptions(FullKeys = new[] { "audio-type" }, ShortKeys = new[] { "at" }, Description = "Optional. Preferred download audio type (Aac|Unknown)")]
        public string AudioType { get; set; }
    }

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
            var o = new Options();
            if (!AppOptions.TryParse(args, o) || o.Help)
            {
                AppOptions.PrintHelp<Options>();
            }
            else if (o.Info != null)
            {
                RunInfo(o.Info);
            }
            else if (o.Search != null)
            {
                RunSearch(o.Search);
            }
            else if (o.Download != null)
            {
                RunDownload(o.Download, o.Output, o.Resolution, o.VideoType, o.AudioType);
            }
            else
            {
                AppOptions.PrintHelp<Options>();
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

        private static void RunDownload(string url, string path, int resolution, string videoType, string audioType)
        {
            videoType = videoType ?? VideoType.Mp4.ToString();
            audioType = audioType ?? AudioType.Aac.ToString();

            var videoInfos = DownloadUrlResolver.GetDownloadUrls(url, false).ToList();

            var cc = videoInfos.OrderBy(it => Math.Abs(it.Resolution - resolution)).ToList();
            if (videoType != null && cc.Any(it => it.VideoType.ToString() == videoType))
                cc = cc.Where(it => it.VideoType.ToString() == videoType).ToList();
            if (audioType != null && cc.Any(it => it.AudioType.ToString() == audioType))
                cc = cc.Where(it => it.AudioType.ToString() == audioType).ToList();

            VideoInfo video = cc.First();

            if (video.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(video);

            if (string.IsNullOrEmpty(path))
            {
                var chars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();
                path = "[" + video.VideoId + "]" + string.Join("_", video.Title.Split(chars)) + ".mp4";
            }

            Console.WriteLine(path);

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
