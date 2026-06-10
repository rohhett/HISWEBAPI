using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public class PatientInvestigationDetails
    {
        public int HospId { get; set; }
        public int BranchId { get; set; }
        public int VisitId { get; set; }
        public int FTDID { get; set; }
        public int InvestigationId { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public int LabNo { get; set; }
        public int TokenNo { get; set; }
        public int IsUrgent { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; }
        public int BarCode { get; set; }

        public dynamic Create(ICustomSqlHelper sqlHelper, SqlTransaction tnx)
        {
            return sqlHelper.DML(tnx, "I_PatientInvestigationDetails", CommandType.StoredProcedure, new
            {
                @hospId = HospId,
                @branchId = BranchId,
                @visitId = VisitId,
                @FTDID = FTDID,
                @investigationId = InvestigationId,
                @doctorId = DoctorId,
                @patientId = PatientId,
                @labNo = LabNo,
                @TokenNo = TokenNo,
                @isUrgent = IsUrgent,
                @userId = UserId,
                @IpAddress = IpAddress,
                @BarCode = BarCode
            }, new { result = 0 });
        }
    }
}