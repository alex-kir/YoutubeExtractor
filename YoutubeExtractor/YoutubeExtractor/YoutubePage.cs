using Newtonsoft.Json.Linq;

namespace YoutubeExtractor
{
    class YoutubePage
    {
        public string Source;

        public JObject PlayerConfigJson;
        internal JObject ConfigJson2;
    }
}