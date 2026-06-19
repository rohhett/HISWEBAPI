using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Domain;
using HISWEBAPI.DTO;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using HISWEBAPI.Utilities;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace HISWEBAPI.Repositories.Implementations
{
    public class PatientRepository : IPatientRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PatientRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService,
            IDistributedCache distributedCache,
            IConfiguration configuration)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
            _distributedCache = distributedCache;
            _configuration = configuration;
        }

        private const string CACHE_KEY_PatientMaster_All = "_PatientMaster_All";
        private const string CACHE_KEY_SearchPatientMaster_All = "_SearchPatientMaster_All";

        public ServiceResult<CreateUpdatePatientMasterResponse> CreateUpdatePatientMaster(
            CreateUpdatePatientMasterRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdatePatientMaster called. PatientId={request.PatientId}, FirstName={request.FirstName}");

                // Handle patient image file upload if provided
                string patientImagePath = null;
                if (request.PatientImageFile != null && request.PatientImageFile.Length > 0)
                {
                    _log.Info($"Processing patient image file: {request.PatientImageFile.FileName}, Size: {request.PatientImageFile.Length} bytes");

                    var fileUploadHelper = new FileUploadHelper(_configuration);
                    var (uploadSuccess, filePath, uploadError) = fileUploadHelper.UploadFile(
                        request.PatientImageFile,
                        "PatientImages"
                    );

                    if (!uploadSuccess)
                    {
                        _log.Error($"Patient image upload failed: {uploadError}");
                        var alertUpload = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                        return ServiceResult<CreateUpdatePatientMasterResponse>.Failure(
                            alertUpload.Type,
                            $"Patient image upload failed: {uploadError}",
                            500
                        );
                    }

                    patientImagePath = filePath;
                    _log.Info($"Patient image uploaded successfully: {patientImagePath}");
                }

                // Parse DOB to DateTime — SQL Server date column requires a proper DateTime, not a string
                DateTime dobParsed;
                bool dobParsedOk = false;

                // Try common formats: dd-MM-yyyy, yyyy-MM-dd, dd/MM/yyyy, MM/dd/yyyy
                string[] dobFormats = { "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };
                dobParsedOk = DateTime.TryParseExact(
                    request.Dob?.Trim(),
                    dobFormats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out dobParsed);

                if (!dobParsedOk)
                {
                    // Fallback: try general parse
                    dobParsedOk = DateTime.TryParse(request.Dob?.Trim(), out dobParsed);
                }

                if (!dobParsedOk)
                {
                    _log.Warn($"Invalid DOB format received: {request.Dob}");
                    var alertDob = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return ServiceResult<CreateUpdatePatientMasterResponse>.Failure(
                        alertDob.Type,
                        $"Invalid date of birth format: '{request.Dob}'. Expected formats: dd-MM-yyyy or yyyy-MM-dd",
                        400
                    );
                }

                var result = _sqlHelper.ExecuteScalar(
                    "IU_PatientMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        @hospId = globalValues.hospId,
                        @branchId = request.BranchId,
                        @patientId = request.PatientId,
                        @title = request.Title,
                        @firstName = request.FirstName,
                        @middleName = request.MiddleName ?? (object)DBNull.Value,
                        @lastName = request.LastName ?? (object)DBNull.Value,
                        @ageYears = request.AgeYears,
                        @ageMonths = request.AgeMonths,
                        @ageDays = request.AgeDays,
                        @dob = dobParsed,
                        @gender = request.Gender,
                        @maritalStatus = request.MaritalStatus ?? (object)DBNull.Value,
                        @relation = request.Relation ?? (object)DBNull.Value,
                        @relativeName = request.RelativeName ?? (object)DBNull.Value,
                        @idProofName = request.IdProofName ?? (object)DBNull.Value,
                        @idProofNumber = request.IdProofNumber ?? (object)DBNull.Value,
                        @selfContactNumber = request.SelfContactNumber,
                        @emergencyContactNumber = request.EmergencyContactNumber ?? (object)DBNull.Value,
                        @email = request.Email ?? (object)DBNull.Value,
                        @privilegedCardNumber = request.PrivilegedCardNumber ?? (object)DBNull.Value,
                        @address = request.Address ?? (object)DBNull.Value,
                        @countryId = request.CountryId,
                        @country = request.Country ?? (object)DBNull.Value,
                        @stateId = request.StateId,
                        @state = request.State ?? (object)DBNull.Value,
                        @districtId = request.DistrictId,
                        @district = request.District ?? (object)DBNull.Value,
                        @cityId = request.CityId,
                        @city = request.City ?? (object)DBNull.Value,
                        @insuranceCompanyId = request.InsuranceCompanyId,
                        @corporateId = request.CorporateId,
                        @cardNo = request.CardNo ?? (object)DBNull.Value,
                        @patientImagePath = patientImagePath ?? (object)DBNull.Value,
                        @userId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress,
                        @IsVaccination = request.IsVaccination,
                        @vipPatient = request.VipPatient ?? (object)DBNull.Value,
                        @PolicyNo = request.PolicyNo ?? (object)DBNull.Value,
                        @PolicyCardNo = request.PolicyCardNo ?? (object)DBNull.Value,
                        @ExpiryDate = request.ExpiryDate ?? (object)DBNull.Value,
                        @CardHolder = request.CardHolder ?? (object)DBNull.Value,
                        @ReferalNo = request.ReferalNo ?? (object)DBNull.Value,
                        @ReferalDate = request.ReferalDate ?? (object)DBNull.Value,
                        @OnlinePtId = request.OnlinePtId,
                        @healthId = request.HealthId ?? (object)DBNull.Value,
                        @healthIdNumber = request.HealthIdNumber ?? (object)DBNull.Value,
                        @landlineNo = request.LandlineNo ?? (object)DBNull.Value,
                        @birthPlace = request.BirthPlace ?? (object)DBNull.Value,
                        @religion = request.Religion ?? (object)DBNull.Value,
                        @relationPhone = request.RelationPhone ?? (object)DBNull.Value,
                        @relationAge = request.RelationAge ?? (object)DBNull.Value,
                        @relationGender = request.RelationGender ?? (object)DBNull.Value,
                        @eMG_FirstName = request.EMG_FirstName ?? (object)DBNull.Value,
                        @eMG_LastName = request.EMG_LastName ?? (object)DBNull.Value,
                        @eMG_Relation = request.EMG_Relation ?? (object)DBNull.Value,
                        @eMG_MobileNo = request.EMG_MobileNo ?? (object)DBNull.Value,
                        @eMG_ResidentNo = request.EMG_ResidentNo ?? (object)DBNull.Value,
                        @eMG_Address = request.EMG_Address ?? (object)DBNull.Value,
                        @isInternational = request.IsInternational,
                        @locality = request.Locality ?? (object)DBNull.Value,
                        @passportNumber = request.PassportNumber ?? (object)DBNull.Value,
                        @internationalNo = request.InternationalNo ?? (object)DBNull.Value,
                        @membershipNo = request.MembershipNo ?? (object)DBNull.Value,
                        @patientType = request.PatientType ?? (object)DBNull.Value,
                        @identityMark = request.IdentityMark ?? (object)DBNull.Value,
                        @identityMark2 = request.IdentityMark2 ?? (object)DBNull.Value,
                        @referenceType = request.ReferenceType ?? (object)DBNull.Value,
                        @remarks = request.Remarks ?? (object)DBNull.Value,
                    }
                );

                // Clear patient cache after successful operation
                _distributedCache.Remove(CACHE_KEY_PatientMaster_All);
                _distributedCache.Remove(CACHE_KEY_SearchPatientMaster_All);
                _log.Info($"Cleared PatientMaster cache. Key={CACHE_KEY_PatientMaster_All}");

                int resultValue = Convert.ToInt32(result);

                if (resultValue == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Patient already exists: {request.FirstName} {request.LastName}, Contact={request.SelfContactNumber}");
                    return ServiceResult<CreateUpdatePatientMasterResponse>.Failure(
                        alert.Type,
                        "Patient already exists with the same name and contact number",
                        409
                    );
                }

                if (resultValue > 0)
                {
                   

                    var responseData = new CreateUpdatePatientMasterResponse
                    {
                        PatientId = resultValue,
                        PatientImagePath = patientImagePath
                    };

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.PatientId == 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );

                    _log.Info($"Patient {(request.PatientId == 0 ? "created" : "updated")} successfully. PatientId={resultValue}");

                    return ServiceResult<CreateUpdatePatientMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        request.PatientId == 0 ? 201 : 200
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_FAILED");
                _log.Error($"Patient operation failed with result: {resultValue}");
                return ServiceResult<CreateUpdatePatientMasterResponse>.Failure(
                    alert1.Type,
                    alert1.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<CreateUpdatePatientMasterResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }



        public ServiceResult<string> UploadPatientDocument(
    UploadPatientDocumentRequest request,
    AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"UploadPatientDocument called. PatientId={request.PatientId}, DocumentId={request.DocumentId}");

                // Validate file
                if (request.DocumentFile == null || request.DocumentFile.Length == 0)
                {
                    var alertFile = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return ServiceResult<string>.Failure(
                        alertFile.Type,
                        "Document file is required",
                        400
                    );
                }

                // Upload file to DMS
                var fileUploadHelper = new FileUploadHelper(_configuration);
                var (uploadSuccess, filePath, uploadError) = fileUploadHelper.UploadFile(
                    request.DocumentFile,
                    "PatientDocuments"
                );

                if (!uploadSuccess)
                {
                    _log.Error($"Document file upload failed: {uploadError}");
                    var alertUpload = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    return ServiceResult<string>.Failure(
                        alertUpload.Type,
                        $"Document file upload failed: {uploadError}",
                        500
                    );
                }

                _log.Info($"Document file uploaded successfully: {filePath}");

                // Save to database
                _sqlHelper.DML(
                    "IU_PatientDocumentMapping",
                    CommandType.StoredProcedure,
                    new
                    {
                        @hospId = globalValues.hospId,
                        @documentId = request.DocumentId,
                        @patientId = request.PatientId,
                        @documentPath = filePath,
                        @userId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    }
                );

                // Clear cache for this patient's documents
                _distributedCache.Remove($"_PatientDocumentMapping_{request.PatientId}");
                _log.Info($"Cleared PatientDocumentMapping cache for PatientId={request.PatientId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    filePath,
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<PatientDocumentMappingResponse>> GetPatientDocumentMapping(int patientId)
        {
            try
            {
                _log.Info($"GetPatientDocumentMapping called. PatientId={patientId}");

                string cacheKey = $"_PatientDocumentMapping_{patientId}";

                var cachedData = _distributedCache.GetString(cacheKey);
                List<PatientDocumentMappingResponse> documents;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"PatientDocumentMapping retrieved from cache. Key={cacheKey}");
                    documents = JsonSerializer.Deserialize<List<PatientDocumentMappingResponse>>(cachedData);
                }
                else
                {
                    _log.Info($"PatientDocumentMapping cache miss. Fetching from database. Key={cacheKey}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_PatientDocumentMapping",
                        CommandType.StoredProcedure,
                        new { @patientId = patientId }
                    );

                    documents = dataTable?.AsEnumerable().Select(row => new PatientDocumentMappingResponse
                    {
                        DocumentId = row.Field<int>("DocumentId"),
                        DocumentName = row.Field<string>("DocumentName") ?? string.Empty,
                        DocumentCode = row.Field<string>("DocumentCode") ?? string.Empty,
                        DocumentPath = row.Field<string>("DocumentPath") ?? string.Empty,
                        IsMandatory = row.Field<int>("IsMandatory"),

                    }).ToList() ?? new List<PatientDocumentMappingResponse>();

                    if (documents.Any())
                    {
                        var serialized = JsonSerializer.Serialize(documents);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"PatientDocumentMapping cached. Key={cacheKey}, Count={documents.Count}");
                    }
                }

                if (!documents.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No documents found for PatientId={patientId}");
                    return ServiceResult<IEnumerable<PatientDocumentMappingResponse>>.Failure(
                        alert.Type,
                        "No documents found for this patient",
                        404
                    );
                }

                return ServiceResult<IEnumerable<PatientDocumentMappingResponse>>.Success(
                    documents,
                    "Info",
                    $"{documents.Count} document(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<PatientDocumentMappingResponse>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }
        public ServiceResult<IEnumerable<PatientMasterModel>> GetPatientMaster(
            int? patientId = null,
            string? uhid = null,
            string? contactNumber = null,
            int? branchId = null)
        {
            try
            {
                _log.Info($"GetPatientMaster called. PatientId={patientId?.ToString() ?? "All"}, Uhid={uhid ?? "All"}, ContactNumber={contactNumber ?? "All"}, BranchId={branchId?.ToString() ?? "All"}");

                // Try to get ALL patients from cache
                var cachedData = _distributedCache.GetString(CACHE_KEY_PatientMaster_All);
                List<PatientMasterModel> allPatients;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"PatientMaster data retrieved from cache. Key={CACHE_KEY_PatientMaster_All}");
                    allPatients = JsonSerializer.Deserialize<List<PatientMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"PatientMaster cache miss. Fetching all data from database. Key={CACHE_KEY_PatientMaster_All}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetPatientMaster",
                        CommandType.StoredProcedure
                    // No parameters — SP returns all patients
                    );

                    allPatients = dataTable?.AsEnumerable().Select(row => new PatientMasterModel
                    {
                        PatientId = row.Field<int>("PatientId"),
                        BranchId = row.Field<int>("BranchId"),
                        Uhid = row.Field<string>("UHID") ?? string.Empty,
                        Title = row.Field<string>("Title") ?? string.Empty,
                        FirstName = row.Field<string>("FirstName") ?? string.Empty,
                        MiddleName = row.Field<string>("MiddleName"),
                        LastName = row.Field<string>("LastName"),
                        PatientName = row.Field<string>("PatientName") ?? string.Empty,
                        AgeYears = row.Field<int?>("AgeYears"),
                        AgeMonths = row.Field<int?>("AgeMonths"),
                        AgeDays = row.Field<int?>("AgeDays"),
                        Age = row.Field<string>("Age"),
                        Dob = row.Field<string>("DOB"),
                        Gender = row.Field<string>("Gender"),
                        MaritalStatus = row.Field<string>("MaritalStatus"),
                        Relation = row.Field<string>("Relation"),
                        RelativeName = row.Field<string>("RelativeName"),
                        IdProofName = row.Field<string>("IdProofName"),
                        IdProofNumber = row.Field<string>("IdProofNumber"),
                        ContactNumber = row.Field<string>("ContactNumber"),
                        EmergencyContactNumber = row.Field<string>("EmergencyContactNumber"),
                        Email = row.Field<string>("Email"),
                        PrivilegedCardNumber = row.Field<string>("PrivilegedCardNumber"),
                        Address = row.Field<string>("Address"),
                        CountryId = row.Field<int?>("CountryId"),
                        Country = row.Field<string>("Country"),
                        StateId = row.Field<int?>("StateId"),
                        State = row.Field<string>("State"),
                        DistrictId = row.Field<int?>("DistrictId"),
                        District = row.Field<string>("District"),
                        CityId = row.Field<int?>("CityId"),
                        City = row.Field<string>("City"),
                        InsuranceCompanyId = row.Field<int?>("InsuranceCompanyId"),
                        CorporateId = row.Field<int?>("CorporateId"),
                        CardNo = row.Field<string>("CardNo"),
                        IsVaccination = row.Field<int?>("IsVaccination"),
                        VIPPatient = row.Field<int?>("VIPPatient"),
                        PatientImagePath = row.Field<string>("PatientImagePath"),
                        PolicyNo = row.Field<string>("PolicyNo"),
                        PolicyCardNo = row.Field<string>("PolicyCardNo"),
                        ExpiryDate = row.Field<string>("ExpiryDate"),
                        CardHolder = row.Field<string>("CardHolder"),
                        ReferalNo = row.Field<string>("ReferalNo"),
                        ReferalDate = row.Field<string>("ReferalDate"),
                        LandlineNo = row.Field<string>("LandlineNo"),
                        BirthPlace = row.Field<string>("BirthPlace"),
                        Religion = row.Field<string>("Religion"),
                        RelationPhone = row.Field<string>("RelationPhone"),
                        RelationAge = row.Field<int?>("RelationAge"),
                        RelationGender = row.Field<string>("RelationGender"),
                        EMG_FirstName = row.Field<string>("EMG_FirstName"),
                        EMG_LastName = row.Field<string>("EMG_LastName"),
                        EMG_Relation = row.Field<string>("EMG_Relation"),
                        EMG_MobileNo = row.Field<string>("EMG_MobileNo"),
                        EMG_ResidentNo = row.Field<string>("EMG_ResidentNo"),
                        EMG_Address = row.Field<string>("EMG_Address"),
                        IsInternational = row.Field<int?>("IsInternational"),
                        Locality = row.Field<string>("Locality"),
                        PassportNumber = row.Field<string>("PassportNumber"),
                        InternationalNo = row.Field<string>("InternationalNo"),
                        MembershipNo = row.Field<string>("MembershipNo"),
                        PatientType = row.Field<string>("PatientType"),
                        IdentityMark = row.Field<string>("IdentityMark"),
                        IdentityMark2 = row.Field<string>("IdentityMark2"),
                        ReferenceType = row.Field<string>("ReferenceType"),
                        Remarks = row.Field<string>("Remarks"),
                        DoctorId = row.IsNull("DoctorId") ? 0 : row.Field<int>("DoctorId"),
                        IPDNo = row.Field<string>("IPDNo"),
                        DayCareNo = row.Field<string>("DayCareNo"),
                        DialysisNo = row.Field<string>("DialysisNo"),
                        EmergencyNo = row.Field<string>("EmergencyNo"),

                       
                    }).ToList() ?? new List<PatientMasterModel>();

                    // Store ALL patients in cache (no expiration)
                    if (allPatients.Any())
                    {
                        var serialized = JsonSerializer.Serialize(allPatients);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(CACHE_KEY_PatientMaster_All, serialized, cacheOptions);
                        _log.Info($"All PatientMaster data cached permanently. Key={CACHE_KEY_PatientMaster_All}, Count={allPatients.Count}");
                    }
                }

                // Filter in memory based on parameters (always from cache)
                List<PatientMasterModel> filteredPatients = allPatients;

                if (patientId.HasValue)
                {
                    _log.Info($"Filtering by PatientId: {patientId.Value}");
                    filteredPatients = filteredPatients.Where(p => p.PatientId == patientId.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(uhid))
                {
                    _log.Info($"Filtering by UHID: {uhid}");
                    filteredPatients = filteredPatients
                        .Where(p => p.Uhid != null && p.Uhid.Equals(uhid, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(contactNumber))
                {
                    _log.Info($"Filtering by ContactNumber: {contactNumber}");
                    filteredPatients = filteredPatients
                        .Where(p => p.ContactNumber != null && p.ContactNumber.Contains(contactNumber))
                        .ToList();
                }

                if (branchId.HasValue)
                {
                    _log.Info($"Filtering by BranchId: {branchId.Value}");
                    filteredPatients = filteredPatients.Where(p => p.BranchId == branchId.Value).ToList();
                }

                if (!filteredPatients.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("No patients found for the given filters");
                    return ServiceResult<IEnumerable<PatientMasterModel>>.Failure(
                        alert.Type,
                        "No patients found",
                        404
                    );
                }

                _log.Info($"Retrieved {filteredPatients.Count} patient(s) from cache");

                return ServiceResult<IEnumerable<PatientMasterModel>>.Success(
                    filteredPatients,
                    "Info",
                    $"{filteredPatients.Count} patient(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<PatientMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<IEnumerable<SearchPatientMasterModel>> SearchPatientMaster(
            int? patientId = null,
            string? uhid = null,
            string? firstName = null,
            string? middleName = null,
            string? lastName = null,
            string? relativeName = null,
            string? dob = null,
            string? contactNumber = null,
            string? emergencyContactNumber = null,
            string? address = null,
            string? registrationDate = null,
            string? ipdNo = null,
            int? branchId = null)
        {
            try
            {
                _log.Info($"SearchPatientMaster called.");

                var cachedData = _distributedCache.GetString(CACHE_KEY_SearchPatientMaster_All);
                List<SearchPatientMasterModel> allPatients;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"SearchPatientMaster data retrieved from cache.");
                    allPatients = JsonSerializer.Deserialize<List<SearchPatientMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"SearchPatientMaster cache miss. Fetching from database.");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_SearchPatientMaster",
                        CommandType.StoredProcedure
                    );

                    allPatients = dataTable?.AsEnumerable().Select(row => new SearchPatientMasterModel
                    {
                        PatientId = row.Field<int>("PatientId"),
                        BranchId = row.Field<int>("BranchId"),
                        Uhid = row.Field<string>("UHID") ?? string.Empty,
                        Title = row.Field<string>("Title") ?? string.Empty,
                        FirstName = row.Field<string>("FirstName") ?? string.Empty,
                        MiddleName = row.IsNull("MiddleName") ? null : row.Field<string>("MiddleName"),
                        LastName = row.IsNull("LastName") ? null : row.Field<string>("LastName"),
                        PatientName = row.Field<string>("PatientName") ?? string.Empty,
                        AgeYears = row.IsNull("AgeYears") ? null : row.Field<int?>("AgeYears"),
                        AgeMonths = row.IsNull("AgeMonths") ? null : row.Field<int?>("AgeMonths"),
                        AgeDays = row.IsNull("AgeDays") ? null : row.Field<int?>("AgeDays"),
                        Age = row.IsNull("Age") ? null : row.Field<string>("Age"),
                        Dob = row.IsNull("DOB") ? null : row.Field<string>("DOB"),
                        Gender = row.IsNull("Gender") ? null : row.Field<string>("Gender"),
                        Relation = row.IsNull("Relation") ? null : row.Field<string>("Relation"),
                        RelativeName = row.IsNull("RelativeName") ? null : row.Field<string>("RelativeName"),
                        ContactNumber = row.IsNull("ContactNumber") ? null : row.Field<string>("ContactNumber"),
                        EmergencyContactNumber = row.IsNull("EmergencyContactNumber") ? null : row.Field<string>("EmergencyContactNumber"),
                        Email = row.IsNull("Email") ? null : row.Field<string>("Email"),
                        FullAddress = row.IsNull("FullAddress") ? null : row.Field<string>("FullAddress"),
                        RegistrationDate = row.IsNull("RegistrationDate") ? null : row.Field<string>("RegistrationDate"),
                        IPDNo = row.Field<string>("IPDNo"),
                    }).ToList() ?? new List<SearchPatientMasterModel>();

                    if (allPatients.Any())
                    {
                        var serialized = JsonSerializer.Serialize(allPatients);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(CACHE_KEY_SearchPatientMaster_All, serialized, cacheOptions);
                        _log.Info($"SearchPatientMaster data cached. Count={allPatients.Count}");
                    }
                }

                // In-memory filtering
                List<SearchPatientMasterModel> filtered = allPatients;

                if (patientId.HasValue)
                    filtered = filtered.Where(p => p.PatientId == patientId.Value).ToList();

                if (!string.IsNullOrWhiteSpace(uhid))
                    filtered = filtered
                        .Where(p => p.Uhid != null && p.Uhid.Contains(uhid, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(firstName))
                    filtered = filtered
                        .Where(p => p.FirstName != null && p.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(middleName))
                    filtered = filtered
                        .Where(p => p.MiddleName != null && p.MiddleName.Contains(middleName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(lastName))
                    filtered = filtered
                        .Where(p => p.LastName != null && p.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(relativeName))
                    filtered = filtered
                        .Where(p => p.RelativeName != null && p.RelativeName.Contains(relativeName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(dob))
                    filtered = filtered
                        .Where(p => p.Dob != null && p.Dob.Equals(dob, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(contactNumber))
                    filtered = filtered
                        .Where(p => p.ContactNumber != null && p.ContactNumber.Contains(contactNumber))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(emergencyContactNumber))
                    filtered = filtered
                        .Where(p => p.EmergencyContactNumber != null && p.EmergencyContactNumber.Contains(emergencyContactNumber))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(address))
                    filtered = filtered
                        .Where(p => p.FullAddress != null && p.FullAddress.Contains(address, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(registrationDate))
                    filtered = filtered
                        .Where(p => p.RegistrationDate != null && p.RegistrationDate.Equals(registrationDate, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrWhiteSpace(ipdNo))
                    filtered = filtered
                        .Where(p => p.IPDNo != null && p.IPDNo.Contains(ipdNo, StringComparison.OrdinalIgnoreCase))
                        .ToList();

              

                if (branchId.HasValue)
                    filtered = filtered.Where(p => p.BranchId == branchId.Value).ToList();

                if (!filtered.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("No patients found for the given filters");
                    return ServiceResult<IEnumerable<SearchPatientMasterModel>>.Failure(
                        alert.Type,
                        "No patients found",
                        404
                    );
                }

                _log.Info($"Retrieved {filtered.Count} patient(s) from cache");

                return ServiceResult<IEnumerable<SearchPatientMasterModel>>.Success(
                    filtered,
                    "Info",
                    $"{filtered.Count} patient(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<SearchPatientMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }
        public ServiceResult<ServiceBillingDetailsModel> GetServiceAllDetailsForOPDBilling(
          int corporateId,
          int doctorId,
          int serviceItemId,
          int categoryId,
          int subCategoryId,
          int subSubCategoryId,
          int bedTypeId)
        {
            try
            {
                _log.Info($"GetServiceAllDetailsForOPDBilling called. CorporateId={corporateId}, DoctorId={doctorId}, ServiceItemId={serviceItemId}, CategoryId={categoryId}, SubCategoryId={subCategoryId}, SubSubCategoryId={subSubCategoryId}, BedTypeId={bedTypeId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetServiceAllDetailsForOPDBilling",
                    CommandType.StoredProcedure,
                    new
                    {
                        @corporateId = corporateId,
                        @doctorId = doctorId,
                        @serviceItemId = serviceItemId,
                        @categoryId = categoryId,
                        @subCategoryId = subCategoryId,
                        @subSubCategoryId = subSubCategoryId,
                        @bedTypeId = bedTypeId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"No billing details found for ServiceItemId={serviceItemId}, CorporateId={corporateId}");
                    return ServiceResult<ServiceBillingDetailsModel>.Failure(
                        alert.Type,
                        "No billing details found for the given service and corporate",
                        404
                    );
                }

                var row = dataTable.Rows[0];
                var result = new ServiceBillingDetailsModel
                {
                    Rate = row["Rate"] != DBNull.Value ? Convert.ToDecimal(row["Rate"]) : 0,
                    RateListId = row["RateListId"] != DBNull.Value ? Convert.ToInt32(row["RateListId"]) : 0,
                    IsRateEditable = row["IsRateEditable"] != DBNull.Value ? Convert.ToInt32(row["IsRateEditable"]) : 1,
                    ServiceName = row["ServiceName"]?.ToString() ?? string.Empty,
                    Code = row["Code"]?.ToString() ?? string.Empty,
                    CorporateAlias = row["CorporateAlias"]?.ToString() ?? string.Empty,
                    CorporateCode = row["CorporateCode"]?.ToString() ?? string.Empty,
                    ValidityDays = row["ValidityDays"] != DBNull.Value ? Convert.ToInt32(row["ValidityDays"]) : 0,
                    DiscountPer = row["DiscountPer"] != DBNull.Value ? Convert.ToDecimal(row["DiscountPer"]) : 0,
                    DiscountReason = row["DiscountReason"]?.ToString() ?? string.Empty,
                    IsNonPayable = row["IsNonPayable"] != DBNull.Value ? Convert.ToInt32(row["IsNonPayable"]) : 0,
                    ServiceItemId = row["ServiceItemId"] != DBNull.Value ? Convert.ToInt32(row["ServiceItemId"]) : serviceItemId,
                    CorporateId = row["CorporateId"] != DBNull.Value ? Convert.ToInt32(row["CorporateId"]) : corporateId,
                    CategoryId = row["CategoryId"] != DBNull.Value ? Convert.ToInt32(row["CategoryId"]) : categoryId,
                    SubCategoryId = row["SubCategoryId"] != DBNull.Value ? Convert.ToInt32(row["SubCategoryId"]) : subCategoryId,
                    SubSubCategoryId = row["SubSubCategoryId"] != DBNull.Value ? Convert.ToInt32(row["SubSubCategoryId"]) : subSubCategoryId,
                    IsCorporateDiscount = row["IsCorporateDiscount"] != DBNull.Value ? Convert.ToInt32(row["IsCorporateDiscount"]) : 0,
                    GSTPer = row["GSTPer"] != DBNull.Value ? Convert.ToDecimal(row["GSTPer"]) : 0,
                    SampleTypeId = row["SampleTypeId"] != DBNull.Value ? Convert.ToInt32(row["SampleTypeId"]) : 0
                };

                _log.Info($"Service billing details retrieved. ServiceName={result.ServiceName}, Rate={result.Rate}, RateListId={result.RateListId}");

                return ServiceResult<ServiceBillingDetailsModel>.Success(
                    result,
                    "Info",
                    "Service billing details retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<ServiceBillingDetailsModel>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<SaveOPDBillingResponse> SaveOPDBilling(
           SaveOPDBillingRequest request,
           AllGlobalValues globalValues)
        {
            var connectionString = _configuration.GetConnectionString("ConnectionString");
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            var tnx = CustomSqlHelper.getSqlTransaction(con);

            try
            {
                _log.Info($"SaveOPDBilling called. PatientId={request.VisitDetails.PatientId}, BranchId={request.VisitDetails.BranchId}");

                var v = request.VisitDetails;
                decimal totalPaidAmount = 0;
                if (request.PaymentDetails?.Count > 0)
                    totalPaidAmount = request.PaymentDetails.Sum(p => p.Amount);

                // ── 1. PatientVisitDetails ───────────────────────────────────────────
                var pvd = new PatientVisitDetails
                {
                    HospId = globalValues.hospId,
                    BranchId = v.BranchId,
                    PatientId = v.PatientId,
                    Uhid = v.Uhid,
                    Type = "OPD",
                    TypeId = 1,
                    CurrentAge = v.CurrentAge,
                    DoctorId = 0,           // populated from first consultation item
                    CorporateId = v.CorporateId,
                    InsuranceCompanyId = v.InsuranceCompanyId,
                    ReferDoctorId = v.ReferDoctorId > 0 ? v.ReferDoctorId : (int?)null,
                    TotalBillAmount = v.GrossBillAmount,
                    TotalDiscountPerOnBill = v.TotalDiscPerOnBill,
                    TotalDiscountAmountOnBill = v.TotalDiscAmtOnBill,
                    DiscountApprovedById = v.DiscApprovedById > 0 ? v.DiscApprovedById : (int?)null,
                    DiscountReason = v.DiscountReason,
                    RoundOff = v.RoundOff,
                    TotalPatientPayableAmount = v.NetAmount,
                    TotalPaidAmount = totalPaidAmount,
                    TotalBalanceAmount = v.NetAmount - totalPaidAmount,
                    UserId = globalValues.userId,
                    IpAddress = globalValues.ipAddress,
                    UniqueId = v.UniqueId,
                    Mlc = v.Mlc,
                    Pi = v.Pi,
                    Remark = v.Remark,
                    PolicyNo = v.PolicyNo,
                    PolicyCardNo = v.PolicyCardNo,
                    ExpiryDate = v.ExpiryDate,
                    CardHolder = v.CardHolder,
                    ReferalNo = v.ReferalNo,
                    ReferalDate = v.ReferalDate,
                    ProId = v.ProId,
                    ProName = v.ProName,
                    IsSendMRD = v.IsSendMRD
                };

                int visitId = Convert.ToInt32(pvd.Create(_sqlHelper, tnx));
                _log.Info($"PatientVisitDetails created. VisitId={visitId}");

                // ── 2. FinancialTransactions ─────────────────────────────────────────
                var ft = new FinancialTransactions
                {
                    HospId = globalValues.hospId,
                    BranchId = v.BranchId,
                    VisitId = visitId,
                    PatientId = v.PatientId,
                    TnxType = "OPDBilling",
                    TnxTypeId = 1,
                    GrossAmount = v.GrossBillAmount,
                    DiscountPercentage = v.TotalDiscPerOnBill,
                    DiscountAmount = v.TotalDiscAmtOnBill,
                    RoundOff = v.RoundOff,
                    NetAmount = v.NetAmount,
                    Remarks = v.Remarks,
                    UserId = globalValues.userId,
                    IpAddress = globalValues.ipAddress,
                    UniqueId = v.UniqueId
                };

                int ftid = Convert.ToInt32(ft.Create(_sqlHelper, tnx));
                _log.Info($"FinancialTransactions created. FTID={ftid}");

                // ── 3. Process billing items ─────────────────────────────────────────
                bool isReceipt = false;
                bool isDoctorAppointment = false;
                bool isLabInvestigations = false;

                int labNo = 0;
                var sampleTypeBarcodeMap = new Dictionary<int, int>();
                int pathologyTokenNo = 0;
                int radiologyTokenNo = 0;
                int cardiologyTokenNo = 0;

                foreach (var item in request.BillingItems)
                {
                    // ── 3a. FinancialTransactionDetails ──────────────────────────────
                    decimal itemDiscPer, itemDiscAmt, itemNetAmt;
                    string itemDiscReason;

                    if (request.IsBillDiscount == 1)
                    {
                        itemDiscPer = v.TotalDiscPerOnBill;
                        itemDiscAmt = (item.GrossAmt * v.TotalDiscPerOnBill) / 100;
                        itemNetAmt = item.GrossAmt - itemDiscAmt;
                        itemDiscReason = v.DiscountReason;
                    }
                    else
                    {
                        itemDiscPer = item.DiscPer;
                        itemDiscAmt = item.DiscAmt;
                        itemNetAmt = item.NetAmt;
                        itemDiscReason = item.DiscountReason;
                    }

                    var ftd = new FinancialTransactionDetails
                    {
                        HospId = globalValues.hospId,
                        BranchId = v.BranchId,
                        FTID = ftid,
                        VisitId = visitId,
                        PatientId = v.PatientId,
                        ServiceItemId = item.ServiceItemId,
                        SubSubCategoryId = item.SubSubCategoryId,
                        ServiceName = item.ServiceName,
                        ServiceCode = item.Code,
                        CorporateAlias = item.CorporateAlias,
                        CorporateCode = item.CorporateCode,
                        DoctorId = item.DoctorId > 0 ? item.DoctorId : (int?)null,
                        CorporateId = v.CorporateId > 0 ? v.CorporateId : (int?)null,
                        Rate = item.Rate,
                        Qty = item.Qty,
                        GrossAmt = item.GrossAmt,
                        DiscPer = itemDiscPer,
                        DiscAmt = itemDiscAmt,
                        NetAmt = itemNetAmt,
                        IsCorporateNonPayable = item.IsNonPayable,
                        IsUnderPackage = item.IsUnderPackage,
                        DiscountReason = itemDiscReason,
                        PackageId = item.PackageId,
                        RateListId = item.RateListId,
                        DiagnosisId = v.DiagnosisId,
                        UserId = globalValues.userId,
                        IpAddress = globalValues.ipAddress
                    };

                    int ftdId = Convert.ToInt32(ftd.Create(_sqlHelper, tnx));
                    _log.Info($"FinancialTransactionDetails created. FTDId={ftdId}, ServiceItemId={item.ServiceItemId}");

                    // ── 3b. Consultation → DoctorAppointments (CategoryId == 1) ──────
                    if (item.CategoryId == 1)
                    {
                        var appt = new DoctorAppointments
                        {
                            HospId = globalValues.hospId,
                            BranchId = v.BranchId,
                            VisitId = visitId,
                            DoctorId = item.DoctorId,
                            PatientId = v.PatientId,
                            FTDID = ftdId,
                            AppDateTime = DateTime.Now,
                            AppointmentType = "DirectAppointment",
                            ValidUpToDate = DateTime.Now.AddDays(item.ValidityDays),
                            ValidityDays = item.ValidityDays,
                            UserId = globalValues.userId,
                            IpAddress = globalValues.ipAddress
                        };

                        appt.Create(_sqlHelper, tnx);
                        isDoctorAppointment = true;
                        _log.Info($"DoctorAppointments created for DoctorId={item.DoctorId}");
                    }

                    // ── 3c. Investigation (CategoryId == 3) ──────────────────────────
                    else if (item.CategoryId == 3)
                    {
                        // Barcode – one per unique SampleTypeId (Pathology only)
                        int barCode = 0;
                        if (item.SubCategoryId == 1 && item.SampleTypeId > 0)
                        {
                            if (!sampleTypeBarcodeMap.ContainsKey(item.SampleTypeId))
                            {
                                int newBarcode = Convert.ToInt32(_sqlHelper.ExecuteScalar(
                                    tnx,
                                    "S_GetNextGlobalBarcode",
                                    CommandType.StoredProcedure,
                                    new { @branchId = v.BranchId }));
                                sampleTypeBarcodeMap[item.SampleTypeId] = newBarcode;
                            }
                            barCode = sampleTypeBarcodeMap[item.SampleTypeId];
                        }

                        // Lab number – shared for all investigations in this visit
                        if (labNo == 0)
                        {
                            labNo = Convert.ToInt32(_sqlHelper.ExecuteScalar(
                                tnx,
                                "getLabNo",
                                CommandType.StoredProcedure,
                                new { @branchId = v.BranchId },
                                new { result = 0 }));
                        }

                        // Token number per sub-category
                        int tokenNo = 0;
                        if (item.SubCategoryId == 1 || item.SubCategoryId == 2 || item.SubCategoryId == 3)
                        {
                            tokenNo = Convert.ToInt32(_sqlHelper.ExecuteScalar(
                                tnx,
                                "S_GetLabTokenNo",
                                CommandType.StoredProcedure,
                                new { @branchId = v.BranchId, @SubCategoryId = item.SubCategoryId },
                                new { result = 0 }));
                        }

                        if (pathologyTokenNo == 0 && item.SubCategoryId == 1) pathologyTokenNo = tokenNo;
                        if (radiologyTokenNo == 0 && item.SubCategoryId == 2) radiologyTokenNo = tokenNo;
                        if (cardiologyTokenNo == 0 && item.SubCategoryId == 3) cardiologyTokenNo = tokenNo;

                        var pid = new PatientInvestigationDetails
                        {
                            HospId = globalValues.hospId,
                            BranchId = v.BranchId,
                            VisitId = visitId,
                            FTDID = ftdId,
                            InvestigationId = item.ServiceItemId,
                            DoctorId = item.DoctorId,
                            PatientId = v.PatientId,
                            LabNo = labNo,
                            TokenNo = item.SubCategoryId == 1 ? pathologyTokenNo
                                    : item.SubCategoryId == 2 ? radiologyTokenNo
                                    : cardiologyTokenNo,
                            IsUrgent = item.IsUrgent,
                            BarCode = barCode,
                            UserId = globalValues.userId,
                            IpAddress = globalValues.ipAddress
                        };

                        pid.Create(_sqlHelper, tnx);
                        isLabInvestigations = true;
                        _log.Info($"PatientInvestigationDetails created for InvestigationId={item.ServiceItemId}");
                    }
                }

                // ── 4. Receipt ───────────────────────────────────────────────────────
                int receiptId = 0;
                if (totalPaidAmount > 0)
                {
                    var receipt = new Receipts
                    {
                        HospId = globalValues.hospId,
                        BranchId = v.BranchId,
                        FTID = ftid,
                        VisitId = visitId,
                        PatientId = v.PatientId,
                        Amount = totalPaidAmount,
                        PlutusTransactionReferenceID = request.PaymentDetails[0].PlutusTransactionReferenceID,
                        TransactionLogId = request.PaymentDetails[0].TransactionLogId,
                        UserId = globalValues.userId,
                        IpAddress = globalValues.ipAddress,
                        UniqueId = v.UniqueId
                    };

                    receiptId = Convert.ToInt32(receipt.Create(_sqlHelper, tnx));
                    _log.Info($"Receipt created. ReceiptId={receiptId}");

                    foreach (var p in request.PaymentDetails)
                    {
                        // PaymentModeTypeId 4 = Credit → skip
                        if (p.PaymentModeTypeId == 4)
                            continue;

                        var rpmd = new ReceiptsPaymentModeDetails
                        {
                            HospId = globalValues.hospId,
                            BranchId = v.BranchId,
                            ReceiptID = receiptId,
                            Amount = p.Amount,
                            PaymentModeId = p.PaymentModeId,
                            BankId = p.BankId > 0 ? p.BankId : (int?)null,
                            ReferenceNo = p.RefNo,
                            UserId = globalValues.userId,
                            IpAddress = globalValues.ipAddress
                        };

                        rpmd.Create(_sqlHelper, tnx);
                    }

                    isReceipt = true;
                }

                tnx.Commit();
                _log.Info($"SaveOPDBilling committed. VisitId={visitId}, FTID={ftid}, ReceiptId={receiptId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<SaveOPDBillingResponse>.Success(
                    new SaveOPDBillingResponse
                    {
                        VisitId = visitId,
                        FTID = ftid,
                        ReceiptId = receiptId,
                        IsReceipt = isReceipt,
                        IsDoctorAppointment = isDoctorAppointment,
                        IsLabInvestigations = isLabInvestigations
                    },
                    alert.Type,
                    "OPD Billing saved successfully",
                    201
                );
            }
            catch (Exception ex)
            {
                try { tnx.Rollback(); } catch { /* swallow rollback exception */ }
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<SaveOPDBillingResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
            finally
            {
                tnx.Dispose();
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        public ServiceResult<IEnumerable<PackageAllDetailsModel>> GetPackageAllDetails(int packageId)
        {
            try
            {
                _log.Info($"GetPackageAllDetails called. PackageId={packageId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetPackageAllDetails",
                    CommandType.StoredProcedure,
                    new { packageId = packageId }
                );

                var packageDetails = dataTable?.AsEnumerable().Select(row => new PackageAllDetailsModel
                {
                    PackageId = row.Field<int>("PackageId"),
                    PackageName = row.Field<string>("PackageName") ?? string.Empty,
                    PackageCode = row.Field<string>("PackageCode") ?? string.Empty,
                    IsActive = row.Field<int>("IsActive"),
                    SubSubCategoryId = row.Field<int?>("SubSubCategoryId") ?? 0,
                    SubCategoryId = row.Field<int?>("SubCategoryId") ?? 0,
                    CategoryId = row.Field<int?>("CategoryId") ?? 0,
                    StartsFrom = row.Field<string>("StartsFrom") ?? string.Empty,
                    ExpiresOn = row.Field<string>("ExpiresOn") ?? string.Empty,
                    PackageServiceNameCode = row.Field<string>("PackageServiceNameCode") ?? string.Empty,
                    PackageServiceName = row.Field<string>("PackageServiceName") ?? string.Empty,
                    PackageServiceId = row.Field<int>("PackageServiceId"),
                    QTY = row.Field<int>("QTY"),
                    PackageServiceCategory = row.Field<string>("PackageServiceCategory") ?? string.Empty,
                    PackageServiceCode = row.Field<string>("PackageServiceCode") ?? string.Empty,
                    PackageServiceCategoryId = row.Field<int?>("PackageServiceCategoryId") ?? 0,
                    PackageServiceSubCategoryId = row.Field<int?>("PackageServiceSubCategoryId") ?? 0,
                    PackageServiceSubSubCategoryId = row.Field<int?>("PackageServiceSubSubCategoryId") ?? 0,

                }).ToList() ?? new List<PackageAllDetailsModel>();

                if (!packageDetails.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No package details found for PackageId={packageId}");
                    return ServiceResult<IEnumerable<PackageAllDetailsModel>>.Failure(
                        alert.Type,
                        $"No details found for PackageId: {packageId}",
                        404
                    );
                }

                _log.Info($"Retrieved {packageDetails.Count} service item(s) for PackageId={packageId}");

                return ServiceResult<IEnumerable<PackageAllDetailsModel>>.Success(
                    packageDetails,
                    "Info",
                    $"{packageDetails.Count} service item(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<PackageAllDetailsModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetReceiptDetailsByFTID(int ftid, int isReceipt, int receiptId, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"GetReceiptDetailsByFTID called. FTID={ftid}, IsReceipt={isReceipt}, ReceiptId={receiptId}, PrintUserId={globalValues.userId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetReceiptDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @FTID = ftid,
                        @isReceipt = isReceipt,
                        @receiptId = receiptId,
                        @printUserId = globalValues.userId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No receipt details found for FTID={ftid}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No receipt details found",
                        404
                    );
                }

                // Return raw DataTable as list of dictionaries without model mapping
                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"Receipt details retrieved successfully for FTID={ftid}. Rows={result.Count}");

                return ServiceResult<object>.Success(
                    result,
                    "Info",
                    $"Receipt details retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetOPDReceiptList(string visitNo)
        {
            try
            {
                _log.Info($"GetOPDReceiptList called. VisitNo={visitNo}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetReceiptAllDetailsForOPDPatient",
                    CommandType.StoredProcedure,
                    new
                    {
                        @VisitNo = visitNo
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No receipt list found for VisitNo={visitNo}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No receipts found for the given visit",
                        404
                    );
                }

                // Return raw DataTable as list of dictionaries without model mapping
                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"OPD receipt list retrieved successfully for VisitNo={visitNo}. Rows={result.Count}");

                return ServiceResult<object>.Success(
                    result,
                    "Info",
                    $"Receipt list retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetOPDCardDetails(long ftid)
        {
            try
            {
                _log.Info($"GetOPDCardDetails called. FTID={ftid}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetOPDCardDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @FTID = ftid
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No OPD card details found for FTID={ftid}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No OPD card details found",
                        404
                    );
                }

                // Return raw DataTable as list of dictionaries without model mapping
                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"OPD card details retrieved successfully for FTID={ftid}. Rows={result.Count}");

                return ServiceResult<object>.Success(
                    result,
                    "Info",
                    $"OPD card details retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<DataTable> FindDuplicateService(int serviceItemId, int patientId)
        {
            try
            {
                _log.Info($"FindDuplicateService called. ServiceItemId={serviceItemId}, PatientId={patientId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetDublicateServiceName",
                    CommandType.StoredProcedure,
                    new
                    {
                        @ServiceItemId = serviceItemId,
                        @PatientId = patientId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No duplicate service found for ServiceItemId={serviceItemId}, PatientId={patientId}");
                    return ServiceResult<DataTable>.Failure(
                        alert.Type,
                        "No duplicate service found for today",
                        404
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                _log.Info($"Found {dataTable.Rows.Count} duplicate service record(s)");

                return ServiceResult<DataTable>.Success(
                    dataTable,
                    alert1.Type,
                    $"{dataTable.Rows.Count} duplicate service record(s) found",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<DataTable>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetInvestigationObservationMappingDetails(int investigationId, int ageInDays, string gender)
        {
            try
            {
                _log.Info($"GetInvestigationObservationMappingDetails called. InvestigationId={investigationId}, AgeInDays={ageInDays}, Gender={gender}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_getInvestigationObservationMappingDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @investigationId = investigationId,
                        @ageInDays = ageInDays,
                        @gender = gender
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No observation mapping details found for InvestigationId={investigationId}, AgeInDays={ageInDays}, Gender={gender}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                _log.Info($"GetInvestigationObservationMappingDetails retrieved {rawData.Count} record(s)");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetUserDiscountRights(int userId)
        {
            try
            {
                _log.Info($"GetUserDiscountRights called. UserId={userId}");

                string cacheKey = $"_UserDiscountRights_User{userId}";

                var cachedData = _distributedCache.GetString(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserDiscountRights data retrieved from cache. Key={cacheKey}");
                    return ServiceResult<object>.Success(
                        System.Text.Json.JsonSerializer.Deserialize<object>(cachedData),
                        "Info",
                        "Data retrieved successfully",
                        200
                    );
                }

                _log.Info($"UserDiscountRights cache miss. Fetching from database. Key={cacheKey}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetUserDiscountRights",
                    CommandType.StoredProcedure,
                    new { @userId = userId }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No discount rights found for UserId={userId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                // Convert DataTable to raw list of dictionaries
                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                // Cache the raw data
                var serialized = System.Text.Json.JsonSerializer.Serialize(rawData);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = null,
                    SlidingExpiration = null
                };
                _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                _log.Info($"UserDiscountRights data cached permanently. Key={cacheKey}");

                return ServiceResult<object>.Success(
                    rawData,
                    "Info",
                    "Discount rights retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetPatientPreviousDues(int branchId, int patientId)
        {
            try
            {
                _log.Info($"GetPatientPreviousDues called. BranchId={branchId}, PatientId={patientId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetPatientPreviousDues",
                    CommandType.StoredProcedure,
                    new
                    {
                        @branchId = branchId,
                        @patientId = patientId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No previous dues found for BranchId={branchId}, PatientId={patientId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                _log.Info($"GetPatientPreviousDues retrieved {rawData.Count} record(s)");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetPatientLastConsultationDetail(int patientId)
        {
            try
            {
                _log.Info($"GetPatientLastConsultationDetail called. PatientId={patientId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetLastVisit",
                    CommandType.StoredProcedure,
                    new
                    {
                        @patientId = patientId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No last consultation detail found for PatientId={patientId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                _log.Info($"GetPatientLastConsultationDetail retrieved {rawData.Count} record(s)");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetServiceItemDetailsByVisitId(int visitId)
        {
            try
            {
                _log.Info($"GetServiceItemDetailsByVisitId called. VisitId={visitId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_getServiceDetailsByVisitId",
                    CommandType.StoredProcedure,
                    new
                    {
                        @VisitId = visitId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No service item details found for VisitId={visitId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                _log.Info($"GetServiceItemDetailsByVisitId retrieved {rawData.Count} record(s)");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetPatientBalanceAmountOPD(string uhid)
        {
            try
            {
                _log.Info($"GetPatientBalanceAmountOPD called. UHID={uhid}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetPatientDueAmountOPD",
                    CommandType.StoredProcedure,
                    new { @uhid = uhid }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No OPD balance amount found for UHID={uhid}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                _log.Info($"GetPatientBalanceAmountOPD retrieved {rawData.Count} record(s) for UHID={uhid}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetPatientBalanceAmountIPD(string uhid)
        {
            try
            {
                _log.Info($"GetPatientBalanceAmountIPD called. UHID={uhid}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetPatientDueAmountIPD",
                    CommandType.StoredProcedure,
                    new { @uhid = uhid }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No IPD balance amount found for UHID={uhid}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                _log.Info($"GetPatientBalanceAmountIPD retrieved {rawData.Count} record(s) for UHID={uhid}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetPatientBalanceAmountPharmacy(string uhid)
        {
            try
            {
                _log.Info($"GetPatientBalanceAmountPharmacy called. UHID={uhid}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetPatientDueAmountPharmacy",
                    CommandType.StoredProcedure,
                    new { @uhid = uhid }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No Pharmacy balance amount found for UHID={uhid}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var rawData = dataTable.Rows
                    .Cast<DataRow>()
                    .Select(row => dataTable.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(col => col.ColumnName, col => row[col] == DBNull.Value ? null : row[col])
                    ).ToList();

                _log.Info($"GetPatientBalanceAmountPharmacy retrieved {rawData.Count} record(s) for UHID={uhid}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<Dictionary<string, object>>> SearchPatientForConsultation(
           SearchPatientForConsultationRequest request)
        {
            try
            {
                _log.Info($"SearchPatientForConsultation called. BranchId={request.BranchId}, TypeId={request.TypeId}, " +
                          $"FromDate={request.FromDate}, ToDate={request.ToDate}");

                // Parse dates
                if (!DateTime.TryParse(request.FromDate, out DateTime fromDate))
                {
                    var alertDate = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                        alertDate.Type, "Invalid FromDate format", 400);
                }

                if (!DateTime.TryParse(request.ToDate, out DateTime toDate))
                {
                    var alertDate = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                        alertDate.Type, "Invalid ToDate format", 400);
                }

                var dataTable = _sqlHelper.GetDataTable(
                    "S_SearchPatientForConsultation",
                    CommandType.StoredProcedure,
                    new
                    {
                        @branchId = request.BranchId,
                        @uhid = request.Uhid ?? string.Empty,
                        @appNo = request.AppNo,
                        @doctorId = request.DoctorId,
                        @typeId = request.TypeId,
                        @bedTypeId = request.BedTypeId,
                        @fromDate = fromDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        @toDate = toDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        @statusId = request.StatusId,
                        @dateTypeId = request.DateTypeId,
                        @doctorDepartmentId = request.DoctorDepartmentId
                    });

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("SearchPatientForConsultation: no records found");
                    return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                        alert.Type, "No patients found", 404);
                }

                // Convert every row to a plain dictionary so the response is
                // column-name driven, not model driven.
                var rows = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns
                        .Cast<System.Data.DataColumn>()
                        .ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        )
                ).ToList();

                _log.Info($"SearchPatientForConsultation: returned {rows.Count} record(s)");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<IEnumerable<Dictionary<string, object>>>.Success(
                    rows,
                    alert1.Type,
                    $"{rows.Count} patient(s) found",
                    200);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                    alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> GetPatientVital(int patientId)
        {
            try
            {
                _log.Info($"GetPatientVital called. PatientId={patientId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_getPatientVital",
                    CommandType.StoredProcedure,
                    new { patientId = patientId }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No vitals found for PatientId={patientId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        $"No vitals found for PatientId: {patientId}",
                        404
                    );
                }

                // Group by VitalDateTime first, then take VisitId from that group
                var grouped = dataTable.AsEnumerable()
                    .GroupBy(row => row["VitalDateTime"]?.ToString() ?? string.Empty)
                    .Select(g =>
                    {
                        // Take VisitId from the first row that has a non-zero VisitId
                        // within this VitalDateTime group
                        int visitId = g
                            .Select(row => row["VisitId"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(row["VisitId"]))
                            .FirstOrDefault(v => v != 0);

                        return new
                        {
                            VitalDateTime = g.Key,
                            VisitId = visitId,
                            Vitals = g.Select(row =>
                            {
                                var dict = new Dictionary<string, object>();
                                foreach (DataColumn col in dataTable.Columns)
                                {
                                    // Exclude VitalDateTime and VisitId from inner vitals
                                    // since they are now at group level
                                    if (col.ColumnName == "VitalDateTime") continue;
                                    if (col.ColumnName == "VisitId") continue;
                                    dict[col.ColumnName] = row[col] == DBNull.Value
                                        ? null
                                        : row[col];
                                }
                                return dict;
                            }).ToList()
                        };
                    })
                    .ToList();

                _log.Info($"Retrieved {grouped.Count} VitalDateTime group(s) for PatientId={patientId}");

                return ServiceResult<object>.Success(
                    grouped,
                    "Info",
                    $"{grouped.Count} group(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<string> SavePatientVital(SavePatientVitalRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"savePatientVital called. VisitId={request.VisitId}, PatientId={request.PatientId}, VitalId={request.VitalId}, Id={request.Id}");

                var dataTable = _sqlHelper.GetDataTable(
                    "I_savePatientVital",
                    CommandType.StoredProcedure,
                    new
                    {
                        @visitId = request.VisitId,
                        @patientId = request.PatientId,
                        @vitalId = request.VitalId,
                        @vitalValue = request.VitalValue,
                        @vitalDateTime = string.IsNullOrWhiteSpace(request.VitalDateTime)
                            ? (object)DBNull.Value
                            : request.VitalDateTime,
                        @Id = request.Id,
                        @userId = globalValues.userId,
                        @ipAddress= globalValues.ipAddress
                    }
                );

                bool success = dataTable != null
                    && dataTable.Rows.Count > 0
                    && Convert.ToInt32(dataTable.Rows[0]["success"]) == 1;

                if (!success)
                {
                    var failAlert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_FAILED");
                    _log.Error($"savePatientVital SP returned failure for PatientId={request.PatientId}");
                    return ServiceResult<string>.Failure(failAlert.Type, failAlert.Message, 500);
                }

              

                var alert = _messageService.GetMessageAndTypeByAlertCode(
                    request.Id > 0 ? "DATA_UPDATED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                );

                _log.Info($"PatientVital {(request.Id > 0 ? "updated" : "saved")} successfully. PatientId={request.PatientId}");

                return ServiceResult<string>.Success(
                    request.Id > 0 ? "Vital updated successfully" : "Vital saved successfully",
                    alert.Type,
                    alert.Message,
                    request.Id > 0 ? 200 : 201
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> GetPatientObservationResultsTrend(int patientId, int pageNumber, int pageSize)
        {
            try
            {
                _log.Info($"GetPatientObservationResultsTrend called. PatientId={patientId}, PageNumber={pageNumber}, PageSize={pageSize}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetPatientObservationResultsTrend",
                    CommandType.StoredProcedure,
                    new
                    {
                        @PatientId = patientId,
                        @PageNumber = pageNumber,
                        @PageSize = pageSize
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No observation trend data found for PatientId={patientId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var data = dataTable.AsEnumerable().Select(row =>
                      dataTable.Columns.Cast<DataColumn>().ToDictionary(
                          col => col.ColumnName,
                          col => row[col] == DBNull.Value ? null : row[col]
                      )
                  ).ToList<object>();
                  
                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                _log.Info($"Retrieved {data.Count} observation trend record(s) for PatientId={patientId}");

                return ServiceResult<object>.Success(
                    data,
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<SaveIPDAdmissionResponse> SaveIPDAdmission(
    SaveIPDAdmissionRequest request,
    AllGlobalValues globalValues)
        {
            var connectionString = _configuration.GetConnectionString("ConnectionString");
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            var tnx = CustomSqlHelper.getSqlTransaction(con);

            try
            {
                _log.Info($"SaveIPDAdmission called. PatientId={request.PatientId}, BranchId={request.BranchId}");

                // ── 1. PatientVisitDetails ───────────────────────────────────────────
                var pvd = new PatientVisitDetails
                {
                    HospId = globalValues.hospId,
                    BranchId = request.BranchId,
                    PatientId = request.PatientId,
                    Uhid = request.Uhid,
                    Type = "IPD",
                    TypeId = 2,
                    CurrentAge = request.CurrentAge,
                    DoctorId = request.PrimaryDoctorId,
                    CorporateId = request.CorporateId,
                    InsuranceCompanyId = request.InsuranceCompanyId,
                    ReferDoctorId = request.ReferDoctorId > 0 ? request.ReferDoctorId : (int?)null,
                    ProId = request.ProId,
                    ProName = request.ProName,
                    AdmissionType = request.AdmissionType,
                    BillingTypeId = request.BillingTypeId,
                    RoomTypeId = request.RoomTypeId,
                    BedId = request.BedId,
                    AdmissionDate = request.AdmissionDate,
                    AdmissionTime = request.AdmissionTime,
                    StatusId = 1,
                    Status = "IN",
                    AttendantRelation = request.AttendantRelation,
                    AttendantName = request.AttendantName,
                    AttendantContactNumber = request.AttendantContactNumber,
                    HandleWithCare = request.HandleWithCare,
                    NameMasking = request.NameMasking,
                    UserId = globalValues.userId,
                    IpAddress = globalValues.ipAddress
                };

                int visitId = Convert.ToInt32(pvd.Create(_sqlHelper, tnx));
                _log.Info($"PatientVisitDetails created. VisitId={visitId}");

                // ── 2. MLC (only when AdmissionType == "MLC") ───────────────────────
                if (request.AdmissionType == "MLC")
                {
                    _sqlHelper.DML(tnx, "I_MLC", CommandType.StoredProcedure, new
                    {
                        @VisitId = visitId,
                        @MLCNo = request.MlcNo,
                        @MLCTypeId = request.MlcTypeId,
                        @MLCType = request.MlcType,
                        @InjuryTypeId = request.InjuryTypeId,
                        @InjuryType = request.InjuryType,
                        @BroughtBy = request.BroughtBy,
                        @TransportId = request.TransportId,
                        @Transport = request.Transport,
                        @PlaceOfAccident = request.PlaceOfAccident,
                        @PoliceStation = request.PoliceStation,
                        @OfficerName = request.OfficerName,
                        @OfficerPhone = request.OfficerPhone,
                        @ComplaintNo = request.ComplaintNo,
                        @BuckleNoOfPolice = request.BuckleNoOfPolice,
                        @DateOfInjury = request.DateOfInjury,
                        @DateOfInitiation = request.DateOfInitiation,
                        @CauseOfAccident = request.CauseOfAccident,
                        @IdentificationMarks = request.IdentificationMarks,
                        @Remarks = request.Remarks,
                        @UserId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    });
                    _log.Info($"MLC record created for VisitId={visitId}");
                }

                // ── 3. Secondary doctor mappings ────────────────────────────────────
                foreach (var doctorId in request.SecondaryDoctorIds)
                {
                    _sqlHelper.DML(tnx, "I_IPDVisitDoctorMapping", CommandType.StoredProcedure, new
                    {
                        @visitId = visitId,
                        @doctorId = doctorId,
                        @isPrimaryDoctor = 0,
                        @userId = globalValues.userId,
                        @ipAddress = globalValues.ipAddress
                    });
                }

                // ── 4. Primary doctor mapping ────────────────────────────────────────
                _sqlHelper.DML(tnx, "I_IPDVisitDoctorMapping", CommandType.StoredProcedure, new
                {
                    @visitId = visitId,
                    @doctorId = request.PrimaryDoctorId,
                    @isPrimaryDoctor = 1,
                    @userId = globalValues.userId,
                    @ipAddress = globalValues.ipAddress
                });
                _log.Info($"Doctor mappings created. PrimaryDoctorId={request.PrimaryDoctorId}");

                // ── 5. Bed mapping ───────────────────────────────────────────────────
                _sqlHelper.DML(tnx, "IU_IPDVisitBedMapping", CommandType.StoredProcedure, new
                {
                    @visitId = visitId,
                    @bedId = request.BedId,
                    @userId = globalValues.userId,
                    @ipAddress = globalValues.ipAddress
                });

                // ── 6. Update bed status to occupied ────────────────────────────────
                _sqlHelper.DML(tnx, "U_UpdateBedStatus", CommandType.StoredProcedure, new
                {
                    @bedId = request.BedId,
                    @currentStatus = 1   // PatientAdmitted
                });
                _log.Info($"Bed {request.BedId} marked as occupied");

                // ── 7. Corporate mapping ─────────────────────────────────────────────
                _sqlHelper.DML(tnx, "IU_IPDVisitCorporateMapping", CommandType.StoredProcedure, new
                {
                    @visitId = visitId,
                    @insuranceCompanyId = request.InsuranceCompanyId,
                    @corporateId = request.CorporateId,
                    @userId = globalValues.userId,
                    @ipAddress = globalValues.ipAddress
                });

                // ── 8. Doctor IPD sequence number ────────────────────────────────────
                _sqlHelper.DML(tnx, "I_IPDVisitDoctorSequence", CommandType.StoredProcedure, new
                {
                    @branchId = request.BranchId,
                    @doctorId = request.PrimaryDoctorId,
                    @visitId = visitId
                });

                tnx.Commit();
                _log.Info($"SaveIPDAdmission committed. VisitId={visitId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<SaveIPDAdmissionResponse>.Success(
                    new SaveIPDAdmissionResponse { VisitId = visitId },
                    alert.Type,
                    "IPD Admission saved successfully",
                    201
                );
            }
            catch (Exception ex)
            {
                try { tnx.Rollback(); } catch { }
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<SaveIPDAdmissionResponse>.Failure(alert.Type, alert.Message, 500);
            }
            finally
            {
                tnx.Dispose();
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        public ServiceResult<object> SearchIPDPatient(SearchIPDPatientRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"SearchIPDPatient called. BranchId={request.BranchId}, SearchBy={request.SearchBy}, StatusId={request.StatusId}");

                string filter = null;
                if (!string.IsNullOrWhiteSpace(request.SearchBy) && !string.IsNullOrWhiteSpace(request.SearchValue))
                {
                    if (request.SearchBy == "PVD.VisitNo")
                        filter = request.SearchBy + " = '" + request.SearchValue + "'";
                    else if (request.SearchBy == "AdmissionDate" || request.SearchBy == "DischargeDate")
                    {
                        if (!DateTime.TryParse(request.SearchValue, out DateTime parsedDate))
                        {
                            var alertDate = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                            return ServiceResult<object>.Failure(alertDate.Type, $"Invalid date format for {request.SearchBy}", 400);
                        }
                        filter = request.SearchBy + " = '" + parsedDate.ToString("yyyy-MM-dd") + "'";
                    }
                    else
                        filter = request.SearchBy + " LIKE '%" + request.SearchValue + "%'";
                }

                var dataTable = _sqlHelper.GetDataTable(
                    "S_SearchIPDPatient",
                    CommandType.StoredProcedure,
                    new
                    {
                        @branchId = request.BranchId,
                        @statusId = request.StatusId == 0 ? "0" : request.StatusId.ToString(),
                        @UserId = globalValues.userId.ToString(),
                        @filter = filter
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("SearchIPDPatient: no records found");
                    return ServiceResult<object>.Failure(alert.Type, "No IPD patients found", 404);
                }

                var rows = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"SearchIPDPatient: returned {rows.Count} record(s)");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(rows, alert1.Type, $"{rows.Count} patient(s) found", 200);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<string> UploadVisitWisePatientDocument(
    UploadVisitWisePatientDocumentRequest request,
    AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"UploadVisitWisePatientDocument called. PatientId={request.PatientId}, VisitId={request.VisitId}, DocumentId={request.DocumentId}, DocumentCategoryId={request.DocumentCategoryId}");

                if (request.DocumentFile == null || request.DocumentFile.Length == 0)
                {
                    var alertFile = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return ServiceResult<string>.Failure(alertFile.Type, "Document file is required", 400);
                }

                var fileUploadHelper = new FileUploadHelper(_configuration);
                var (uploadSuccess, filePath, uploadError) = fileUploadHelper.UploadFile(
                    request.DocumentFile,
                    "PatientDocuments"
                );

                if (!uploadSuccess)
                {
                    _log.Error($"Document file upload failed: {uploadError}");
                    var alertUpload = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    return ServiceResult<string>.Failure(alertUpload.Type, $"Document file upload failed: {uploadError}", 500);
                }

                _log.Info($"Document file uploaded successfully: {filePath}");

                _sqlHelper.DML(
                    "IU_VisitWisePatientDocumentMapping",
                    CommandType.StoredProcedure,
                    new
                    {
                        @documentId = request.DocumentId,
                        @patientId = request.PatientId,
                        @visitId = request.VisitId,
                        @documentCategoryId = request.DocumentCategoryId,
                        @documentPath = filePath,
                        @userId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    }
                );

                // Invalidate cache for this combination
                _distributedCache.Remove($"_VisitWisePatientDocumentMapping_{request.DocumentCategoryId}_{request.VisitId}_{request.PatientId}");
                _log.Info($"Cleared VisitWisePatientDocumentMapping cache. VisitId={request.VisitId}, PatientId={request.PatientId}, DocumentCategoryId={request.DocumentCategoryId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(filePath, alert.Type, alert.Message, 200);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> GetVisitWisePatientDocumentMapping(
            int documentCategoryId,
            int visitId,
            int patientId)
        {
            try
            {
                _log.Info($"GetVisitWisePatientDocumentMapping called. DocumentCategoryId={documentCategoryId}, VisitId={visitId}, PatientId={patientId}");

                string cacheKey = $"_VisitWisePatientDocumentMapping_{documentCategoryId}_{visitId}_{patientId}";

                var cachedData = _distributedCache.GetString(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"VisitWisePatientDocumentMapping retrieved from cache. Key={cacheKey}");
                    return ServiceResult<object>.Success(
                        JsonSerializer.Deserialize<object>(cachedData),
                        "Info",
                        "Documents retrieved successfully",
                        200
                    );
                }

                _log.Info($"VisitWisePatientDocumentMapping cache miss. Fetching from database. Key={cacheKey}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_VisitWisePatientDocumentMapping",
                    CommandType.StoredProcedure,
                    new
                    {
                        @documentCategoryId = documentCategoryId,
                        @visitId = visitId,
                        @patientId = patientId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No documents found for DocumentCategoryId={documentCategoryId}, VisitId={visitId}, PatientId={patientId}");
                    return ServiceResult<object>.Failure(alert.Type, "No documents found", 404);
                }

                var rawData = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                var serialized = JsonSerializer.Serialize(rawData);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = null,
                    SlidingExpiration = null
                };
                _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                _log.Info($"VisitWisePatientDocumentMapping cached. Key={cacheKey}, Count={rawData.Count}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} document(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }
    }
}