using System.Data;
using System.Reflection;
using System.Text.Json;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.DTO;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;

namespace HISWEBAPI.Repositories.Implementations
{
    public class EMRRepository : IEMRRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string CACHE_KEY_AllergyMaster_All = "_AllergyMaster_All";

        public EMRRepository(
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

        public ServiceResult<object> GetAllergyMasterList(int? isActive, int? allergyTypeId)
        {
            try
            {
                _log.Info($"GetAllergyMasterList called. IsActive={isActive?.ToString() ?? "All"}");

                var cachedData = _distributedCache.GetString(CACHE_KEY_AllergyMaster_All);
                List<Dictionary<string, object>> allItems;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"AllergyMaster data retrieved from cache. Key={CACHE_KEY_AllergyMaster_All}");
                    allItems = System.Text.Json.JsonSerializer
                        .Deserialize<List<Dictionary<string, object>>>(cachedData);
                }
                else
                {
                    _log.Info($"AllergyMaster cache miss. Fetching from database. Key={CACHE_KEY_AllergyMaster_All}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetAllergyMasterList",
                        CommandType.StoredProcedure
                    );

                    allItems = dataTable?.AsEnumerable().Select(row =>
                        dataTable.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                col => col.ColumnName,
                                col => row[col] == DBNull.Value ? null : row[col]
                            )
                    ).ToList() ?? new List<Dictionary<string, object>>();

                    if (allItems.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allItems);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(CACHE_KEY_AllergyMaster_All, serialized, cacheOptions);
                        _log.Info($"AllergyMaster data cached permanently. Key={CACHE_KEY_AllergyMaster_All}, Count={allItems.Count}");
                    }
                }

                // In-memory filter by IsActive
                if (isActive.HasValue)
                {
                    allItems = allItems.Where(row =>
                    {
                        if (row.TryGetValue("IsActive", out var val) && val != null)
                            return val.ToString() == isActive.Value.ToString();
                        return false;
                    }).ToList();
                    _log.Info($"Filtered by IsActive={isActive.Value}. Count={allItems.Count}");
                }

                if (allergyTypeId.HasValue)
                {
                    allItems = allItems.Where(row =>
                    {
                        if (row.TryGetValue("AllergyTypeId", out var val) && val != null)
                            return val.ToString() == allergyTypeId.Value.ToString();
                        return false;
                    }).ToList();
                    _log.Info($"Filtered by AllergyTypeId={allergyTypeId.Value}. Count={allItems.Count}");
                }

                if (!allItems.Any())
                {
                    var notFoundAlert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<object>.Failure(
                        notFoundAlert.Type,
                        "No allergy records found",
                        404
                    );
                }

                var alert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    allItems,
                    alert.Type,
                    $"{allItems.Count} allergy record(s) retrieved successfully",
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

        public ServiceResult<object> CreateUpdateAllergyMaster(
            CreateUpdateAllergyMasterRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdateAllergyMaster called. AllergyId={request.AllergyId}, AllergyName={request.AllergyName}");

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@AllergyId",     SqlDbType.Int)          { Value = request.AllergyId },
                    new SqlParameter("@AllergyName",   SqlDbType.NVarChar, 256){ Value = request.AllergyName },
                    new SqlParameter("@AllergyTypeId", SqlDbType.Int)          { Value = request.AllergyTypeId },
                    new SqlParameter("@AllergyType",   SqlDbType.NVarChar, 256){ Value = request.AllergyType ?? (object)DBNull.Value },
                    new SqlParameter("@snomedCode",    SqlDbType.NVarChar, 100){ Value = request.SnomedCode ?? (object)DBNull.Value },
                    new SqlParameter("@active",        SqlDbType.Int)          { Value = request.Active },
                    new SqlParameter("@userId",        SqlDbType.Int)          { Value = globalValues.userId },
                    new SqlParameter("@IpAddress",     SqlDbType.NVarChar, 20) { Value = globalValues.ipAddress ?? (object)DBNull.Value },
                    new SqlParameter("@Result",        SqlDbType.Int)          { Direction = ParameterDirection.Output }
                };

                long result = _sqlHelper.RunProcedureInsert("IU_AllergyMaster", parameters);

                if (result == -1)
                {
                    var dupAlert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate AllergyName: {request.AllergyName}");
                    return ServiceResult<object>.Failure(
                        dupAlert.Type,
                        "Allergy name already exists",
                        409
                    );
                }

                if (result > 0)
                {
                    _distributedCache.Remove(CACHE_KEY_AllergyMaster_All);
                    _log.Info($"Cleared AllergyMaster cache. AllergyId={result}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.AllergyId == 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );
                    return ServiceResult<object>.Success(
                        new { AllergyId = result },
                        alert.Type,
                        alert.Message,
                        request.AllergyId == 0 ? 201 : 200
                    );
                }

                var failAlert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(failAlert.Type, failAlert.Message, 500);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<string> DeleteAllergyMaster(int allergyId, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"DeleteAllergyMaster called. AllergyId={allergyId}");

                var result = _sqlHelper.DML(
                    "U_DeleteAllergyMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        @allergyId = allergyId
                    }
                );

                if (result > 0)
                {
                    _distributedCache.Remove(CACHE_KEY_AllergyMaster_All);
                    _log.Info($"Cleared AllergyMaster cache after delete. AllergyId={allergyId}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_DELETED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Allergy deleted successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }
                else
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"AllergyMaster not found for AllergyId={allergyId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "Allergy not found",
                        404
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<IEnumerable<Dictionary<string, object>>> GetSaltNameMasterList()
        {
            try
            {
                _log.Info("GetSaltNameMasterList called.");

                string cacheKey = "_SaltNameMaster_All";

                var cachedData = _distributedCache.GetString(cacheKey);
                List<Dictionary<string, object>> saltNames;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"SaltNameMaster data retrieved from cache. Key={cacheKey}");
                    saltNames = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(cachedData);
                }
                else
                {
                    _log.Info($"SaltNameMaster cache miss. Fetching data from database. Key={cacheKey}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetSaltNameMasterList",
                        CommandType.StoredProcedure
                    );

                    saltNames = ConvertDataTableToList(dataTable);

                    if (saltNames.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(saltNames);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"SaltNameMaster data cached permanently. Key={cacheKey}, Count={saltNames.Count}");
                    }
                }

                if (!saltNames.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("No salt names found");
                    return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                        alert.Type,
                        "No salt names found",
                        404
                    );
                }

                _log.Info($"Retrieved {saltNames.Count} salt name(s) from cache");

                return ServiceResult<IEnumerable<Dictionary<string, object>>>.Success(
                    saltNames,
                    "Info",
                    $"{saltNames.Count} salt name(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        /// <summary>
        /// Converts a DataTable to a list of dictionaries so raw stored procedure
        /// columns are returned without requiring a fixed model mapping.
        /// </summary>
        private List<Dictionary<string, object>> ConvertDataTableToList(DataTable dataTable)
        {
            var result = new List<Dictionary<string, object>>();

            if (dataTable == null)
                return result;

            foreach (DataRow row in dataTable.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    rowDict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                result.Add(rowDict);
            }

            return result;
        }


        // ─── Patient Allergy Details ──────────────────────────────────────────────────

        public ServiceResult<CreateUpdatePatientAllergyDetailsResponse> CreateUpdatePatientAllergyDetails(
            CreateUpdatePatientAllergyDetailsRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdatePatientAllergyDetails called. Id={request.Id}, PatientId={request.PatientId}, AllergyId={request.AllergyId}");

                var result = _sqlHelper.ExecuteScalar(
                    "IU_PatientAllergyDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @Id = request.Id,
                        @patientId = request.PatientId,
                        @AllergyId = request.AllergyId,
                        @AllergyName = request.AllergyName ?? (object)DBNull.Value,
                        @AllergyTypeId = request.AllergyTypeId,
                        @AllergyType = request.AllergyType ?? (object)DBNull.Value,
                        @Reaction = request.Reaction ?? (object)DBNull.Value,
                        @Remarks = request.Remarks ?? (object)DBNull.Value,
                        @InteractionSeverity = request.InteractionSeverity ?? (object)DBNull.Value,
                        @ClinicalStatus = request.ClinicalStatus ?? (object)DBNull.Value,
                        @VerificationStatus = request.VerificationStatus ?? (object)DBNull.Value,
                        @SnomedCode = request.SnomedCode ?? (object)DBNull.Value,
                        @NotKnownAllergy = request.NotKnownAllergy,
                        @userId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    }
                );

                int resultValue = Convert.ToInt32(result);

                if (resultValue == -1)
                {
                    var dupAlert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate allergy entry. PatientId={request.PatientId}, AllergyId={request.AllergyId}");
                    return ServiceResult<CreateUpdatePatientAllergyDetailsResponse>.Failure(
                        dupAlert.Type,
                        "This allergy is already recorded for the patient",
                        409
                    );
                }

                if (resultValue > 0)
                {
                    // Clear cache for this patient's allergy list
                    _distributedCache.Remove($"_PatientAllergyDetails_{request.PatientId}");
                    _log.Info($"Cleared PatientAllergyDetails cache for PatientId={request.PatientId}");

                    var responseData = new CreateUpdatePatientAllergyDetailsResponse { Id = resultValue };
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.Id == 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );

                    _log.Info($"PatientAllergyDetails {(request.Id == 0 ? "created" : "updated")} successfully. Id={resultValue}");

                    return ServiceResult<CreateUpdatePatientAllergyDetailsResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        request.Id == 0 ? 201 : 200
                    );
                }

                var failAlert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_FAILED");
                _log.Error($"PatientAllergyDetails operation failed with result: {resultValue}");
                return ServiceResult<CreateUpdatePatientAllergyDetailsResponse>.Failure(
                    failAlert.Type,
                    failAlert.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<CreateUpdatePatientAllergyDetailsResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<object> GetPatientAllergyDetailList(int patientId)
        {
            try
            {
                _log.Info($"GetPatientAllergyDetailList called. PatientId={patientId}");

                string cacheKey = $"_PatientAllergyDetails_{patientId}";

                var cachedData = _distributedCache.GetString(cacheKey);
                List<Dictionary<string, object>> rawData;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"PatientAllergyDetails retrieved from cache. Key={cacheKey}");
                    rawData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(cachedData);
                }
                else
                {
                    _log.Info($"PatientAllergyDetails cache miss. Fetching from database. Key={cacheKey}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetPatientAllergyDetailList",
                        CommandType.StoredProcedure,
                        new { @patientId = patientId }
                    );

                    rawData = dataTable?.AsEnumerable().Select(row =>
                        dataTable.Columns.Cast<DataColumn>().ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        )
                    ).ToList() ?? new List<Dictionary<string, object>>();

                    if (rawData.Any())
                    {
                        var serialized = JsonSerializer.Serialize(rawData);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"PatientAllergyDetails cached. Key={cacheKey}, Count={rawData.Count}");
                    }
                }

                if (!rawData.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No allergy details found for PatientId={patientId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No allergy details found for this patient",
                        404
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                _log.Info($"Retrieved {rawData.Count} allergy record(s) for PatientId={patientId}");

                return ServiceResult<object>.Success(
                    rawData,
                    alert1.Type,
                    $"{rawData.Count} allergy record(s) retrieved successfully",
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

        public ServiceResult<string> DeletePatientAllergyDetails(
            DeletePatientAllergyDetailsRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"DeletePatientAllergyDetails called. Id={request.Id}, PatientId={request.PatientId}");

                _sqlHelper.DML(
                    "U_DeletePatientAllergyDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @id = request.Id,
                        @patientId = request.PatientId,
                        @deactivationRemarks = request.DeactivationRemarks ?? (object)DBNull.Value
                    }
                );

                // Clear cache for this patient's allergy list
                _distributedCache.Remove($"_PatientAllergyDetails_{request.PatientId}");
                _log.Info($"Cleared PatientAllergyDetails cache for PatientId={request.PatientId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                _log.Info($"PatientAllergyDetails deleted successfully. Id={request.Id}, PatientId={request.PatientId}");

                return ServiceResult<string>.Success(
                    "Allergy record deleted successfully",
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

        private const string CACHE_KEY_DiagnosisMaster_All = "_DiagnosisMaster_All";

        public ServiceResult<object> GetDiagnosisMasterList(int? isActive)
        {
            try
            {
                _log.Info($"GetDiagnosisMasterList called. IsActive={isActive?.ToString() ?? "All"}");

                var cachedData = _distributedCache.GetString(CACHE_KEY_DiagnosisMaster_All);
                List<Dictionary<string, object>> allItems;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"DiagnosisMaster data retrieved from cache. Key={CACHE_KEY_DiagnosisMaster_All}");
                    allItems = System.Text.Json.JsonSerializer
                        .Deserialize<List<Dictionary<string, object>>>(cachedData);
                }
                else
                {
                    _log.Info($"DiagnosisMaster cache miss. Fetching from database. Key={CACHE_KEY_DiagnosisMaster_All}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetDiagnosisMasterList",
                        CommandType.StoredProcedure
                    );

                    allItems = dataTable?.AsEnumerable().Select(row =>
                        dataTable.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                col => col.ColumnName,
                                col => row[col] == DBNull.Value ? null : row[col]
                            )
                    ).ToList() ?? new List<Dictionary<string, object>>();

                    if (allItems.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allItems);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(CACHE_KEY_DiagnosisMaster_All, serialized, cacheOptions);
                        _log.Info($"DiagnosisMaster data cached permanently. Key={CACHE_KEY_DiagnosisMaster_All}, Count={allItems.Count}");
                    }
                }

                // In-memory filter by IsActive; null = return all
                if (isActive.HasValue)
                {
                    allItems = allItems.Where(row =>
                    {
                        if (row.TryGetValue("IsActive", out var val) && val != null)
                            return val.ToString() == isActive.Value.ToString();
                        return false;
                    }).ToList();
                    _log.Info($"Filtered by IsActive={isActive.Value}. Count={allItems.Count}");
                }

                if (!allItems.Any())
                {
                    var notFoundAlert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<object>.Failure(
                        notFoundAlert.Type,
                        "No diagnosis records found",
                        404
                    );
                }

                var alert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    allItems,
                    alert.Type,
                    $"{allItems.Count} diagnosis record(s) retrieved successfully",
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

        public ServiceResult<object> CreateUpdateDiagnosisMaster(
            CreateUpdateDiagnosisMasterRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdateDiagnosisMaster called. DiagnosisId={request.DiagnosisId}, DiagnosisName={request.DiagnosisName}");

                var parameters = new SqlParameter[]
                {
            new SqlParameter("@DiagnosisId",   SqlDbType.Int)          { Value = request.DiagnosisId },
            new SqlParameter("@DiagnosisName", SqlDbType.NVarChar, 256){ Value = request.DiagnosisName },
            new SqlParameter("@snomedCode",    SqlDbType.NVarChar, 100){ Value = request.SnomedCode ?? (object)DBNull.Value },
            new SqlParameter("@active",        SqlDbType.Int)          { Value = request.Active },
            new SqlParameter("@userId",        SqlDbType.Int)          { Value = globalValues.userId },
            new SqlParameter("@IpAddress",     SqlDbType.NVarChar, 20) { Value = globalValues.ipAddress ?? (object)DBNull.Value },
            new SqlParameter("@Result",        SqlDbType.Int)          { Direction = ParameterDirection.Output }
                };

                long result = _sqlHelper.RunProcedureInsert("IU_DiagnosisMaster", parameters);

                if (result == -1)
                {
                    var dupAlert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate DiagnosisName: {request.DiagnosisName}");
                    return ServiceResult<object>.Failure(
                        dupAlert.Type,
                        "Diagnosis name already exists",
                        409
                    );
                }

                if (result > 0)
                {
                    _distributedCache.Remove(CACHE_KEY_DiagnosisMaster_All);
                    _log.Info($"Cleared DiagnosisMaster cache. DiagnosisId={result}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.DiagnosisId == 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );
                    return ServiceResult<object>.Success(
                        new { DiagnosisId = result },
                        alert.Type,
                        alert.Message,
                        request.DiagnosisId == 0 ? 201 : 200
                    );
                }

                var failAlert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(failAlert.Type, failAlert.Message, 500);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        private const string CACHE_KEY_ProcedureMaster_All = "_ProcedureMaster_All";

        public ServiceResult<IEnumerable<Dictionary<string, object>>> GetProcedureMasterList(int? isActive)
        {
            try
            {
                _log.Info($"GetProcedureMasterList called. IsActive={isActive?.ToString() ?? "All"}");

                var cachedData = _distributedCache.GetString(CACHE_KEY_ProcedureMaster_All);
                List<Dictionary<string, object>> allItems;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"ProcedureMaster data retrieved from cache. Key={CACHE_KEY_ProcedureMaster_All}");
                    allItems = System.Text.Json.JsonSerializer
                        .Deserialize<List<Dictionary<string, object>>>(cachedData);
                }
                else
                {
                    _log.Info($"ProcedureMaster cache miss. Fetching from database. Key={CACHE_KEY_ProcedureMaster_All}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetProcedureMasterList",
                        CommandType.StoredProcedure
                    );

                    allItems = dataTable?.AsEnumerable().Select(row =>
                        dataTable.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                col => col.ColumnName,
                                col => row[col] == DBNull.Value ? null : row[col]
                            )
                    ).ToList() ?? new List<Dictionary<string, object>>();

                    if (allItems.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allItems);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(CACHE_KEY_ProcedureMaster_All, serialized, cacheOptions);
                        _log.Info($"ProcedureMaster data cached permanently. Key={CACHE_KEY_ProcedureMaster_All}, Count={allItems.Count}");
                    }
                }

                // In-memory filter by IsActive; null = return all
                if (isActive.HasValue)
                {
                    allItems = allItems.Where(row =>
                    {
                        if (row.TryGetValue("IsActive", out var val) && val != null)
                            return val.ToString() == isActive.Value.ToString();
                        return false;
                    }).ToList();
                    _log.Info($"Filtered by IsActive={isActive.Value}. Count={allItems.Count}");
                }

                if (!allItems.Any())
                {
                    var notFoundAlert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                        notFoundAlert.Type,
                        "No procedure records found",
                        404
                    );
                }

                var alert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<IEnumerable<Dictionary<string, object>>>.Success(
                    allItems,
                    alert.Type,
                    $"{allItems.Count} procedure record(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<Dictionary<string, object>>>.Failure(
                    alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> CreateUpdateProcedureMaster(
            CreateUpdateProcedureMasterRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdateProcedureMaster called. ProcedureId={request.ProcedureId}, ProcedureName={request.ProcedureName}");

                var parameters = new SqlParameter[]
                {
            new SqlParameter("@ProcedureId",   SqlDbType.Int)          { Value = request.ProcedureId },
            new SqlParameter("@ProcedureName", SqlDbType.NVarChar, 256){ Value = request.ProcedureName },
            new SqlParameter("@snomedCode",    SqlDbType.NVarChar, 100){ Value = request.SnomedCode ?? (object)DBNull.Value },
            new SqlParameter("@active",        SqlDbType.Int)          { Value = request.Active },
            new SqlParameter("@userId",        SqlDbType.Int)          { Value = globalValues.userId },
            new SqlParameter("@IpAddress",     SqlDbType.NVarChar, 20) { Value = globalValues.ipAddress ?? (object)DBNull.Value },
            new SqlParameter("@Result",        SqlDbType.Int)          { Direction = ParameterDirection.Output }
                };

                long result = _sqlHelper.RunProcedureInsert("IU_ProcedureMaster", parameters);

                if (result == -1)
                {
                    var dupAlert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate ProcedureName: {request.ProcedureName}");
                    return ServiceResult<object>.Failure(
                        dupAlert.Type,
                        "Procedure name already exists",
                        409
                    );
                }

                if (result > 0)
                {
                    _distributedCache.Remove(CACHE_KEY_ProcedureMaster_All);
                    _log.Info($"Cleared ProcedureMaster cache. ProcedureId={result}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.ProcedureId == 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );
                    return ServiceResult<object>.Success(
                        new { ProcedureId = result },
                        alert.Type,
                        alert.Message,
                        request.ProcedureId == 0 ? 201 : 200
                    );
                }

                var failAlert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(failAlert.Type, failAlert.Message, 500);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        private const string CACHE_KEY_EMRSectionMaster_All = "_EMRSectionMaster_All";

        public ServiceResult<object> CreateUpdateEMRSectionMaster(
            CreateUpdateEMRSectionMasterRequest request,
            AllGlobalValues globalValues)
        {
            SqlConnection con = null;
            SqlTransaction tnx = null;
            try
            {
                _log.Info($"CreateUpdateEMRSectionMaster called. SectionId={request.SectionId}, SectionName={request.SectionName}");

                var connectionString = _configuration.GetConnectionString("ConnectionString");
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("Connection string 'ConnectionString' not found.");

                con = new SqlConnection(connectionString);
                con.Open();
                tnx = CustomSqlHelper.getSqlTransaction(con);

                // Step 1 – Insert or Update EMRSectionMaster
                var parameters = new SqlParameter[]
                {
            new SqlParameter("@sectionId",   SqlDbType.Int)          { Value = request.SectionId },
            new SqlParameter("@sectionName", SqlDbType.NVarChar, 256){ Value = request.SectionName },
            new SqlParameter("@displayName", SqlDbType.NVarChar, 256){ Value = request.DisplayName ?? (object)DBNull.Value },
            new SqlParameter("@isActive",    SqlDbType.Int)          { Value = request.IsActive },
            new SqlParameter("@userId",      SqlDbType.Int)          { Value = globalValues.userId },
            new SqlParameter("@IpAddress",   SqlDbType.NVarChar, 20) { Value = globalValues.ipAddress ?? (object)DBNull.Value },
            new SqlParameter("@Result",      SqlDbType.Int)          { Direction = ParameterDirection.Output }
                };

                long sectionResult = _sqlHelper.RunProcedureInsert("IU_EMRSectionMaster", parameters);

                if (sectionResult == -1)
                {
                    tnx.Rollback();
                    var dupAlert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate SectionName: {request.SectionName}");
                    return ServiceResult<object>.Failure(
                        dupAlert.Type,
                        "Section name already exists",
                        409
                    );
                }

                if (sectionResult <= 0)
                {
                    tnx.Rollback();
                    var failAlert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    _log.Error($"IU_EMRSectionMaster returned unexpected result: {sectionResult}");
                    return ServiceResult<object>.Failure(failAlert.Type, failAlert.Message, 500);
                }

                int resolvedSectionId = (int)sectionResult;
                _log.Info($"EMRSectionMaster {(request.SectionId == 0 ? "inserted" : "updated")}. SectionId={resolvedSectionId}");

                // Step 2 – Delete existing header mappings for this section
                _sqlHelper.DML(tnx, "D_EMRSectionHeaderMapping", CommandType.StoredProcedure, new
                {
                    @sectionId = resolvedSectionId
                });
                _log.Info($"Deleted existing EMRSectionHeaderMapping for SectionId={resolvedSectionId}");

                // Step 3 – Insert new header mappings
                int insertedCount = 0;
                if (request.HeaderMappings != null && request.HeaderMappings.Any())
                {
                    foreach (var item in request.HeaderMappings)
                    {
                        _sqlHelper.DML(tnx, "I_EMRSectionHeaderMapping", CommandType.StoredProcedure, new
                        {
                            @sectionId = resolvedSectionId,
                            @headerId = item.HeaderId,
                            @sequenceNo = item.SequenceNo,
                            @userId = globalValues.userId,
                            @IpAddress = globalValues.ipAddress
                        });
                        insertedCount++;
                    }
                }

                tnx.Commit();
                _log.Info($"CreateUpdateEMRSectionMaster committed. SectionId={resolvedSectionId}, MappingsInserted={insertedCount}");

                // Invalidate cache after successful write
                _distributedCache.Remove(CACHE_KEY_EMRSectionMaster_All);
                _log.Info($"Cleared EMRSectionMaster cache. Key={CACHE_KEY_EMRSectionMaster_All}");

                var alert = _messageService.GetMessageAndTypeByAlertCode(
                    request.SectionId == 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                );
                return ServiceResult<object>.Success(
                    new { SectionId = resolvedSectionId, MappingsInserted = insertedCount },
                    alert.Type,
                    alert.Message,
                    request.SectionId == 0 ? 201 : 200
                );
            }
            catch (Exception ex)
            {
                try { tnx?.Rollback(); } catch { /* swallow */ }
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
            finally
            {
                tnx?.Dispose();
                if (con != null)
                {
                    if (con.State == System.Data.ConnectionState.Open) con.Close();
                    con.Dispose();
                }
            }
        }

        public ServiceResult<object> GetEMRSectionMaster(int? isActive)
        {
            try
            {
                _log.Info($"GetEMRSectionMaster called. IsActive={isActive?.ToString() ?? "All"}");

                var cachedData = _distributedCache.GetString(CACHE_KEY_EMRSectionMaster_All);
                List<Dictionary<string, object>> allItems;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"EMRSectionMaster data retrieved from cache. Key={CACHE_KEY_EMRSectionMaster_All}");
                    allItems = System.Text.Json.JsonSerializer
                        .Deserialize<List<Dictionary<string, object>>>(cachedData);
                }
                else
                {
                    _log.Info($"EMRSectionMaster cache miss. Fetching from database. Key={CACHE_KEY_EMRSectionMaster_All}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetEMRSectionMaster",
                        CommandType.StoredProcedure
                    );

                    allItems = dataTable?.AsEnumerable().Select(row =>
                        dataTable.Columns.Cast<DataColumn>().ToDictionary(
                            col => col.ColumnName,
                            col => row[col] == DBNull.Value ? null : row[col]
                        )
                    ).ToList() ?? new List<Dictionary<string, object>>();

                    if (allItems.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allItems);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(CACHE_KEY_EMRSectionMaster_All, serialized, cacheOptions);
                        _log.Info($"EMRSectionMaster cached permanently. Key={CACHE_KEY_EMRSectionMaster_All}, Count={allItems.Count}");
                    }
                }

                // In-memory filter by IsActive; null = return all
                if (isActive.HasValue)
                {
                    allItems = allItems.Where(row =>
                    {
                        if (row.TryGetValue("IsActive", out var val) && val != null)
                            return val.ToString() == isActive.Value.ToString();
                        return false;
                    }).ToList();
                    _log.Info($"Filtered by IsActive={isActive.Value}. Count={allItems.Count}");
                }

                if (!allItems.Any())
                {
                    var notFoundAlert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn("No EMR section records found.");
                    return ServiceResult<object>.Failure(notFoundAlert.Type, "No EMR section records found", 404);
                }

                var alert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    allItems,
                    alert.Type,
                    $"{allItems.Count} EMR section record(s) retrieved successfully",
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

        public ServiceResult<object> GetEMRSectionHeaderMapping(int sectionId)
        {
            try
            {
                _log.Info($"GetEMRSectionHeaderMapping called. SectionId={sectionId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetEMRSectionHeaderMapping",
                    CommandType.StoredProcedure,
                    new { @sectionId = sectionId }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var notFoundAlert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No header mappings found for SectionId={sectionId}");
                    return ServiceResult<object>.Failure(
                        notFoundAlert.Type,
                        "No header mappings found for this section",
                        404
                    );
                }

                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"GetEMRSectionHeaderMapping retrieved {result.Count} record(s) for SectionId={sectionId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    result,
                    alert.Type,
                    $"{result.Count} header mapping(s) retrieved successfully",
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