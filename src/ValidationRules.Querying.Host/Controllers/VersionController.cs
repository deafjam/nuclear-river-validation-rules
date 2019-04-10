using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Web.Http;

namespace NuClear.ValidationRules.Querying.Host.Controllers
{
    [RoutePrefix("api/version")]
    public sealed class VersionController : ApiController
    {
        private static readonly string Version = "0.0.0";

        static VersionController()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");
            if (!File.Exists(path))
            {
                return;
            }

            // не добавляем глухой try-catch, чтобы при ошибках десериализации возвращать 500
            var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
            Version = (string)jObject["version"];
        }

        [Route(""), HttpGet]
        public IHttpActionResult Get()
        {
            return Ok(Version);
        }
    }
}