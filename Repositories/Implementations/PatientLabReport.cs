using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Services;
using log4net;
using Microsoft.Extensions.Caching.Distributed;
using System.Data;
using System.Reflection;

namespace HISWEBAPI.Repositories.Implementations
{
    public class PatientLabReport : Interfaces.IPatientLabReport
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _configuration;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PatientLabReport(
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

        public DataTable GetLabHeaderFooter(int branchId, int typeId = 4, int dummyMode = 0)
        {
            return _sqlHelper.GetDataTable(
                GetReportStoredProcedure("S_GetPatientHeaderMaster", "S_GetPatientHeaderMasterDummy", dummyMode),
                CommandType.StoredProcedure,
                new
                {
                    @branchId = branchId,
                    @typeId = typeId
                }) ?? new DataTable();
        }

        public DataTable GetPatientInvestigationsForReportPrint(int branchId, int isHeaderPng, string patientInvestigationIdList, int userId, int dummyMode = 0)
        {
            return _sqlHelper.GetDataTable(
                GetReportStoredProcedure("S_GetPatientInvestigationsForReportPrint", "S_GetPatientInvestigationsForReportPrintDummy", dummyMode),
                CommandType.StoredProcedure,
                new
                {
                    @PatientInvestigationIdList = patientInvestigationIdList,
                    @branchId = branchId,
                    @isHeaderPNG = isHeaderPng,
                    @PrintBy = userId
                }) ?? new DataTable();
        }

        public DataTable GetPatientTabularReportForPrint(int patientInvestigationId, int dummyMode = 0)
        {
            return _sqlHelper.GetDataTable(
                GetReportStoredProcedure("S_GetPatientTabularResultsForPrint", "S_GetPatientTabularResultsForPrintDummy", dummyMode),
                CommandType.StoredProcedure,
                new { @PTInvstId = patientInvestigationId }) ?? new DataTable();
        }

        public DataTable GetPatientAllergyReportForPrint(int patientInvestigationId, int dummyMode = 0)
        {
            return _sqlHelper.GetDataTable(
                GetReportStoredProcedure("S_GetPatientAllergyResultsForPrint", "S_GetPatientAllergyResultsForPrintDummy", dummyMode),
                CommandType.StoredProcedure,
                new { @PTInvstId = patientInvestigationId }) ?? new DataTable();
        }

        public DataTable GetPatientFreeTextReportForPrint(int patientInvestigationId, int dummyMode = 0)
        {
            return _sqlHelper.GetDataTable(
                GetReportStoredProcedure("S_GetPatientFreeTextResultsForPrint", "S_GetPatientFreeTextResultsForPrintDummy", dummyMode),
                CommandType.StoredProcedure,
                new { @PTInvstId = patientInvestigationId }) ?? new DataTable();
        }

        public DataTable GetPatientHistoReportForPrint(int patientInvestigationId, int dummyMode = 0)
        {
            return _sqlHelper.GetDataTable(
                GetReportStoredProcedure("S_GetPatientHistoResultsForPrint", "S_GetPatientHistoResultsForPrintDummy", dummyMode),
                CommandType.StoredProcedure,
                new { @PTInvstId = patientInvestigationId }) ?? new DataTable();
        }

        public DataTable GetPatientMicroReportForPrint(int patientInvestigationId, int dummyMode = 0)
        {
            return _sqlHelper.GetDataTable(
                GetReportStoredProcedure("S_GetPatientMicroResultsForPrint", "S_GetPatientMicroResultsForPrintDummy", dummyMode),
                CommandType.StoredProcedure,
                new { @PTInvstId = patientInvestigationId }) ?? new DataTable();
        }

        private static string GetReportStoredProcedure(string normalSP, string dummySP, int dummyMode)
        {
            return dummyMode == 2 ? dummySP : normalSP;
        }
    }
}
