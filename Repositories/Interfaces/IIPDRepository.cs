using HISWEBAPI.DTO;
using HISWEBAPI.Models;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IIPDRepository
    {
        ServiceResult<object> GetIPDPatientBedHistory(int visitId);
        ServiceResult<string> TransferIPDPatientBed(TransferIPDPatientBedRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetIPDPatientDoctorHistory(int visitId);
        ServiceResult<string> TransferIPDPatientDoctor(TransferIPDPatientDoctorRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetIPDPatientCorporateHistory(int visitId);
        ServiceResult<string> UpdateIPDPatientTariffDetails(UpdateIPDPatientTariffDetailsRequest request, AllGlobalValues globalValues);
        ServiceResult<SaveCorporateTransferRequestApprovalResponse> SaveCorporateTransferRequestApproval(SaveCorporateTransferRequestApprovalRequest request, AllGlobalValues globalValues);
        ServiceResult<string> ApproveCorporateTransferRequest(ApproveCorporateTransferRequestRequest request, AllGlobalValues globalValues);
        ServiceResult<string> CancelCorporateTransferRequest(CancelCorporateTransferRequestRequest request, AllGlobalValues globalValues);
        ServiceResult<string> ConfirmCorporateTransferRequest(ConfirmCorporateTransferRequestRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetCorporateTransferRequestListForApproval(string fromDate, string toDate, int branchId, AllGlobalValues globalValues);
        ServiceResult<object> GetCorporateTransferRequestDetailsByCorporateTransferId(int corporateTransferId);
        ServiceResult<object> GetCorporateTransferRequestApprovalDetails(int corporateTransferId);
    }
}