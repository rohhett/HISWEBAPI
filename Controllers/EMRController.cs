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

        [HttpPost("saveEMRSectionScoreFormula")]
        [Authorize]
        public IActionResult SaveEMRSectionScoreFormula([FromBody] SaveEMRSectionScoreFormulaRequest request)
        {
            _log.Info($"SaveEMRSectionScoreFormula called. SectionId={request.SectionId}, Items={request.FormulaItems?.Count ?? 0}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveEMRSectionScoreFormula.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.FormulaItems != null && request.FormulaItems.Any())
            {
                var invalidRows = request.FormulaItems
                    .Where(x => x.HeaderId < 0)
                    .ToList();

                if (invalidRows.Any())
                {
                    _log.Warn($"{invalidRows.Count} row(s) have invalid HeaderId.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "Every formula row must have HeaderId greater than equal to 0",
                        errors = new { invalidHeaderIds = invalidRows.Select(x => x.HeaderId).ToList() }
                    });
                }
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.SaveEMRSectionScoreFormula(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"SaveEMRSectionScoreFormula completed: {serviceResult.Message}");
            else
                _log.Warn($"SaveEMRSectionScoreFormula failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getEMRSectionScoreFormula")]
        [Authorize]
        public IActionResult GetEMRSectionScoreFormula([FromQuery] int sectionId)
        {
            _log.Info($"GetEMRSectionScoreFormula called. SectionId={sectionId}");

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

            var serviceResult = _emrRepository.GetEMRSectionScoreFormula(sectionId);

            if (serviceResult.Result)
                _log.Info($"EMRSectionScoreFormula fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionScoreFormula fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("saveEMRSectionAttributeCondition")]
        [Authorize]
        public IActionResult SaveEMRSectionAttributeCondition(
    [FromBody] SaveEMRSectionAttributeConditionRequest request)
        {
            _log.Info($"SaveEMRSectionAttributeCondition called. SectionId={request.SectionId}, Groups={request.AttributeConditions?.Count ?? 0}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveEMRSectionAttributeCondition.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.AttributeConditions != null && request.AttributeConditions.Any())
            {
                // Validate each group has at least one condition
                var emptyGroups = request.AttributeConditions
                    .Where(g => g.Conditions == null || !g.Conditions.Any())
                    .Select(g => g.TargetHeaderId)
                    .ToList();

                if (emptyGroups.Any())
                {
                    _log.Warn($"Groups with no conditions found. TargetHeaderIds={string.Join(",", emptyGroups)}");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "Each attribute condition group must have at least one condition",
                        errors = new { emptyGroupTargetHeaderIds = emptyGroups }
                    });
                }

                // Validate all HeaderIds in conditions
                var invalidConditions = request.AttributeConditions
                    .SelectMany(g => g.Conditions)
                    .Where(c => c.HeaderId <= 0)
                    .Select(c => c.HeaderId)
                    .ToList();

                if (invalidConditions.Any())
                {
                    _log.Warn($"{invalidConditions.Count} condition(s) have invalid HeaderId.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "Every condition must have HeaderId greater than 0",
                        errors = new { invalidHeaderIds = invalidConditions }
                    });
                }

                // Validate Operator is not empty
                var missingOperators = request.AttributeConditions
                    .SelectMany(g => g.Conditions)
                    .Where(c => string.IsNullOrWhiteSpace(c.Operator))
                    .ToList();

                if (missingOperators.Any())
                {
                    _log.Warn("One or more conditions are missing Operator.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "Operator is required for every condition"
                    });
                }
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.SaveEMRSectionAttributeCondition(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"SaveEMRSectionAttributeCondition completed: {serviceResult.Message}");
            else
                _log.Warn($"SaveEMRSectionAttributeCondition failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getEMRSectionAttributeCondition")]
        [Authorize]
        public IActionResult GetEMRSectionAttributeCondition([FromQuery] int sectionId)
        {
            _log.Info($"GetEMRSectionAttributeCondition called. SectionId={sectionId}");

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

            var serviceResult = _emrRepository.GetEMRSectionAttributeCondition(sectionId);

            if (serviceResult.Result)
                _log.Info($"EMRSectionAttributeCondition fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionAttributeCondition fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPatch("deleteEMRSectionAttributeCondition")]
        [Authorize]
        public IActionResult DeleteEMRSectionAttributeCondition([FromQuery] int id)
        {
            _log.Info($"DeleteEMRSectionAttributeCondition called. Id={id}");

            if (id <= 0)
            {
                _log.Warn("Invalid Id provided for attribute condition deletion.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Id must be greater than 0",
                    errors = new { id }
                });
            }

            var serviceResult = _emrRepository.DeleteEMRSectionAttributeCondition(id);

            if (serviceResult.Result)
                _log.Info($"EMRSectionAttributeCondition deleted successfully: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionAttributeCondition deletion failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getEMRHeaderQueryResult")]
        [Authorize]
        public IActionResult GetEMRHeaderQueryResult([FromQuery] int headerId)
        {
            _log.Info($"GetEMRHeaderQueryResult called. HeaderId={headerId}");

            if (headerId <= 0)
            {
                _log.Warn("Invalid HeaderId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "HeaderId must be greater than 0",
                    errors = new { headerId }
                });
            }

            var serviceResult = _emrRepository.GetEMRHeaderQueryResult(headerId);

            if (serviceResult.Result)
                _log.Info($"EMRSectionHeaderDoctorOptions fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"EMRSectionHeaderDoctorOptions fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("saveDoctorFavouriteEMRSections")]
        [Authorize]
        public IActionResult SaveDoctorFavouriteEMRSections([FromBody] SaveDoctorFavouriteEMRSectionsRequest request)
        {
            _log.Info($"SaveDoctorFavouriteEMRSections called. DoctorId={request?.DoctorId}, SectionCount={request?.SectionIds?.Count ?? 0}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveDoctorFavouriteEMRSections.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.SectionIds != null && request.SectionIds.Any(id => id <= 0))
            {
                _log.Warn("Invalid SectionId(s) provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All SectionIds must be greater than 0",
                    errors = new { invalidSectionIds = request.SectionIds.Where(id => id <= 0).ToList() }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.SaveDoctorFavouriteEMRSections(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"SaveDoctorFavouriteEMRSections completed: {serviceResult.Message}");
            else
                _log.Warn($"SaveDoctorFavouriteEMRSections failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getDoctorFavouriteEMRSections")]
        [Authorize]
        public IActionResult GetDoctorFavouriteEMRSections([FromQuery] int doctorId)
        {
            _log.Info($"GetDoctorFavouriteEMRSections called. DoctorId={doctorId}");

            if (doctorId <= 0)
            {
                _log.Warn("Invalid DoctorId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "DoctorId must be greater than 0",
                    errors = new { doctorId }
                });
            }

            var serviceResult = _emrRepository.GetDoctorFavouriteEMRSections(doctorId);

            if (serviceResult.Result)
                _log.Info($"DoctorFavouriteEMRSections fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"DoctorFavouriteEMRSections fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("createUpdateChiefComplaintMaster")]
        [Authorize]
        public IActionResult CreateUpdateChiefComplaintMaster([FromBody] CreateUpdateChiefComplaintMasterRequest request)
        {
            _log.Info($"CreateUpdateChiefComplaintMaster called. ComplaintId={request.ComplaintId}, ComplaintName={request.ComplaintName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for chief complaint insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.IsActive != 0 && request.IsActive != 1)
            {
                _log.Warn("Invalid IsActive value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 or 1",
                    errors = new { isActive = request.IsActive }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.CreateUpdateChiefComplaintMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"ChiefComplaint operation completed: {serviceResult.Message}");
            else
                _log.Warn($"ChiefComplaint operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getChiefComplaintMasterList")]
        [Authorize]
        public IActionResult GetChiefComplaintMasterList([FromQuery] int? isActive = null)
        {
            _log.Info($"GetChiefComplaintMasterList called. IsActive={isActive?.ToString() ?? "All"}");

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

            var serviceResult = _emrRepository.GetChiefComplaintMasterList(isActive);

            if (serviceResult.Result)
                _log.Info($"Chief complaints fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No chief complaints found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("saveDoctorFavouriteTableEntry")]
        [Authorize]
        public IActionResult SaveDoctorFavouriteTableEntry([FromBody] SaveDoctorFavouriteTableEntryRequest request)
        {
            _log.Info($"SaveDoctorFavouriteTableEntry called. DoctorId={request?.DoctorId}, EntityId={request?.EntityId}, RecordId={request?.RecordId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveDoctorFavouriteTableEntry.");
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
            var serviceResult = _emrRepository.SaveDoctorFavouriteTableEntry(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"SaveDoctorFavouriteTableEntry succeeded: {serviceResult.Message}");
            else
                _log.Warn($"SaveDoctorFavouriteTableEntry failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getDoctorFavouriteTableEntries")]
        [Authorize]
        public IActionResult GetDoctorFavouriteTableEntries(
            [FromQuery] int doctorId,
            [FromQuery] int entityId = 0,
            [FromQuery] int recordId = 0)
        {
            _log.Info($"GetDoctorFavouriteTableEntries called. DoctorId={doctorId}, EntityId={entityId}, RecordId={recordId}");

            if (doctorId <= 0)
            {
                _log.Warn("Invalid DoctorId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "DoctorId must be greater than 0",
                    errors = new { doctorId }
                });
            }

            var serviceResult = _emrRepository.GetDoctorFavouriteTableEntries(doctorId, entityId, recordId);

            if (serviceResult.Result)
                _log.Info($"GetDoctorFavouriteTableEntries fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetDoctorFavouriteTableEntries failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPatch("deleteDoctorFavouriteTableEntry")]
        [Authorize]
        public IActionResult DeleteDoctorFavouriteTableEntry([FromQuery] int id)
        {
            _log.Info($"DeleteDoctorFavouriteTableEntry called. Id={id}");

            if (id <= 0)
            {
                _log.Warn("Invalid Id provided for favourite entry deletion.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Id must be greater than 0",
                    errors = new { id }
                });
            }

            var serviceResult = _emrRepository.DeleteDoctorFavouriteTableEntry(id);

            if (serviceResult.Result)
                _log.Info($"Doctor favourite entry deleted successfully: {serviceResult.Message}");
            else
                _log.Warn($"Doctor favourite entry deletion failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPatch("deleteRecordByTableName")]
        [Authorize]
        public IActionResult DeleteRecordByTableName([FromQuery] int id, [FromQuery] string tableName)
        {
            _log.Info($"DeleteRecordByTableName called. Id={id}, TableName={tableName}");

            if (id <= 0)
            {
                _log.Warn("Invalid Id provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Id must be greater than 0",
                    errors = new { id }
                });
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                _log.Warn("TableName is missing or empty.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "TableName is required",
                    errors = new { tableName }
                });
            }

            var validTableNames = new[] { "ChiefComplaintMaster", "AllergyMaster" };
            if (!validTableNames.Contains(tableName, StringComparer.OrdinalIgnoreCase))
            {
                _log.Warn($"Invalid TableName provided: {tableName}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = $"TableName must be one of: {string.Join(", ", validTableNames)}",
                    errors = new { tableName }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _emrRepository.DeleteRecordByTableName(id, tableName, globalValues);

            if (serviceResult.Result)
                _log.Info($"DeleteRecordByTableName succeeded: {serviceResult.Message}");
            else
                _log.Warn($"DeleteRecordByTableName failed: {serviceResult.Message}");

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