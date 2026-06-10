using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public class PatientVisitDetails
    {
        public int HospId { get; set; }
        public int BranchId { get; set; }
        public int PatientId { get; set; }
        public string Uhid { get; set; }
        public string Type { get; set; }
        public int TypeId { get; set; }
        public string CurrentAge { get; set; }
        public int DoctorId { get; set; }
        public int CorporateId { get; set; }
        public int InsuranceCompanyId { get; set; }
        public int? ReferDoctorId { get; set; }
        public decimal TotalBillAmount { get; set; }
        public decimal TotalDiscountPerOnBill { get; set; }
        public decimal TotalDiscountAmountOnBill { get; set; }
        public int? DiscountApprovedById { get; set; }
        public string DiscountReason { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalPayableAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public decimal TotalPatientPayableAmount { get; set; }
        public decimal TotalCorporatePayableAmount { get; set; }
        public decimal TotalPatientPaidAmount { get; set; }
        public decimal TotalCorporatePaidAmount { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; }
        public string UniqueId { get; set; }
        public string AdmissionType { get; set; }
        public int BillingTypeId { get; set; }
        public int RoomTypeId { get; set; }
        public int BedId { get; set; }
        public string AdmissionDate { get; set; }
        public string AdmissionTime { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public int IsReturn { get; set; }
        public string Mlc { get; set; }
        public string Pi { get; set; }
        public string Remark { get; set; }
        public string PolicyNo { get; set; }
        public string PolicyCardNo { get; set; }
        public string ExpiryDate { get; set; }
        public string CardHolder { get; set; }
        public string ReferalNo { get; set; }
        public string ReferalDate { get; set; }
        public int TokenNo { get; set; }
        public string IsDummyPatient { get; set; }
        public string AppointmentDate { get; set; }
        public int ProId { get; set; }
        public string ProName { get; set; }
        public int IsSendMRD { get; set; }

        // IPD-specific fields
        public string AttendantRelation { get; set; }
        public string AttendantName { get; set; }
        public string AttendantContactNumber { get; set; }
        public int? HandleWithCare { get; set; }
        public int? NameMasking { get; set; }

        public dynamic Create(ICustomSqlHelper sqlHelper, SqlTransaction tnx)
        {
            return sqlHelper.DML(tnx, "I_PatientVisitDetails", CommandType.StoredProcedure, new
            {
                @hospId = HospId,
                @branchId = BranchId,
                @patientId = PatientId,
                @uhid = Uhid,
                @type = Type,
                @typeId = TypeId,
                @currentAge = CurrentAge,
                @doctorId = DoctorId,
                @corporateId = CorporateId,
                @insuranceCompanyId = InsuranceCompanyId,
                @referDoctorId = ReferDoctorId,
                @totalBillAmount = TotalBillAmount,
                @totalDiscountPerOnBill = TotalDiscountPerOnBill,
                @totalDiscountAmountOnBill = TotalDiscountAmountOnBill,
                @discountApprovedById = DiscountApprovedById,
                @discountReason = DiscountReason,
                @roundOff = RoundOff,
                @totalPayableAmount = TotalPayableAmount,
                @totalPaidAmount = TotalPaidAmount,
                @totalBalanceAmount = TotalBalanceAmount,
                @totalPatientPayableAmount = TotalPatientPayableAmount,
                @totalCorporatePayableAmount = TotalCorporatePayableAmount,
                @totalPatientPaidAmount = TotalPatientPaidAmount,
                @totalCorporatePaidAmount = TotalCorporatePaidAmount,
                @userId = UserId,
                @IpAddress = IpAddress,
                @uniqueId = UniqueId,
                @admissionType = AdmissionType,
                @billingTypeId = BillingTypeId,
                @roomTypeId = RoomTypeId,
                @bedId = BedId,
                @admissionDate = AdmissionDate,
                @admissionTime = AdmissionTime,
                @statusId = StatusId,
                @status = Status,
                @isReturn = IsReturn,
                @Remark = Remark,
                @mlc = Mlc,
                @pi = Pi,
                @PolicyNo = PolicyNo,
                @PolicyCardNo = PolicyCardNo,
                @ExpiryDate = ExpiryDate,
                @CardHolder = CardHolder,
                @ReferalNo = ReferalNo,
                @ReferalDate = ReferalDate,
                @TokenNo = TokenNo,
                @IsDummyPatient = IsDummyPatient,
                @AppointmentDate = AppointmentDate,
                @ProId = ProId,
                @ProName = ProName,
                @IsSendMRD = IsSendMRD,
                @AttendantRelation = AttendantRelation,
                @AttendantName = AttendantName,
                @AttendantContactNumber = AttendantContactNumber,
                @HandleWithCare = HandleWithCare,
                @NameMasking = NameMasking
            }, new { result = 0 });
        }
    }
}