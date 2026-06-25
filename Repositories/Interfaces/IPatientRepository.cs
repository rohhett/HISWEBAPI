using HISWEBAPI.DTO;
using HISWEBAPI.Models;
using System.Collections.Generic;
using System.Data;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IPatientRepository
    {
        ServiceResult<CreateUpdatePatientMasterResponse> CreateUpdatePatientMaster(CreateUpdatePatientMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<string> UploadPatientDocument(UploadPatientDocumentRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<PatientDocumentMappingResponse>> GetPatientDocumentMapping(int patientId);

        ServiceResult<IEnumerable<PatientMasterModel>> GetPatientMaster(int? patientId = null, string? uhid = null, string? contactNumber = null, int? branchId = null);
        ServiceResult<IEnumerable<SearchPatientMasterModel>> SearchPatientMaster(
            int? patientId = null,
            string? uhid = null,
            string? firstName = null,
            string? middleName = null,
            string? lastName = null,
            string? relativeName = null,
            string? dob = null,
            string? contactNumber = null,
            string? emergencyContactNumber = null,
            string? address = null,
            string? registrationDate = null,
            string? ipdNo = null,
            int? branchId = null);
        ServiceResult<ServiceBillingDetailsModel> GetServiceAllDetailsForOPDBilling(int branchId,int corporateId, int doctorId, int serviceItemId, int categoryId, int subCategoryId, int subSubCategoryId, int bedTypeId);
        ServiceResult<SaveOPDBillingResponse> SaveOPDBilling(SaveOPDBillingRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<PackageAllDetailsModel>> GetPackageAllDetails(int packageId);
        ServiceResult<object> GetReceiptDetailsByFTID(int ftid, int isReceipt, int receiptId, AllGlobalValues globalValues);
        ServiceResult<object> GetOPDReceiptList(string visitNo);
        ServiceResult<object> GetOPDCardDetails(long ftid);
        ServiceResult<DataTable> FindDuplicateService(int serviceItemId, int patientId);
        ServiceResult<object> GetInvestigationObservationMappingDetails(int investigationId, int ageInDays, string gender);
        ServiceResult<object> GetUserDiscountRights(int userId);

        ServiceResult<object> GetPatientPreviousDues(int branchId, int patientId);
        ServiceResult<object> GetPatientLastConsultationDetail(int patientId);
        ServiceResult<object> GetServiceItemDetailsByVisitId(int visitId);
        ServiceResult<object> GetPatientBalanceAmountOPD(string uhid);
        ServiceResult<object> GetPatientBalanceAmountIPD(string uhid);
        ServiceResult<object> GetPatientBalanceAmountPharmacy(string uhid);
        ServiceResult<IEnumerable<Dictionary<string, object>>> SearchPatientForConsultation(SearchPatientForConsultationRequest request);
        ServiceResult<object> GetPatientVital(int patientId);
        ServiceResult<string> SavePatientVital(SavePatientVitalRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetPatientObservationResultsTrend(int patientId, int pageNumber, int pageSize);
        ServiceResult<SaveIPDAdmissionResponse> SaveIPDAdmission(SaveIPDAdmissionRequest request, AllGlobalValues globalValues);
        ServiceResult<object> SearchIPDPatient(SearchIPDPatientRequest request, AllGlobalValues globalValues);
        ServiceResult<string> UploadVisitWisePatientDocument(UploadVisitWisePatientDocumentRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetVisitWisePatientDocumentMapping(int documentCategoryId, int visitId, int patientId);
        ServiceResult<SaveOPDBookingResponse> SaveOPDBooking(SaveOPDBookingRequest request, AllGlobalValues globalValues);

        ServiceResult<object> GetOPDBookingDetailsForPaymentCollection(string fromDate, string toDate);
        ServiceResult<object> GetOPDBookingDetailsForDiscountApproval(string fromDate, string toDate);
        ServiceResult<object> GetOPDBookingDetailsByBookingId(int bookingId);


    }
}