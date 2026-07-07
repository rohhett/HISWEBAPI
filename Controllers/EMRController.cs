using HISWEBAPI.Configuration;
using HISWEBAPI.DTO;
using HISWEBAPI.Repositories.Implementations;
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
    public class EMRController : ControllerBase
    {
        private readonly IEMRRepository _emrRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public EMRController(
            IEMRRepository emrRepository,
            IResponseMessageService messageService)
        {
            _emrRepository = emrRepository;
            _messageService = messageService;
        }

        [HttpPost("createUpdateAllergyMaster")]
        [Authorize]
        public IActionResult CreateUpdateAllergyMaster([FromBody] CreateUpdateAllergyMasterRequest request)
        {
            _log.Info($"CreateUpdateAllergyMaster called. AllergyId={request.AllergyId}, AllergyName={request.AllergyName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for allergy master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.CreateUpdateAllergyMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"AllergyMaster operation completed: {serviceResult.Message}");
            else
                _log.Warn($"AllergyMaster operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getAllergyMasterList")]
        [Authorize]
        public IActionResult GetAllergyMasterList([FromQuery] int? isActive = null, int? allergyTypeId=null)
        {
            _log.Info($"GetAllergyMasterList called. IsActive={isActive?.ToString() ?? "All"}");

            if (isActive.HasValue && isActive.Value != 0 && isActive.Value != 1)
            {
                _log.Warn($"Invalid IsActive parameter: {isActive.Value}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 (Inactive), 1 (Active), or null (All)",
                    errors = new { isActive }
                });
            }

            var serviceResult = _emrRepository.GetAllergyMasterList(isActive, allergyTypeId);

            if (serviceResult.Result)
                _log.Info($"AllergyMaster fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"AllergyMaster fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPatch("deleteAllergyMaster")]
        [Authorize]
        public IActionResult DeleteAllergyMaster([FromQuery] int allergyId)
        {
            _log.Info($"DeleteAllergyMaster called. AllergyId={allergyId}");

            if (allergyId <= 0)
            {
                _log.Warn("Invalid AllergyId provided for deletion.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "AllergyId must be greater than 0",
                    errors = new { allergyId }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.DeleteAllergyMaster(allergyId, globalValues);

            if (serviceResult.Result)
                _log.Info($"Allergy deleted successfully: {serviceResult.Message}");
            else
                _log.Warn($"Allergy deletion failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message
            });
        }

        [HttpGet("getSaltNameMasterList")]
        [Authorize]
        public IActionResult GetSaltNameMasterList()
        {
            _log.Info("GetSaltNameMasterList called.");

            var serviceResult = _emrRepository.GetSaltNameMasterList();

            if (serviceResult.Result)
                _log.Info($"Salt names fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No salt names found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("createUpdatePatientAllergyDetails")]
        [Authorize]
        public IActionResult CreateUpdatePatientAllergyDetails([FromBody] CreateUpdatePatientAllergyDetailsRequest request)
        {
            _log.Info($"CreateUpdatePatientAllergyDetails called. Id={request.Id}, PatientId={request.PatientId}, AllergyId={request.AllergyId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for patient allergy details insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.NotKnownAllergy != 0 && request.NotKnownAllergy != 1)
            {
                _log.Warn("Invalid NotKnownAllergy value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "NotKnownAllergy must be 0 or 1",
                    errors = new { notKnownAllergy = request.NotKnownAllergy }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.CreateUpdatePatientAllergyDetails(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"PatientAllergyDetails operation completed: {serviceResult.Message}");
            else
                _log.Warn($"PatientAllergyDetails operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientAllergyDetailList")]
        [Authorize]
        public IActionResult GetPatientAllergyDetailList([FromQuery] int patientId)
        {
            _log.Info($"GetPatientAllergyDetailList called. PatientId={patientId}");

            if (patientId <= 0)
            {
                _log.Warn("Invalid PatientId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PatientId must be greater than 0",
                    errors = new { patientId }
                });
            }

            var serviceResult = _emrRepository.GetPatientAllergyDetailList(patientId);

            if (serviceResult.Result)
                _log.Info($"PatientAllergyDetails fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"PatientAllergyDetails fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPatch("deletePatientAllergyDetails")]
        [Authorize]
        public IActionResult DeletePatientAllergyDetails([FromBody] DeletePatientAllergyDetailsRequest request)
        {
            _log.Info($"DeletePatientAllergyDetails called. Id={request?.Id}, PatientId={request?.PatientId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for delete patient allergy details.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.DeletePatientAllergyDetails(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"PatientAllergyDetails deleted successfully: {serviceResult.Message}");
            else
                _log.Warn($"PatientAllergyDetails delete failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("createUpdateDiagnosisMaster")]
        [Authorize]
        public IActionResult CreateUpdateDiagnosisMaster([FromBody] CreateUpdateDiagnosisMasterRequest request)
        {
            _log.Info($"CreateUpdateDiagnosisMaster called. DiagnosisId={request.DiagnosisId}, DiagnosisName={request.DiagnosisName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for diagnosis master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.CreateUpdateDiagnosisMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"DiagnosisMaster operation completed: {serviceResult.Message}");
            else
                _log.Warn($"DiagnosisMaster operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getDiagnosisMasterList")]
        [Authorize]
        public IActionResult GetDiagnosisMasterList([FromQuery] int? isActive = null)
        {
            _log.Info($"GetDiagnosisMasterList called. IsActive={isActive?.ToString() ?? "All"}");

            if (isActive.HasValue && isActive.Value != 0 && isActive.Value != 1)
            {
                _log.Warn($"Invalid IsActive parameter: {isActive.Value}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 (Inactive), 1 (Active), or null (All)",
                    errors = new { isActive }
                });
            }

            var serviceResult = _emrRepository.GetDiagnosisMasterList(isActive);

            if (serviceResult.Result)
                _log.Info($"DiagnosisMaster fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"DiagnosisMaster fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("createUpdateProcedureMaster")]
        [Authorize]
        public IActionResult CreateUpdateProcedureMaster([FromBody] CreateUpdateProcedureMasterRequest request)
        {
            _log.Info($"CreateUpdateProcedureMaster called. ProcedureId={request.ProcedureId}, ProcedureName={request.ProcedureName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for procedure master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.CreateUpdateProcedureMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"ProcedureMaster operation completed: {serviceResult.Message}");
            else
                _log.Warn($"ProcedureMaster operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getProcedureMasterList")]
        [Authorize]
        public IActionResult GetProcedureMasterList([FromQuery] int? isActive = null)
        {
            _log.Info($"GetProcedureMasterList called. IsActive={isActive?.ToString() ?? "All"}");

            if (isActive.HasValue && isActive.Value != 0 && isActive.Value != 1)
            {
                _log.Warn($"Invalid IsActive parameter: {isActive.Value}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 (Inactive), 1 (Active), or null (All)",
                    errors = new { isActive }
                });
            }

            var serviceResult = _emrRepository.GetProcedureMasterList(isActive);

            if (serviceResult.Result)
                _log.Info($"ProcedureMaster fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"ProcedureMaster fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("createUpdateEMRSectionMaster")]
        [Authorize]
        public IActionResult CreateUpdateEMRSectionMaster([FromBody] CreateUpdateEMRSectionMasterRequest request)
        {
            _log.Info($"CreateUpdateEMRSectionMaster called. SectionId={request.SectionId}, SectionName={request.SectionName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for EMR section master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.HeaderMappings != null && request.HeaderMappings.Any())
            {
                var invalidRows = request.HeaderMappings
                    .Where(x => x.HeaderId <= 0)
                    .ToList();

                if (invalidRows.Any())
                {
                    _log.Warn($"{invalidRows.Count} row(s) have invalid HeaderId.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "Every header mapping row must have HeaderId greater than 0",
                        errors = new { invalidHeaderIds = invalidRows.Select(x => x.HeaderId).ToList() }
                    });
                }
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.CreateUpdateEMRSectionMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"EMRSectionMaster operation completed: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionMaster operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getEMRSectionMaster")]
        [Authorize]
        public IActionResult GetEMRSectionMaster([FromQuery] int? isActive = null)
        {
            _log.Info($"GetEMRSectionMaster called. IsActive={isActive?.ToString() ?? "All"}");

            if (isActive.HasValue && isActive.Value != 0 && isActive.Value != 1)
            {
                _log.Warn($"Invalid IsActive parameter: {isActive.Value}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 (Inactive), 1 (Active), or null (All)",
                    errors = new { isActive }
                });
            }

            var serviceResult = _emrRepository.GetEMRSectionMaster(isActive);

            if (serviceResult.Result)
                _log.Info($"EMRSectionMaster fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionMaster fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getEMRSectionHeaderMapping")]
        [Authorize]
        public IActionResult GetEMRSectionHeaderMapping([FromQuery] int sectionId)
        {
            _log.Info($"GetEMRSectionHeaderMapping called. SectionId={sectionId}");

            if (sectionId <= 0)
            {
                _log.Warn("Invalid SectionId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "SectionId must be greater than 0",
                    errors = new { sectionId }
                });
            }

            var serviceResult = _emrRepository.GetEMRSectionHeaderMapping(sectionId);

            if (serviceResult.Result)
                _log.Info($"EMRSectionHeaderMapping fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionHeaderMapping fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getEMRSectionDepartmentMapping")]
        [Authorize]
        public IActionResult GetEMRSectionDepartmentMapping(
[FromQuery] int typeId,
[FromQuery] int relatedToId)
        {
            _log.Info($"GetEMRSectionDepartmentMapping called. TypeId={typeId}, RelatedToId={relatedToId}");

            if (typeId <= 0)
            {
                _log.Warn("Invalid TypeId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "TypeId must be greater than 0",
                    errors = new { typeId }
                });
            }

            if (relatedToId <= 0)
            {
                _log.Warn("Invalid RelatedToId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "RelatedToId must be greater than 0",
                    errors = new { relatedToId }
                });
            }

            var serviceResult = _emrRepository.GetEMRSectionDepartmentMapping(typeId, relatedToId);

            if (serviceResult.Result)
                _log.Info($"EMRSectionDepartmentMapping fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionDepartmentMapping fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("saveEMRSectionDepartmentMapping")]
        [Authorize]
        public IActionResult SaveEMRSectionDepartmentMapping([FromBody] SaveEMRSectionDepartmentMappingRequest request)
        {
            _log.Info($"SaveEMRSectionDepartmentMapping called. TypeId={request.TypeId}, RelatedToId={request.RelatedToId}, Items={request.HeaderMappingData?.Count ?? 0}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveEMRSectionDepartmentMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.HeaderMappingData != null && request.HeaderMappingData.Any())
            {
                var invalidRows = request.HeaderMappingData
                    .Where(x => x.SectionId <= 0)
                    .ToList();

                if (invalidRows.Any())
                {
                    _log.Warn($"{invalidRows.Count} row(s) have invalid TypeId, SectionId, or RelatedToId.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "Every mapping row must have SectionId > 0 "
                    });
                }
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.SaveEMRSectionDepartmentMapping(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"SaveEMRSectionDepartmentMapping completed: {serviceResult.Message}");
            else
                _log.Warn($"SaveEMRSectionDepartmentMapping failed: {serviceResult.Message}");

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