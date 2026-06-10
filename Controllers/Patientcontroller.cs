using HISWEBAPI.Configuration;
using HISWEBAPI.DTO;
using HISWEBAPI.Repositories.Implementations;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PatientController(
            IPatientRepository patientRepository,
            IResponseMessageService messageService)
        {
            _patientRepository = patientRepository;
            _messageService = messageService;
        }

        [HttpPost("createUpdatePatientMaster")]
        [Authorize]
        public IActionResult CreateUpdatePatientMaster([FromForm] CreateUpdatePatientMasterRequest request)
        {
            _log.Info($"CreateUpdatePatientMaster called. PatientId={request.PatientId}, FirstName={request.FirstName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for patient insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate DOB format
            if (string.IsNullOrWhiteSpace(request.Dob))
            {
                _log.Warn("DOB is missing.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Date of birth is required",
                    errors = new { dob = "DOB cannot be empty" }
                });
            }

            // Validate BranchId
            if (request.BranchId <= 0)
            {
                _log.Warn("Invalid BranchId.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId must be greater than 0",
                    errors = new { branchId = request.BranchId }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _patientRepository.CreateUpdatePatientMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Patient operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Patient operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("uploadPatientDocument")]
        [Authorize]
        public IActionResult UploadPatientDocument([FromForm] UploadPatientDocumentRequest request)
        {
            _log.Info($"UploadPatientDocument called. PatientId={request.PatientId}, DocumentId={request.DocumentId}");

            if (!ModelState.IsValid)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            if (request.PatientId <= 0)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PatientId must be greater than 0",
                    errors = new { request.PatientId }
                });
            }

            if (request.DocumentId <= 0)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "DocumentId must be greater than 0",
                    errors = new { request.DocumentId }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _patientRepository.UploadPatientDocument(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Document uploaded successfully: {serviceResult.Message}");
            else
                _log.Warn($"Document upload failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientDocumentMapping")]
        [Authorize]
        public IActionResult GetPatientDocumentMapping([FromQuery] int patientId)
        {
            _log.Info($"GetPatientDocumentMapping called. PatientId={patientId}");

            if (patientId < 0)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PatientId must be greater than equal to 0",
                    errors = new { patientId }
                });
            }

            var serviceResult = _patientRepository.GetPatientDocumentMapping(patientId);

            if (serviceResult.Result)
                _log.Info($"Patient documents fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Patient documents fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }
        [HttpGet("getPatientMaster")]
        [Authorize]
        public IActionResult GetPatientMaster(
            [FromQuery] int? patientId = null,
            [FromQuery] string? uhid = null,
            [FromQuery] string? contactNumber = null,
            [FromQuery] int? branchId = null)
        {
            _log.Info($"GetPatientMaster called. PatientId={patientId?.ToString() ?? "All"}, Uhid={uhid ?? "All"}, ContactNumber={contactNumber ?? "All"}, BranchId={branchId?.ToString() ?? "All"}");

            // Validate patientId if provided
            if (patientId.HasValue && patientId.Value <= 0)
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

            // Validate branchId if provided
            if (branchId.HasValue && branchId.Value <= 0)
            {
                _log.Warn("Invalid BranchId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId must be greater than 0",
                    errors = new { branchId }
                });
            }

            var serviceResult = _patientRepository.GetPatientMaster(patientId, uhid, contactNumber, branchId);

            if (serviceResult.Result)
                _log.Info($"Patients fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No patients found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("searchPatientMaster")]
        [Authorize]
        public IActionResult SearchPatientMaster(
    [FromQuery] int? patientId = null,
    [FromQuery] string? uhid = null,
    [FromQuery] string? firstName = null,
    [FromQuery] string? middleName = null,
    [FromQuery] string? lastName = null,
    [FromQuery] string? relativeName = null,
    [FromQuery] string? dob = null,
    [FromQuery] string? contactNumber = null,
    [FromQuery] string? emergencyContactNumber = null,
    [FromQuery] string? address = null,
    [FromQuery] string? registrationDate = null,
    [FromQuery] int? ipdNo = null,
    [FromQuery] int? branchId = null)
        {
            _log.Info($"SearchPatientMaster called.");

            if (patientId.HasValue && patientId.Value <= 0)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new { result = false, messageType = alert.Type, message = "PatientId must be greater than 0", errors = new { patientId } });
            }

            if (branchId.HasValue && branchId.Value <= 0)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new { result = false, messageType = alert.Type, message = "BranchId must be greater than 0", errors = new { branchId } });
            }

            var serviceResult = _patientRepository.SearchPatientMaster(
                patientId, uhid, firstName, middleName, lastName,
                relativeName, dob, contactNumber, emergencyContactNumber,
                address, registrationDate, ipdNo, branchId);

            if (serviceResult.Result)
                _log.Info($"Patients fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No patients found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getServiceAllDetailsForOPDBilling")]
        [Authorize]
        public IActionResult GetServiceAllDetailsForOPDBilling(
           [FromQuery] int corporateId,
           [FromQuery] int doctorId,
           [FromQuery] int serviceItemId,
           [FromQuery] int categoryId,
           [FromQuery] int subCategoryId,
           [FromQuery] int subSubCategoryId,
           [FromQuery] int bedTypeId = 0)
        {
            _log.Info($"GetServiceAllDetailsForOPDBilling called. CorporateId={corporateId}, DoctorId={doctorId}, ServiceItemId={serviceItemId}, CategoryId={categoryId}, SubCategoryId={subCategoryId}, SubSubCategoryId={subSubCategoryId}, BedTypeId={bedTypeId}");

            // Validate all required params must be > 0
            var validationErrors = new Dictionary<string, int>();

            if (corporateId <= 0) validationErrors["corporateId"] = corporateId;
            if (doctorId <= 0) validationErrors["doctorId"] = doctorId;
            if (serviceItemId <= 0) validationErrors["serviceItemId"] = serviceItemId;
            if (categoryId <= 0) validationErrors["categoryId"] = categoryId;
            if (subCategoryId <= 0) validationErrors["subCategoryId"] = subCategoryId;
            if (subSubCategoryId <= 0) validationErrors["subSubCategoryId"] = subSubCategoryId;

            if (validationErrors.Any())
            {
                _log.Warn($"Invalid parameters: {string.Join(", ", validationErrors.Keys)}");
                var alertVal = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alertVal.Type,
                    message = $"The following parameters must be greater than 0: {string.Join(", ", validationErrors.Keys)}",
                    errors = validationErrors
                });
            }

            var serviceResult = _patientRepository.GetServiceAllDetailsForOPDBilling(
                corporateId, doctorId, serviceItemId, categoryId, subCategoryId, subSubCategoryId, bedTypeId);

            if (serviceResult.Result)
                _log.Info($"Service billing details fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Service billing details fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("saveOPDBilling")]
        [Authorize]
        public IActionResult SaveOPDBilling([FromBody] SaveOPDBillingRequest request)
        {
            _log.Info($"SaveOPDBilling called. PatientId={request?.VisitDetails?.PatientId}, BranchId={request?.VisitDetails?.BranchId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveOPDBilling.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Additional validation
            if (request.BillingItems == null || request.BillingItems.Count == 0)
            {
                _log.Warn("No billing items provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "At least one billing item is required",
                    errors = new[] { "BillingItems cannot be empty" }
                });
            }

            if (request.VisitDetails.PatientId <= 0)
            {
                _log.Warn("Invalid PatientId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PatientId must be greater than 0",
                    errors = new { patientId = request.VisitDetails.PatientId }
                });
            }

            if (request.VisitDetails.BranchId <= 0)
            {
                _log.Warn("Invalid BranchId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId must be greater than 0",
                    errors = new { branchId = request.VisitDetails.BranchId }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _patientRepository.SaveOPDBilling(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"SaveOPDBilling succeeded: {serviceResult.Message}");
            else
                _log.Warn($"SaveOPDBilling failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPackageAllDetails")]
        [Authorize]
        public IActionResult GetPackageAllDetails([FromQuery] int packageId)
        {
            _log.Info($"GetPackageAllDetails called. PackageId={packageId}");

            if (packageId <= 0)
            {
                _log.Warn("Invalid PackageId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PackageId must be greater than 0",
                    errors = new { packageId }
                });
            }

            var serviceResult = _patientRepository.GetPackageAllDetails(packageId);

            if (serviceResult.Result)
                _log.Info($"Package details fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Package details fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getReceiptDetailsByFTID")]
        [Authorize]
        public IActionResult GetReceiptDetailsByFTID(
    [FromQuery] int ftid,
    [FromQuery] int isReceipt=0,
    [FromQuery] int receiptId = 0)
        {
            _log.Info($"GetReceiptDetailsByFTID called. FTID={ftid}, IsReceipt={isReceipt}, ReceiptId={receiptId}");

            if (ftid <= 0)
            {
                _log.Warn("Invalid FTID provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "FTID must be greater than 0",
                    errors = new { ftid }
                });
            }

            if (isReceipt != 0 && isReceipt != 1)
            {
                _log.Warn($"Invalid isReceipt value: {isReceipt}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "isReceipt must be 0 or 1",
                    errors = new { isReceipt }
                });
            }

            if (receiptId < 0)
            {
                _log.Warn("Invalid ReceiptId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "ReceiptId must be greater than or equal to 0",
                    errors = new { receiptId }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _patientRepository.GetReceiptDetailsByFTID(ftid, isReceipt, receiptId, globalValues);

            if (serviceResult.Result)
                _log.Info($"Receipt details fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Receipt details fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getOPDReceiptList")]
        [Authorize]
        public IActionResult GetOPDReceiptList([FromQuery] long visitNo)
        {
            _log.Info($"GetOPDReceiptList called. VisitNo={visitNo}");

            if (visitNo <= 0)
            {
                _log.Warn("Invalid VisitNo provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "VisitNo must be greater than 0",
                    errors = new { visitNo }
                });
            }

            var serviceResult = _patientRepository.GetOPDReceiptList(visitNo);

            if (serviceResult.Result)
                _log.Info($"OPD receipt list fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"OPD receipt list fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getOPDCardDetails")]
        [Authorize]
        public IActionResult GetOPDCardDetails([FromQuery] long ftid)
        {
            _log.Info($"GetOPDCardDetails called. FTID={ftid}");

            if (ftid <= 0)
            {
                _log.Warn("Invalid FTID provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "FTID must be greater than 0",
                    errors = new { ftid }
                });
            }

            var serviceResult = _patientRepository.GetOPDCardDetails(ftid);

            if (serviceResult.Result)
                _log.Info($"OPD card details fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"OPD card details fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("findDuplicateService")]
        [Authorize]
        public IActionResult FindDuplicateService(
    [FromQuery] int serviceItemId,
    [FromQuery] int patientId)
        {
            _log.Info($"FindDuplicateService called. ServiceItemId={serviceItemId}, PatientId={patientId}");

            if (serviceItemId <= 0)
            {
                _log.Warn("Invalid ServiceItemId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "ServiceItemId must be greater than 0",
                    errors = new { serviceItemId }
                });
            }

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

            var serviceResult = _patientRepository.FindDuplicateService(serviceItemId, patientId);

            // Convert DataTable rows to list of dictionaries for raw JSON output
            object rawData = null;
            if (serviceResult.Result && serviceResult.Data != null)
            {
                rawData = serviceResult.Data.Rows
                    .Cast<DataRow>()
                    .Select(row => serviceResult.Data.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();
            }

            if (serviceResult.Result)
                _log.Info($"Duplicate service check completed: {serviceResult.Message}");
            else
                _log.Warn($"Duplicate service check result: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = rawData
            });
        }


        [HttpGet("getInvestigationObservationMappingDetails")]
        [Authorize]
        public IActionResult GetInvestigationObservationMappingDetails(
    [FromQuery] int investigationId,
    [FromQuery] int ageInDays,
    [FromQuery] string gender)
        {
            _log.Info($"GetInvestigationObservationMappingDetails called. InvestigationId={investigationId}, AgeInDays={ageInDays}, Gender={gender}");

            if (investigationId <= 0)
            {
                _log.Warn("Invalid InvestigationId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "InvestigationId must be greater than 0",
                    errors = new { investigationId }
                });
            }

            if (ageInDays < 0)
            {
                _log.Warn("Invalid AgeInDays provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "AgeInDays must be greater than or equal to 0",
                    errors = new { ageInDays }
                });
            }

            if (string.IsNullOrWhiteSpace(gender))
            {
                _log.Warn("Invalid Gender provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Gender is required",
                    errors = new { gender }
                });
            }

            if (gender.ToUpper() != "M" && gender.ToUpper() != "F" && gender.ToUpper() != "B")
            {
                _log.Warn($"Invalid Gender value provided: {gender}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Gender must be M, F or B",
                    errors = new { gender }
                });
            }

            var serviceResult = _patientRepository.GetInvestigationObservationMappingDetails(investigationId, ageInDays, gender);

            if (serviceResult.Result)
                _log.Info($"GetInvestigationObservationMappingDetails fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetInvestigationObservationMappingDetails failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getUserDiscountRights")]
        [Authorize]
        public IActionResult GetUserDiscountRights([FromQuery] int userId)
        {
            _log.Info($"GetUserDiscountRights called. UserId={userId}");

            if (userId <= 0)
            {
                _log.Warn("Invalid UserId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "UserId must be greater than 0",
                    errors = new { userId }
                });
            }

            var serviceResult = _patientRepository.GetUserDiscountRights(userId);

            if (serviceResult.Result)
                _log.Info($"GetUserDiscountRights fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetUserDiscountRights failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientPreviousDues")]
        [Authorize]
        public IActionResult GetPatientPreviousDues(
    [FromQuery] int branchId,
    [FromQuery] int patientId)
        {
            _log.Info($"GetPatientPreviousDues called. BranchId={branchId}, PatientId={patientId}");

            if (branchId <= 0)
            {
                _log.Warn("Invalid BranchId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId must be greater than 0",
                    errors = new { branchId }
                });
            }

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

            var serviceResult = _patientRepository.GetPatientPreviousDues(branchId, patientId);

            if (serviceResult.Result)
                _log.Info($"GetPatientPreviousDues fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetPatientPreviousDues failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientLastConsultationDetail")]
        [Authorize]
        public IActionResult GetPatientLastConsultationDetail([FromQuery] int patientId)
        {
            _log.Info($"GetPatientLastConsultationDetail called. PatientId={patientId}");

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

            var serviceResult = _patientRepository.GetPatientLastConsultationDetail(patientId);

            if (serviceResult.Result)
                _log.Info($"GetPatientLastConsultationDetail fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetPatientLastConsultationDetail failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getServiceItemDetailsByVisitId")]
        [Authorize]
        public IActionResult GetServiceItemDetailsByVisitId([FromQuery] int visitId)
        {
            _log.Info($"GetServiceItemDetailsByVisitId called. VisitId={visitId}");

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

            var serviceResult = _patientRepository.GetServiceItemDetailsByVisitId(visitId);

            if (serviceResult.Result)
                _log.Info($"GetServiceItemDetailsByVisitId fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetServiceItemDetailsByVisitId failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientBalanceAmountOPD")]
        [Authorize]
        public IActionResult GetPatientBalanceAmountOPD([FromQuery] string uhid)
        {
            _log.Info($"GetPatientBalanceAmountOPD called. UHID={uhid}");

            if (string.IsNullOrWhiteSpace(uhid))
            {
                _log.Warn("Invalid UHID provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "UHID is required",
                    errors = new { uhid }
                });
            }

            var serviceResult = _patientRepository.GetPatientBalanceAmountOPD(uhid);

            if (serviceResult.Result)
                _log.Info($"GetPatientBalanceAmountOPD fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetPatientBalanceAmountOPD failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientBalanceAmountIPD")]
        [Authorize]
        public IActionResult GetPatientBalanceAmountIPD([FromQuery] string uhid)
        {
            _log.Info($"GetPatientBalanceAmountIPD called. UHID={uhid}");

            if (string.IsNullOrWhiteSpace(uhid))
            {
                _log.Warn("Invalid UHID provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "UHID is required",
                    errors = new { uhid }
                });
            }

            var serviceResult = _patientRepository.GetPatientBalanceAmountIPD(uhid);

            if (serviceResult.Result)
                _log.Info($"GetPatientBalanceAmountIPD fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetPatientBalanceAmountIPD failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientBalanceAmountPharmacy")]
        [Authorize]
        public IActionResult GetPatientBalanceAmountPharmacy([FromQuery] string uhid)
        {
            _log.Info($"GetPatientBalanceAmountPharmacy called. UHID={uhid}");

            if (string.IsNullOrWhiteSpace(uhid))
            {
                _log.Warn("Invalid UHID provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "UHID is required",
                    errors = new { uhid }
                });
            }

            var serviceResult = _patientRepository.GetPatientBalanceAmountPharmacy(uhid);

            if (serviceResult.Result)
                _log.Info($"GetPatientBalanceAmountPharmacy fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"GetPatientBalanceAmountPharmacy failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("searchPatientForConsultation")]
        [Authorize]
        public IActionResult SearchPatientForConsultation(
           [FromQuery] int branchId,
           [FromQuery] int typeId,
           [FromQuery] string fromDate,
           [FromQuery] string toDate,
           [FromQuery] string uhid = null,
           [FromQuery] int appNo = 0,
           [FromQuery] int doctorId = 0,
           [FromQuery] int doctorDepartmentId = 0,
           [FromQuery] int dateTypeId = 0,
           [FromQuery] int statusId = 0,
           [FromQuery] int bedTypeId = 0)
        {
            _log.Info($"SearchPatientForConsultation called. BranchId={branchId}, TypeId={typeId}, " +
                      $"FromDate={fromDate}, ToDate={toDate}");

            // Manual validation
            if (branchId <= 0)
            {
                _log.Warn("Invalid BranchId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId must be greater than 0",
                    errors = new { branchId }
                });
            }

            if (typeId != 1 && typeId != 2)
            {
                _log.Warn($"Invalid TypeId: {typeId}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "TypeId must be 1 (OPD) or 2 (IPD)",
                    errors = new { typeId }
                });
            }

            if (string.IsNullOrWhiteSpace(fromDate))
            {
                _log.Warn("FromDate is missing.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "FromDate is required",
                    errors = new { fromDate }
                });
            }

            if (string.IsNullOrWhiteSpace(toDate))
            {
                _log.Warn("ToDate is missing.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "ToDate is required",
                    errors = new { toDate }
                });
            }

            var request = new SearchPatientForConsultationRequest
            {
                BranchId = branchId,
                TypeId = typeId,
                FromDate = fromDate,
                ToDate = toDate,
                Uhid = uhid,
                AppNo = appNo,
                DoctorId = doctorId,
                DoctorDepartmentId = doctorDepartmentId,
                DateTypeId = dateTypeId,
                StatusId = statusId,
                BedTypeId = bedTypeId
            };

            var serviceResult = _patientRepository.SearchPatientForConsultation(request);

            if (serviceResult.Result)
                _log.Info($"SearchPatientForConsultation completed: {serviceResult.Message}");
            else
                _log.Warn($"SearchPatientForConsultation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getPatientVital")]
        [Authorize]
        public IActionResult getPatientVital([FromQuery] int patientId)
        {
            _log.Info($"getPatientVital called. PatientId={patientId}");

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

            var serviceResult = _patientRepository.GetPatientVital(patientId);

            if (serviceResult.Result)
                _log.Info($"Patient vitals fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Patient vitals fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("savePatientVital")]
        [Authorize]
        public IActionResult savePatientVital([FromBody] SavePatientVitalRequest request)
        {
            _log.Info($"savePatientVital called. VisitId={request.VisitId}, PatientId={request.PatientId}, VitalId={request.VitalId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for savePatientVital.");
                var modelAlert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = modelAlert.Type,
                    message = modelAlert.Message,
                    errors = ModelState
                });
            }

            if (request.VisitId <= 0)
            {
                _log.Warn("Invalid VisitId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "VisitId must be greater than 0",
                    errors = new { request.VisitId }
                });
            }

            if (request.PatientId <= 0)
            {
                _log.Warn("Invalid PatientId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PatientId must be greater than 0",
                    errors = new { request.PatientId }
                });
            }

            if (request.VitalId <= 0)
            {
                _log.Warn("Invalid VitalId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "VitalId must be greater than 0",
                    errors = new { request.VitalId }
                });
            }

            if (string.IsNullOrWhiteSpace(request.VitalValue))
            {
                _log.Warn("VitalValue is missing.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "VitalValue is required",
                    errors = new { request.VitalValue }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _patientRepository.SavePatientVital(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Patient vital saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"Patient vital save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpGet("getPatientObservationResultsTrend")]
        [Authorize]
        public IActionResult GetPatientObservationResultsTrend(
    [FromQuery] int patientId,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
        {
            _log.Info($"GetPatientObservationResultsTrend called. PatientId={patientId}, PageNumber={pageNumber}, PageSize={pageSize}");

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

            if (pageNumber <= 0)
            {
                _log.Warn("Invalid PageNumber provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PageNumber must be greater than 0",
                    errors = new { pageNumber }
                });
            }

            if (pageSize <= 0)
            {
                _log.Warn("Invalid PageSize provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "PageSize must be greater than 0",
                    errors = new { pageSize }
                });
            }

            var serviceResult = _patientRepository.GetPatientObservationResultsTrend(patientId, pageNumber, pageSize);

            if (serviceResult.Result)
                _log.Info($"Observation trend fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Observation trend fetch failed: {serviceResult.Message}");

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