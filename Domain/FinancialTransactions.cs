using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public enum TnxType { OPDBilling = 1, OPDRefund = 2, IPDBilling = 3, StockReceiving = 4, OPDSell = 5, IPDIssue = 6, OPDReturn = 7, IPDReturn = 8 }

    public class FinancialTransactions
    {
        public int HospId { get; set; }
        public int BranchId { get; set; }
        public int? ReceivingId { get; set; }
        public int? VisitId { get; set; }
        public int? BillId { get; set; }
        public int? PatientId { get; set; }
        public TnxType tnxType { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal NetAmount { get; set; }
        public string Remarks { get; set; }
        public string GstType { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; }
        public string UniqueId { get; set; }
        public string AppointmentDate { get; set; }

        public dynamic Create(ICustomSqlHelper sqlHelper, SqlTransaction tnx)
        {
            return sqlHelper.DML(tnx, "I_FinancialTransactions", CommandType.StoredProcedure, new
            {
                @hospId = HospId,
                @branchId = BranchId,
                @receivingId = ReceivingId,
                @visitId = VisitId,
                @billId = BillId,
                @patientId = PatientId,
                @tnxType = Enum.GetName(typeof(TnxType), this.tnxType),
                @tnxTypeId = this.tnxType,
                @grossAmount = GrossAmount,
                @discountPercentage = DiscountPercentage,
                @discountAmount = DiscountAmount,
                @totalTaxAmount = TotalTaxAmount,
                @roundOff = RoundOff,
                @netAmount = NetAmount,
                @remarks = Remarks,
                @userId = UserId,
                @IpAddress = IpAddress,
                @uniqueId = UniqueId,
                @gstType = GstType,
                @AppointmentDate = AppointmentDate
            }, new { result = 0 });
        }
    }
}