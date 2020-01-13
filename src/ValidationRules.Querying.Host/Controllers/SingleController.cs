using System.Web.Http;
using NuClear.ValidationRules.Hosting.Common;
using NuClear.ValidationRules.Querying.Host.CheckModes;
using NuClear.ValidationRules.Querying.Host.Composition;
using NuClear.ValidationRules.SingleCheck;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Controllers
{
    [RoutePrefix("api/Single")]
    public class SingleController : ApiController
    {
        private readonly ValidationResultFactory _factory;
        private readonly PipelineFactory _pipelineFactory;
        private readonly VersionHelper _versionHelper;
        private readonly ICheckModeDescriptor _checkModeDescriptor;

        public SingleController(ValidationResultFactory factory, PipelineFactory pipelineFactory, CheckModeDescriptorFactory descriptorFactory, VersionHelper versionHelper)
        {
            _factory = factory;
            _pipelineFactory = pipelineFactory;
            _versionHelper = versionHelper;
            _checkModeDescriptor = descriptorFactory.GetDescriptorFor(CheckMode.Single);
        }

        [Route(""), HttpPost]
        public IHttpActionResult Post([FromBody] ApiRequest request)
        {
            var pipeline = _pipelineFactory.Create(_versionHelper.Version);
            var validationResults = pipeline.Execute(request.OrderId, _checkModeDescriptor);
            var result = _factory.GetValidationResult(validationResults, _checkModeDescriptor);
            return Ok(result);
        }

        public class ApiRequest
        {
            public long OrderId { get; set; }
        }
    }
}
