using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public class PatientBillDetails
    {
        public int HospId { get; set; }
        public int BranchId { get; set; }
        public int RoleId { get; set; }
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public int TypeId { get; set; }
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
        public decimal TotalAmountSettledWithPatientAdvance { get; set; }
        public int IsSupplementaryBill { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; }

        public long Create(ICustomSqlHelper sqlHelper, SqlTransaction tnx)
        {
            var parameters = new SqlParameter[]
            {
                new SqlParameter("@hospId",                    SqlDbType.Int)     { Value = HospId },
                new SqlParameter("@branchId",                  SqlDbType.Int)     { Value = BranchId },
                new SqlParameter("@roleId",                    SqlDbType.Int)     { Value = RoleId },
                new SqlParameter("@patientId",                 SqlDbType.Int)     { Value = PatientId },
                new SqlParameter("@visitId",                   SqlDbType.Int)     { Value = VisitId },
                new SqlParameter("@typeId",                    SqlDbType.Int)     { Value = TypeId },
                new SqlParameter("@totalBillAmount",           SqlDbType.Decimal) { Value = TotalBillAmount,            Precision = 16, Scale = 6 },
                new SqlParameter("@totalDiscountPerOnBill",    SqlDbType.Decimal) { Value = TotalDiscountPerOnBill,     Precision = 16, Scale = 6 },
                new SqlParameter("@totalDiscountAmountOnBill", SqlDbType.Decimal) { Value = TotalDiscountAmountOnBill,  Precision = 16, Scale = 6 },
                new SqlParameter("@discountApprovedById",      SqlDbType.Int)     { Value = (object)DiscountApprovedById ?? DBNull.Value },
                new SqlParameter("@discountReason",            SqlDbType.NVarChar, 256) { Value = (object)DiscountReason ?? DBNull.Value },
                new SqlParameter("@roundOff",                  SqlDbType.Decimal) { Value = RoundOff,                  Precision = 16, Scale = 6 },
                new SqlParameter("@totalPayableAmount",        SqlDbType.Decimal) { Value = TotalPayableAmount,         Precision = 16, Scale = 6 },
                new SqlParameter("@totalPaidAmount",           SqlDbType.Decimal) { Value = TotalPaidAmount,            Precision = 16, Scale = 6 },
                new SqlParameter("@totalBalanceAmount",        SqlDbType.Decimal) { Value = TotalBalanceAmount,         Precision = 16, Scale = 6 },
                new SqlParameter("@totalPatientPayableAmount", SqlDbType.Decimal) { Value = TotalPatientPayableAmount,  Precision = 16, Scale = 6 },
                new SqlParameter("@totalCorporatePayableAmount",SqlDbType.Decimal){ Value = TotalCorporatePayableAmount, Precision = 16, Scale = 6 },
                new SqlParameter("@totalPatientPaidAmount",    SqlDbType.Decimal) { Value = TotalPatientPaidAmount,     Precision = 16, Scale = 6 },
                new SqlParameter("@totalCorporatePaidAmount",  SqlDbType.Decimal) { Value = TotalCorporatePaidAmount,   Precision = 16, Scale = 6 },
                new SqlParameter("@totalAmountSettledWithPatientAdvance",  SqlDbType.Decimal) { Value = TotalAmountSettledWithPatientAdvance,   Precision = 16, Scale = 6 },
                new SqlParameter("@isSupplementaryBill",                    SqlDbType.Int)     { Value = IsSupplementaryBill },
                new SqlParameter("@userId",                    SqlDbType.Int)     { Value = UserId },
                new SqlParameter("@IpAddress",                 SqlDbType.NVarChar, 20) { Value = IpAddress ?? string.Empty },
                new SqlParameter("@Result",                    SqlDbType.Int)     { Direction = ParameterDirection.Output }
            };

            return sqlHelper.RunProcedureInsert("I_PatientBillDetails", parameters);
        }
    }
}