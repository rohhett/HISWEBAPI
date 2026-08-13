using HISWEBAPI.Configuration;
using HISWEBAPI.DTO;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IPDController : ControllerBase
    {
        private readonly IIPDRepository _ipdRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IPDController(
            IIPDRepository ipdRepository,
            IResponseMessageService messageService)
        {
            _ipdRepository = ipdRepository;
            _messageService = messageService;
        }

        [HttpGet("getIPDPatientBedHistory")]
        [Authorize]
        public IActionResult GetIPDPatientBedHistory([FromQuery] int visitId)
        {
            _log.Info($"GetIPDPatientBedHistory called. VisitId={visitId}");

            if (visitId <= 0)
            {
                _log.Warn("Invalid VisitId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "VisitId must be greater than 0",
                    errors = new { visitId }
                });
            }

            var serviceResult = _ipdRepository.GetIPDPatientBedHistory(visitId);

            if (serviceResult.Result)
                _log.Info($"IPD bed history fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"IPD bed history fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPatch("transferIPDPatientBed")]
        [Authorize]
        public IActionResult TransferIPDPatientBed([FromBody] TransferIPDPatientBedRequest request)
        {
            _log.Info($"TransferIPDPatientBed called. VisitId={request?.VisitId}, CurrentBedId={request?.CurrentBedId}, NewBedId={request?.NewBedId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for TransferIPDPatientBed.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.NewBedId == request.CurrentBedId)
            {
                _log.Warn("NewBedId and CurrentBedId are the same.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "NewBedId must be different from CurrentBedId",
                    errors = new { request.NewBedId, request.CurrentBedId }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _ipdRepository.TransferIPDPatientBed(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"IPD patient bed transferred successfully: {serviceResult.Message}");
            else
                _log.Warn($"IPD patient bed transfer failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }
    }
}