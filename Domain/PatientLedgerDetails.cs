using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Domain
{
    public enum LedgerTransactionType
    {
        Credit = 1,
        Debit = 2,
        Refund=3
    }

    public class PatientLedgerDetails
    {
        public int PatientId { get; set; }
        public int LedgerId { get; set; }
        public LedgerTransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public int VisitId { get; set; } = 0;
        public long BillId { get; set; } = 0;
        public int ReceiptId { get; set; } = 0;
        public int UserId { get; set; }
        public string IpAddress { get; set; }

        public dynamic Create(ICustomSqlHelper sqlHelper, SqlTransaction tnx)
        {
            return sqlHelper.DML(tnx, "I_PatientLedgerDetails", CommandType.StoredProcedure, new
            {
                @ledgerId = LedgerId,
                @patientId = PatientId,
                @transactionTypeId = (int)TransactionType,
                @transactionType = Enum.GetName(typeof(LedgerTransactionType), TransactionType),
                @amount = Amount,
                @visitId = VisitId,
                @billId = BillId,
                @receiptId = ReceiptId,
                @userId = UserId,
                @IpAddress = IpAddress
            }, new { result = 0 });
        }
    }
}