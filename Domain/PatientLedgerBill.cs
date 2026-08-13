using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public enum LedgerBillTransactionType
    {
        Credit = 1,
        Debit = 2,
        Refund=3
    }

    public class PatientLedgerBill
    {
        public int PatientId { get; set; }
        public int LedgerId { get; set; } = 0;
        public LedgerBillTransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public int UserId { get; set; }
        public string IpAddress { get; set; }

        public dynamic Create(ICustomSqlHelper sqlHelper, SqlTransaction tnx)
        {
            return sqlHelper.DML(tnx, "IU_PatientLedgerBill", CommandType.StoredProcedure, new
            {
                @ledgerId = LedgerId,
                @patientId = PatientId,
                @transactionTypeId = (int)TransactionType,
                @transactionType = Enum.GetName(typeof(LedgerBillTransactionType), TransactionType),
                @amount = Amount,
                @userId = UserId,
                @IpAddress = IpAddress
            }, new { result = 0 });
        }
    }
}