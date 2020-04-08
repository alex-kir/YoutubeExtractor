using System;

namespace Services
{
    class AppOptionsAttribute : Attribute
    {
        public string Description = "";

        public string[] FullKeys = Array.Empty<String>();
        public string[] ShortKeys = Array.Empty<String>();
    }
}
