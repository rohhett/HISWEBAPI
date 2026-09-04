using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Domain;
using HISWEBAPI.DTO;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace HISWEBAPI.Repositories.Implementations
{
    public class IPDRepository : IIPDRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Bed status constants (matches U_UpdateBedStatus contract)
        private const int BED_STATUS_AVAILABLE = 0;
        private const int BED_STATUS_PATIENT_ADMITTED = 1;

        public IPDRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService,
            IConfiguration configuration)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
            _configuration = configuration;
        }

        public ServiceResult<object> GetIPDPatientBedHistory(int visitId)
        {
            try
            {
                _log.Info($"GetIPDPatientBedHistory called. VisitId={visitId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetIPDPatientBedHistory",
                    CommandType.StoredProcedure,
                    new { @visitId = visitId }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No bed history found for VisitId={visitId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No bed history found for the given visit",
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

                _log.Info($"IPD bed history retrieved successfully for VisitId={visitId}. Rows={result.Count}");

                return ServiceResult<object>.Success(
                    result,
                    "Info",
                    "Bed history retrieved successfully",
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

        public ServiceResult<string> TransferIPDPatientBed(
            TransferIPDPatientBedRequest request,
            AllGlobalValues globalValues)
        {
            var connectionString = _configuration.GetConnectionString("ConnectionString");
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            var tnx = CustomSqlHelper.getSqlTransaction(con);

            try
            {
                _log.Info($"TransferIPDPatientBed called. VisitId={request.VisitId}, CurrentBedId={request.CurrentBedId}, NewBedId={request.NewBedId}");

                // 1. Update PatientVisitDetails with new billing/room/bed
                _sqlHelper.DML(
                    tnx,
                    "U_TransferIPDPatientBed",
                    CommandType.StoredProcedure,
                    new
                    {
                        @billingTypeId = request.BillingTypeId,
                        @roomTypeId = request.RoomTypeId,
                        @bedId = request.NewBedId,
                        @visitId = request.VisitId,
                        @userId = globalValues.userId,
                        @ipAddress = globalValues.ipAddress
                    }
                );

                // 2. Insert new bed mapping row / close previous current mapping
                _sqlHelper.DML(
                    tnx,
                    "IU_IPDVisitBedMapping",
                    CommandType.StoredProcedure,
                    new
                    {
                        @visitId = request.VisitId,
                        @bedId = request.NewBedId,
                        @isTransfer=1,
                        @userId = globalValues.userId,
                        @ipAddress = globalValues.ipAddress
                    }
                );

                // 3. Free up old bed
                _sqlHelper.DML(
                    tnx,
                    "U_UpdateBedStatus",
                    CommandType.StoredProcedure,
                    new
                    {
                        @bedId = request.CurrentBedId,
                        @currentStatus = BED_STATUS_AVAILABLE
                    }
                );

                // 4. Occupy new bed
                _sqlHelper.DML(
                    tnx,
                    "U_UpdateBedStatus",
                    CommandType.StoredProcedure,
                    new
                    {
                        @bedId = request.NewBedId,
                        @currentStatus = BED_STATUS_PATIENT_ADMITTED
                    }
                );

                tnx.Commit();
                _log.Info($"TransferIPDPatientBed committed. VisitId={request.VisitId}, NewBedId={request.NewBedId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    "Patient bed transferred successfully",
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                try { tnx.Rollback(); } catch { /* swallow rollback exception */ }
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
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

        public ServiceResult<object> GetIPDPatientDoctorHistory(int visitId)
        {
            try
            {
                _log.Info($"GetIPDPatientDoctorHistory called. VisitId={visitId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetIPDPatientDoctorHistory",
                    CommandType.StoredProcedure,
                    new { @visitId = visitId }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No doctor history found for VisitId={visitId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No doctor history found for the given visit",
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

                _log.Info($"IPD doctor history retrieved successfully for VisitId={visitId}. Rows={result.Count}");

                return ServiceResult<object>.Success(
                    result,
                    "Info",
                    "Doctor history retrieved successfully",
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

        public ServiceResult<string> TransferIPDPatientDoctor(
            TransferIPDPatientDoctorRequest request,
            AllGlobalValues globalValues)
        {
            var connectionString = _configuration.GetConnectionString("ConnectionString");
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            var tnx = CustomSqlHelper.getSqlTransaction(con);

            try
            {
                _log.Info($"TransferIPDPatientDoctor called. VisitId={request.VisitId}, PrimaryDoctorId={request.PrimaryDoctorId}, SecondaryCount={request.SecondaryDoctorIds?.Count ?? 0}");

                // 1. Close out the currently active doctor mapping(s) for this visit
                _sqlHelper.DML(
                    tnx,
                    "U_DisableIPDVisitDoctorMapping",
                    CommandType.StoredProcedure,
                    new
                    {
                        @visitId = request.VisitId,
                        @userId = globalValues.userId,
                        @ipAddress = globalValues.ipAddress
                    }
                );

                // 2. Insert secondary (non-primary) doctor mappings
                if (request.SecondaryDoctorIds != null)
                {
                    foreach (var secondaryDoctorId in request.SecondaryDoctorIds)
                    {
                        _sqlHelper.DML(
                            tnx,
                            "I_IPDVisitDoctorMapping",
                            CommandType.StoredProcedure,
                            new
                            {
                                @visitId = request.VisitId,
                                @doctorId = secondaryDoctorId,
                                @isPrimaryDoctor = 0,
                                @userId = globalValues.userId,
                                @ipAddress = globalValues.ipAddress
                            }
                        );
                    }
                }

                // 3. Insert primary doctor mapping
                _sqlHelper.DML(
                    tnx,
                    "I_IPDVisitDoctorMapping",
                    CommandType.StoredProcedure,
                    new
                    {
                        @visitId = request.VisitId,
                        @doctorId = request.PrimaryDoctorId,
                        @isPrimaryDoctor = 1,
                        @userId = globalValues.userId,
                        @ipAddress = globalValues.ipAddress
                    }
                );

                // 4. Doctor-wise IPD sequence number — only create if it doesn't already exist
                int isSeqExists = _sqlHelper.ExecuteScalar(
                    tnx,
                    "S_CheckDoctorVisitSeqExists",
                    CommandType.StoredProcedure,
                    new
                    {
                        @visitId = request.VisitId,
                        @doctorId = request.PrimaryDoctorId
                    }
                );

                

                if (isSeqExists == 0)
                {
                    _sqlHelper.DML(
                        tnx,
                        "I_IPDVisitDoctorSequence",
                        CommandType.StoredProcedure,
                        new
                        {
                            @branchId = request.BranchId,
                            @doctorId = request.PrimaryDoctorId,
                            @visitId = request.VisitId
                        }
                    );
                }

                tnx.Commit();
                _log.Info($"TransferIPDPatientDoctor committed. VisitId={request.VisitId}, PrimaryDoctorId={request.PrimaryDoctorId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    "Patient doctor transferred successfully",
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                try { tnx.Rollback(); } catch { /* swallow rollback exception */ }
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
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

        public ServiceResult<object> GetIPDPatientCorporateHistory(int visitId)
        {
            try
            {
                _log.Info($"GetIPDPatientCorporateHistory called. VisitId={visitId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetIPDPatientCorporateHistory",
                    CommandType.StoredProcedure,
                    new { @visitId = visitId }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No corporate history found for VisitId={visitId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No corporate history found for the given visit",
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

                _log.Info($"IPD corporate history retrieved successfully for VisitId={visitId}. Rows={result.Count}");

                return ServiceResult<object>.Success(
                    result,
                    "Info",
                    "Corporate history retrieved successfully",
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

        public ServiceResult<string> UpdateIPDPatientTariffDetails(
            UpdateIPDPatientTariffDetailsRequest request,
            AllGlobalValues globalValues)
        {
            var connectionString = _configuration.GetConnectionString("ConnectionString");
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            var tnx = CustomSqlHelper.getSqlTransaction(con);

            try
            {
                _log.Info($"UpdateIPDPatientTariffDetails called. VisitId={request.VisitId}, PatientId={request.PatientId}, CorporateId={request.CorporateId}, IsChangeTariff={request.IsChangeTariff}");

                // 1. Update corporate/insurance/relation/card details + push new corporate mapping row
                DateTime TransferDate = Convert.ToDateTime(request.TransferDate);


                _sqlHelper.DML(
                    tnx,
                    "U_UpdateIPDTariffDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @visitId = request.VisitId,
                        @patientId = request.PatientId,
                        @insuranceCompanyId = request.InsuranceCompanyId,
                        @corporateId = request.CorporateId,
                        @relation = (object)request.Relation ?? DBNull.Value,
                        @relativeName = (object)request.RelativeName ?? DBNull.Value,
                        @cardNo = (object)request.CardNo ?? DBNull.Value,
                        @transferDate = TransferDate,
                        @remarks = (object)request.Remarks ?? DBNull.Value,
                        @reasonForTransfer = (object)request.ReasonForTransfer ?? DBNull.Value,
                        @authorizationNumber = (object)request.AuthorizationNumber ?? DBNull.Value,
                        @billingTypeId = request.BillingTypeId,
                        @userId = globalValues.userId,
                        @ipAddress = globalValues.ipAddress
                    }
                );

                // 2. Optionally recalculate tariff rates + roll up billing totals for the visit
                if (request.IsChangeTariff == 1)
                {
                    DateTime fromDate = Convert.ToDateTime(request.ChangeTariffFromDate);
                    DateTime toDate = Convert.ToDateTime(request.ChangeTariffToDate);

                    _sqlHelper.DML(
                        tnx,
                        "U_UpdateIPDTariffAfterCorporateChange",
                        CommandType.StoredProcedure,
                        new
                        {
                            @branchId = request.BranchId,
                            @visitId = request.VisitId,
                            @newCorporateId = request.CorporateId,
                            @changeFromDate = fromDate.ToString("yyyy-MM-dd"),
                            @changeToDate = toDate.ToString("yyyy-MM-dd"),
                            @userId = globalValues.userId,
                            @ipAddress = globalValues.ipAddress
                        }
                    );

                    _sqlHelper.DML(
                        tnx,
                        "U_UpdateIPDBillingByVisitDetails",
                        CommandType.StoredProcedure,
                        new
                        {
                            @visitId = request.VisitId,
                            @userId = globalValues.userId,
                            @ipAddress = globalValues.ipAddress
                        }
                    );
                }

                tnx.Commit();
                _log.Info($"UpdateIPDPatientTariffDetails committed. VisitId={request.VisitId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    "Patient tariff details updated successfully",
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                try { tnx.Rollback(); } catch { /* swallow rollback exception */ }
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
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

        public ServiceResult<SaveCorporateTransferRequestApprovalResponse> SaveCorporateTransferRequestApproval(
    SaveCorporateTransferRequestApprovalRequest request,
    AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"SaveCorporateTransferRequestApproval called. PatientId={request.PatientId}, BranchId={request.BranchId}, VisitId={request.VisitId}");

                // Parse optional tariff-change dates (SP columns are DATE)
                DateTime? changeFromDate = null;
                DateTime? changeToDate = null;

                if (request.IsChangeTariff == 1)
                {
                    if (DateTime.TryParse(request.ChangeFromDate, out var cfd))
                        changeFromDate = cfd;

                    if (DateTime.TryParse(request.ChangeToDate, out var ctd))
                        changeToDate = ctd;
                }

                DateTime TransferDate = Convert.ToDateTime(request.TransferDate);


                // I_CorporateTransferRequestDetails uses a true OUTPUT parameter (no trailing SELECT @Result;),
                // so RunProcedureInsert is required here. No item table for CorporateTransfer — header only.
                long corporateTransferIdResult = _sqlHelper.RunProcedureInsert(
                    "I_CorporateTransferRequestDetails",
                    new IDataParameter[]
                    {
                        new SqlParameter("@BranchId", request.BranchId),
                        new SqlParameter("@RoleId", request.RoleId),
                        new SqlParameter("@PatientId", request.PatientId),
                        new SqlParameter("@VisitId", request.VisitId),
                        new SqlParameter("@TypeId", request.TypeId),
                        new SqlParameter("@InsuranceCompanyId", request.InsuranceCompanyId),
                        new SqlParameter("@CorporateId", request.CorporateId),
                        new SqlParameter("@BillingTypeId", request.BillingTypeId),
                        new SqlParameter("@IsChangeTariff", request.IsChangeTariff),
                        new SqlParameter("@ChangeFromDate", (object)changeFromDate ?? DBNull.Value),
                        new SqlParameter("@ChangeToDate", (object)changeToDate ?? DBNull.Value),
                        new SqlParameter("@Relation", (object)request.Relation ?? DBNull.Value),
                        new SqlParameter("@RelativeName", (object)request.RelativeName ?? DBNull.Value),
                        new SqlParameter("@CardNo", (object)request.CardNo ?? DBNull.Value),

                        new SqlParameter("@TransferDate", TransferDate),
                        new SqlParameter("@Remarks", (object)request.Remarks ?? DBNull.Value),
                        new SqlParameter("@ReasonForTransfer", (object)request.ReasonForTransfer ?? DBNull.Value),
                        new SqlParameter("@AuthorizationNumber", (object)request.AuthorizationNumber ?? DBNull.Value),

                        new SqlParameter("@UserId", globalValues.userId),
                        new SqlParameter("@IpAddress", (object)globalValues.ipAddress ?? DBNull.Value),
                        new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
                    });



                int corporateTransferId = Convert.ToInt32(corporateTransferIdResult);
                _log.Info($"CorporateTransferRequestDetails created. CorporateTransferId={corporateTransferId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<SaveCorporateTransferRequestApprovalResponse>.Success(
                    new SaveCorporateTransferRequestApprovalResponse { CorporateTransferId = corporateTransferId },
                    alert.Type,
                    "Corporate transfer request saved successfully",
                    201
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<SaveCorporateTransferRequestApprovalResponse>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<string> ApproveCorporateTransferRequest(ApproveCorporateTransferRequestRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"ApproveCorporateTransferRequest called. CorporateTransferId={request.CorporateTransferId}, Flag={request.Flag}");

                _sqlHelper.DML(
                    "U_ApproveCorporateTransferRequest",
                    CommandType.StoredProcedure,
                    new
                    {
                        @CorporateTransferId = request.CorporateTransferId,
                        @flag = request.Flag,
                        @ApprovalRemarks = (object)request.ApprovalRemarks ?? DBNull.Value,
                        @UserId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    });

                _log.Info($"ApproveCorporateTransferRequest completed. CorporateTransferId={request.CorporateTransferId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    "Corporate transfer request approval updated successfully",
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<string> CancelCorporateTransferRequest(CancelCorporateTransferRequestRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CancelCorporateTransferRequest called. CorporateTransferId={request.CorporateTransferId}");

                _sqlHelper.DML(
                    "U_CancelCorporateTransferRequest",
                    CommandType.StoredProcedure,
                    new
                    {
                        @CorporateTransferId = request.CorporateTransferId,
                        @CancelReason = (object)request.CancelReason ?? DBNull.Value,
                        @UserId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    });

                _log.Info($"CancelCorporateTransferRequest completed. CorporateTransferId={request.CorporateTransferId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    "Corporate transfer request cancelled successfully",
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<string> ConfirmCorporateTransferRequest(ConfirmCorporateTransferRequestRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"ConfirmCorporateTransferRequest called. CorporateTransferId={request.CorporateTransferId}");

                _sqlHelper.DML(
                    "U_ConfirmCorporateTransferRequest",
                    CommandType.StoredProcedure,
                    new
                    {
                        @CorporateTransferId = request.CorporateTransferId,
                        @UserId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    });

                _log.Info($"ConfirmCorporateTransferRequest completed. CorporateTransferId={request.CorporateTransferId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    "Corporate transfer marked as created successfully",
                    alert.Type,
                    alert.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> GetCorporateTransferRequestListForApproval(string fromDate, string toDate, int branchId, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"GetCorporateTransferRequestListForApproval called. FromDate={fromDate}, ToDate={toDate}, BranchId={branchId}");

                if (!DateTime.TryParse(fromDate, out DateTime parsedFromDate))
                {
                    var alertDate = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return ServiceResult<object>.Failure(alertDate.Type, "Invalid FromDate format", 400);
                }

                if (!DateTime.TryParse(toDate, out DateTime parsedToDate))
                {
                    var alertDate = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return ServiceResult<object>.Failure(alertDate.Type, "Invalid ToDate format", 400);
                }

                var dataTable = _sqlHelper.GetDataTable(
                    "S_CorporateTransferRequestDetailsForApproval",
                    CommandType.StoredProcedure,
                    new
                    {
                        @fromDate = parsedFromDate.ToString("yyyy-MM-dd"),
                        @toDate = parsedToDate.ToString("yyyy-MM-dd"),
                        @branchId = branchId,
                        @userId = globalValues.userId
                    });

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("GetCorporateTransferRequestListForApproval: no records found");
                    return ServiceResult<object>.Failure(alert.Type, "No corporate transfer requests found", 404);
                }

                var rows = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"GetCorporateTransferRequestListForApproval retrieved {rows.Count} record(s)");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(rows, alert1.Type, $"{rows.Count} corporate transfer request(s) retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> GetCorporateTransferRequestDetailsByCorporateTransferId(int corporateTransferId)
        {
            try
            {
                _log.Info($"GetCorporateTransferRequestDetailsByCorporateTransferId called. CorporateTransferId={corporateTransferId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_CorporateTransferRequestDetailsByCorporateTransferId",
                    CommandType.StoredProcedure,
                    new { @CorporateTransferId = corporateTransferId });

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No corporate transfer details found for CorporateTransferId={corporateTransferId}");
                    return ServiceResult<object>.Failure(alert.Type, "No corporate transfer details found", 404);
                }

                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"Corporate transfer details retrieved successfully for CorporateTransferId={corporateTransferId}. Rows={result.Count}");

                return ServiceResult<object>.Success(result, "Info", "Corporate transfer details retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> GetCorporateTransferRequestApprovalDetails(int corporateTransferId)
        {
            try
            {
                _log.Info($"GetCorporateTransferRequestApprovalDetails called. CorporateTransferId={corporateTransferId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetCorporateTransferRequestApprovalDetails",
                    CommandType.StoredProcedure,
                    new { @CorporateTransferId = corporateTransferId });

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No approval details found for CorporateTransferId={corporateTransferId}");
                    return ServiceResult<object>.Failure(alert.Type, "No approval details found", 404);
                }

                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"Approval details retrieved successfully for CorporateTransferId={corporateTransferId}");

                return ServiceResult<object>.Success(result, "Info", "Approval details retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<object>.Failure(alert.Type, alert.Message, 500);
            }
        }

        public ServiceResult<object> GetCorporateTransferRequestDetailsByVisitId(int visitId)
        {
            try
            {
                _log.Info($"GetCorporateTransferRequestDetailsByVisitId called. VisitId={visitId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_CorporateTransferRequestDetailsByVisitId",
                    CommandType.StoredProcedure,
                    new { @visitId = visitId }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No corporate transfer request details found for VisitId={visitId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No corporate transfer request details found for the given visit",
                        404
                    );
                }

                // Raw DataTable -> List<Dictionary<string,object>> (no model mapping),
                // so any new columns added to the SP surface automatically.
                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"GetCorporateTransferRequestDetailsByVisitId retrieved {result.Count} record(s) for VisitId={visitId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    result,
                    alert1.Type,
                    $"{result.Count} record(s) retrieved successfully",
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


        public ServiceResult<SaveIPDBillingResponse> SaveIPDBilling(
    SaveIPDBillingRequest request,
    AllGlobalValues globalValues)
        {
            var connectionString = _configuration.GetConnectionString("ConnectionString");
            SqlConnection con = new SqlConnection(connectionString);
            con.Open();
            var tnx = CustomSqlHelper.getSqlTransaction(con);

            try
            {
                _log.Info($"SaveIPDBilling called. PatientId={request.VisitDetails.PatientId}, VisitId={request.VisitDetails.VisitId}, BranchId={request.VisitDetails.BranchId}");

                var v = request.VisitDetails;
                int visitId = v.VisitId;

                decimal totalPaidAmount = 0;
                if (request.PaymentDetails?.Count > 0)
                {
                    totalPaidAmount = request.PaymentDetails.Sum(p => p.Amount);
                }

                // ── 1. PatientBillDetails ────────────────────────────────────────────
                var pbd = new PatientBillDetails
                {
                    HospId = globalValues.hospId,
                    BranchId = v.BranchId,
                    RoleId = v.RoleId,
                    PatientId = v.PatientId,
                    VisitId = visitId,
                    TypeId = 2,                                // 2 = IPD
                    TotalBillAmount = v.GrossBillAmount,
                    TotalDiscountPerOnBill = v.TotalDiscPerOnBill,
                    TotalDiscountAmountOnBill = v.TotalDiscAmtOnBill,
                    DiscountApprovedById = v.DiscApprovedById > 0 ? v.DiscApprovedById : (int?)null,
                    DiscountReason = v.DiscountReason,
                    RoundOff = v.RoundOff,
                    TotalPayableAmount = v.NetAmount,
                    TotalPaidAmount = totalPaidAmount,
                    TotalBalanceAmount = v.NetAmount - totalPaidAmount,
                    TotalPatientPayableAmount = v.NetAmount,
                    TotalCorporatePayableAmount = 0,
                    TotalPatientPaidAmount = totalPaidAmount,
                    TotalCorporatePaidAmount = 0,
                    IsSupplementaryBill = v.IsSupplementaryBill,
                    UserId = globalValues.userId,
                    IpAddress = globalValues.ipAddress
                };

                int billId = Convert.ToInt32(pbd.Create(_sqlHelper, tnx));
                _log.Info($"PatientBillDetails created. BillId={billId}");

                // ── 2. FinancialTransactions ─────────────────────────────────────────
                var ft = new FinancialTransactions
                {
                    HospId = globalValues.hospId,
                    BranchId = v.BranchId,
                    VisitId = visitId,
                    BillId = billId,
                    PatientId = v.PatientId,
                    tnxType = TnxType.IPDBilling,
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

                    if (!DateTime.TryParse(item.BillingDate, out DateTime parsedBillingDate))
                    {
                        var alertDate = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                        return ServiceResult<SaveIPDBillingResponse>.Failure(alertDate.Type, "Invalid Billing Date format", 400);
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
                        Remarks = item.Remarks,
                        CorporateAlias = item.CorporateAlias,
                        CorporateCode = item.CorporateCode,
                        DoctorId = item.DoctorId > 0 ? item.DoctorId : (int?)null,
                        PerformingDoctorId = item.PerformingDoctorId > 0 ? item.PerformingDoctorId : (int?)null,
                        CorporateId = v.CorporateId > 0 ? v.CorporateId : (int?)null,
                        Rate = item.Rate,
                        Qty = item.Qty,
                        GrossAmt = item.GrossAmt,
                        DiscPer = itemDiscPer,
                        DiscAmt = itemDiscAmt,
                        NetAmt = itemNetAmt,
                        IsCorporateNonPayable = item.IsNonPayable,
                        DiscountReason = itemDiscReason,
                        RateListId = item.RateListId,
                      
                        BillingDate = parsedBillingDate.ToString("yyyy-MM-dd"),
                        UserId = globalValues.userId,
                        IpAddress = globalValues.ipAddress
                    };

                    int ftdId = Convert.ToInt32(ftd.Create(_sqlHelper, tnx));
                    _log.Info($"FinancialTransactionDetails created. FTDId={ftdId}, ServiceItemId={item.ServiceItemId}");

                    // ── 3b. Investigation (CategoryTypeId == 3) ──────────────────────
                    if (item.CategoryTypeId == 3)
                    {
                        // Barcode – one per unique SampleTypeId (Pathology only)
                        int barCode = 0;
                        if (item.LabTypeId == 1 && item.SampleTypeId > 0)
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
                        if (item.LabTypeId == 1 || item.LabTypeId == 2 || item.LabTypeId == 3)
                        {
                            tokenNo = Convert.ToInt32(_sqlHelper.ExecuteScalar(
                                tnx,
                                "S_GetLabTokenNo",
                                CommandType.StoredProcedure,
                                new { @branchId = v.BranchId, @SubCategoryId = item.LabTypeId },
                                new { result = 0 }));
                        }

                        if (pathologyTokenNo == 0 && item.LabTypeId == 1) pathologyTokenNo = tokenNo;
                        if (radiologyTokenNo == 0 && item.LabTypeId == 2) radiologyTokenNo = tokenNo;
                        if (cardiologyTokenNo == 0 && item.LabTypeId == 3) cardiologyTokenNo = tokenNo;

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
                            TokenNo = item.LabTypeId == 1 ? pathologyTokenNo
                                    : item.LabTypeId == 2 ? radiologyTokenNo
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
                bool isReceipt = false;
                if (totalPaidAmount > 0 && v.IsSupplementaryBill==1)
                {
                    var receipt = new Receipts
                    {
                        HospId = globalValues.hospId,
                        BranchId = v.BranchId,
                        RoleId = v.RoleId,
                        FTID = ftid,
                        VisitId = visitId,
                        PatientId = v.PatientId,
                        Amount = totalPaidAmount,
                        IsCopaymentReceipt = request.PaymentDetails[0].IsCopaymentReceipt,
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

                        if (p.IsPatientAdvanceAmount == 1)
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
                _log.Info($"SaveIPDBilling committed. VisitId={visitId}, FTID={ftid}, ReceiptId={receiptId}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<SaveIPDBillingResponse>.Success(
                    new SaveIPDBillingResponse
                    {
                        VisitId = visitId,
                        FTID = ftid,
                        ReceiptId = receiptId,
                        IsReceipt = isReceipt,
                        IsLabInvestigations = isLabInvestigations
                    },
                    alert.Type,
                    "IPD Billing saved successfully",
                    201
                );
            }
            catch (Exception ex)
            {
                try { tnx.Rollback(); } catch { /* swallow rollback exception */ }
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<SaveIPDBillingResponse>.Failure(
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

        public ServiceResult<object> GetIPDBillingSummary(int branchId, int visitId)
        {
            try
            {
                _log.Info($"GetIPDBillingSummary called. BranchId={branchId}, VisitId={visitId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetIPDBillingDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @branchId = branchId,
                        @visitId = visitId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No IPD billing details found for VisitId={visitId}, BranchId={branchId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No IPD billing details found",
                        404
                    );
                }

                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"GetIPDBillingSummary retrieved {result.Count} record(s) for VisitId={visitId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    result,
                    alert1.Type,
                    $"{result.Count} billing item(s) retrieved successfully",
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

        public ServiceResult<object> GetIPDPatientBillAmounts(int visitId, int patientId)
        {
            try
            {
                _log.Info($"GetIPDPatientBillAmounts called. VisitId={visitId}, PatientId={patientId}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetIPDPatientBillAmounts",
                    CommandType.StoredProcedure,
                    new
                    {
                        @visitId = visitId,
                        @patientId = patientId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No IPD patient bill amounts found for VisitId={visitId}, PatientId={patientId}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No IPD patient bill amounts found",
                        404
                    );
                }

                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"GetIPDPatientBillAmounts retrieved {result.Count} record(s) for VisitId={visitId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    result,
                    alert1.Type,
                    "IPD patient bill amounts retrieved successfully",
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
        public ServiceResult<object> GetIPDPatientOrderDetails(int ftid, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"GetIPDPatientOrderDetails called. FTID={ftid}");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetIPDPatientOrderDetails",
                    CommandType.StoredProcedure,
                    new
                    {
                        @FTID = ftid,
                        @printUserId = globalValues.userId
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No IPD patient order details found for FTID={ftid}");
                    return ServiceResult<object>.Failure(
                        alert.Type,
                        "No IPD patient order details found",
                        404
                    );
                }

                var result = dataTable.AsEnumerable().Select(row =>
                    dataTable.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    )
                ).ToList();

                _log.Info($"GetIPDPatientOrderDetails retrieved {result.Count} record(s) for FTID={ftid}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                return ServiceResult<object>.Success(
                    result,
                    alert1.Type,
                    $"{result.Count} order detail(s) retrieved successfully",
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