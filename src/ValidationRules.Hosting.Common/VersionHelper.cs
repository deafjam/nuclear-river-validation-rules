using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NuClear.ValidationRules.Hosting.Common
{
    public sealed class VersionHelper
    {
        public string Version { get; } = "0.0.0";

        public VersionHelper()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");
            if (!File.Exists(path))
            {
                return;
            }

            var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
            Version = (string)jObject["version"];
        }
    }
}