using System;
using System.Data;
using System.Linq;
using System.Reflection;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.DTO;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using log4net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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
    }
}