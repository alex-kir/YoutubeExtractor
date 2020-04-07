using System;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeExtractor.App
{
    class Program
    {
        static void Main(string[] args)
        {
            //try
            //{
            AssemblySmartResolver.Install(typeof(Program).Assembly);
            Run(args);

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //    return -1;
            //}
            
        }

        private static void Run(string[] args)
        {
            switch (args.ElementAtOrDefault(0))
            {
                case "download" when
                    !string.IsNullOrWhiteSpace(args.ElementAtOrDefault(1)) &&
                    !string.IsNullOrWhiteSpace(args.ElementAtOrDefault(2)):
                    RunDownload(args[1], args[2], args.ElementAtOrDefault(3), args.ElementAtOrDefault(4), args.ElementAtOrDefault(5));
                    break;
                case "info" when
                    !string.IsNullOrWhiteSpace(args.ElementAtOrDefault(1)):
                    RunInfo(args[1]);
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
    }
}
