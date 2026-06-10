using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public class DoctorAppointments
    {
        public int HospId { get; set; }
        public int BranchId { get; set; }
        public int VisitId { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public DateTime AppDateTime { get; set; }
        public int FTDID { get; set; }
        public string AppointmentType { get; set; }
        public DateTime ValidUpToDate { get; set; }
        public int ValidityDays { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; }
        public string AppointmentDate { get; set; }

        public dynamic Create(ICustomSqlHelper sqlHelper, SqlTransaction tnx)
        {
            return sqlHelper.DML(tnx, "I_DoctorAppointments", CommandType.StoredProcedure, new
            {
                @hospId = HospId,
                @branchId = BranchId,
                @visitId = VisitId,
                @doctorId = DoctorId,
                @patientId = PatientId,
                @appDateTime = AppDateTime,
                @FTDID = FTDID,
                @appointmentType = AppointmentType,
                @validUpToDate = ValidUpToDate,
                @validityDays = ValidityDays,
                @userId = UserId,
                @IpAddress = IpAddress,
                @AppointmentDate = AppointmentDate
            }, new { result = 0 });
        }
    }
}