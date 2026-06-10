using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public enum SMSType
    {
        DirectAppointment = 1,
        DischargeIntimation = 2,
        ReportCollection = 3,
        IPDAdmission = 4,
        IPDDischarge = 5,
        PharmacyReceipt = 6
    }

    public class SMS
    {
        public int branchId { get; set; }
        public int patientId { get; set; }
        public int visitId { get; set; }
        public int patientInvestigationId { get; set; }
        public int appId { get; set; }
        public SMSType SMSType { get; set; }
        public int ftid { get; set; }

        private readonly SqlConnection con;
        private readonly SqlTransaction tnx;
        private readonly bool isLocalConnection;
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IConfiguration _configuration;

       
        public SMS(ICustomSqlHelper sqlHelper, IConfiguration configuration)
        {
            _sqlHelper = sqlHelper;
            _configuration = configuration;

            var connectionString = _configuration.GetConnectionString("ConnectionString");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string 'ConnectionString' not found.");

            this.con = new SqlConnection(connectionString);
            this.con.Open();
            this.tnx = CustomSqlHelper.getSqlTransaction(this.con);
            this.isLocalConnection = true;
        }

        
        public SMS(ICustomSqlHelper sqlHelper, SqlTransaction tran)
        {
            _sqlHelper = sqlHelper;
            this.tnx = tran;
            this.isLocalConnection = false;
        }

      
        public dynamic Insert()
        {
            try
            {
                var result = _sqlHelper.DML(
                    this.tnx,
                    "I_SMSQueue",
                    CommandType.StoredProcedure,
                    new
                    {
                        @branchId = this.branchId,
                        @typeId = (int)this.SMSType,
                        @type = Enum.GetName(typeof(SMSType), this.SMSType),
                        @patientId = this.patientId,
                        @visitId = this.visitId,
                        @appId = this.appId,
                        @patientInvestigationId = this.patientInvestigationId,
                        @ftid = this.ftid
                    },
                    new
                    {
                        result = 0
                    }
                );

                if (this.isLocalConnection)
                    this.tnx.Commit();

                return result;
            }
            catch (Exception)
            {
                if (this.isLocalConnection)
                    this.tnx.Rollback();

                throw;
            }
            finally
            {
                if (this.isLocalConnection)
                {
                    this.tnx.Dispose();
                    if (this.con.State == ConnectionState.Open)
                        this.con.Close();
                    this.con.Dispose();
                }
            }
        }
    }
}