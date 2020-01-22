using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

using NuClear.ValidationRules.Querying.Host.Composition;
using NuClear.ValidationRules.Querying.Host.DataAccess;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Controllers
{
    [RoutePrefix("api/Manual")]
    public class ManualController : ApiController
    {
        private readonly VersioningService _versioningService;
        private readonly ValidationResultRepositiory _repository;
        private readonly ValidationResultFactory _factory;

        public ManualController(VersioningService versioningService, ValidationResultRepositiory repository, ValidationResultFactory factory)
        {
            _versioningService = versioningService;
            _repository = repository;
            _factory = factory;
        }

        [Route("{stateToken:guid}"), HttpPost]
        public async Task<IHttpActionResult> Post([FromBody]ApiRequest request, [FromUri]Guid stateToken)
        {
            var versionId = await _versioningService.WaitForVersion(stateToken);

            var validationResults = _repository.GetResults(CheckMode.Manual, versionId, request.OrderIds, request.ProjectId, request.ReleaseDate, request.ReleaseDate.AddMonths(1));
            var result = _factory.GetValidationResult(CheckMode.Manual, validationResults);
            return Ok(result);
        }

        [Route(""), HttpPost]
        public IHttpActionResult Post([FromBody]ApiRequest request)
        {
            var versionId = _versioningService.GetLatestVersion();
            var validationResults = _repository.GetResults(CheckMode.Manual, versionId, request.OrderIds, request.ProjectId, request.ReleaseDate, request.ReleaseDate.AddMonths(1));
            var result = _factory.GetValidationResult(CheckMode.Manual, validationResults);
            return Ok(result);
        }

        public class ApiRequest
        {
            public IReadOnlyCollection<long> OrderIds { get; set; }
            public long? ProjectId { get; set; }
            public DateTime ReleaseDate { get; set; }
        }
    }
}
