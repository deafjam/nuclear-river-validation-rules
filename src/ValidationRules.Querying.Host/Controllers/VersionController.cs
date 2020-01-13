using System.Web.Http;
using NuClear.ValidationRules.Hosting.Common;

namespace NuClear.ValidationRules.Querying.Host.Controllers
{
    [RoutePrefix("api/version")]
    public sealed class VersionController : ApiController
    {
        private readonly VersionHelper _versionHelper;

        public VersionController(VersionHelper versionHelper)
        {
            _versionHelper = versionHelper;
        }

        [Route(""), HttpGet]
        public IHttpActionResult Get()
        {
            return Ok(_versionHelper.Version);
        }
    }
}