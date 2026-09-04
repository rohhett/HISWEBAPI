using HISWEBAPI.DTO;
using HISWEBAPI.Models;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IEMRRepository
    {
        ServiceResult<object> GetAllergyMasterList(int? isActive,int? allergyTypeId);
        ServiceResult<object> CreateUpdateAllergyMaster(CreateUpdateAllergyMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<string> DeleteAllergyMaster(int allergyId, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<Dictionary<string, object>>> GetSaltNameMasterList(string saltName = null);
        ServiceResult<CreateUpdatePatientAllergyDetailsResponse> CreateUpdatePatientAllergyDetails(CreateUpdatePatientAllergyDetailsRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetPatientAllergyDetailList(int patientId);
        ServiceResult<string> DeletePatientAllergyDetails(DeletePatientAllergyDetailsRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetDiagnosisMasterList(int? isActive);
        ServiceResult<object> CreateUpdateDiagnosisMaster(CreateUpdateDiagnosisMasterRequest request, AllGlobalValues globalValues);

        ServiceResult<IEnumerable<Dictionary<string, object>>> GetProcedureMasterList(int? isActive);
        ServiceResult<object> CreateUpdateProcedureMaster(CreateUpdateProcedureMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<object> CreateUpdateEMRSectionMaster(CreateUpdateEMRSectionMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetEMRSectionMaster(int? isActive);
        ServiceResult<object> GetEMRSectionHeaderMapping(int sectionId);
        ServiceResult<object> GetEMRSectionDepartmentMapping(int typeId, int relatedToId);
        ServiceResult<string> SaveEMRSectionDepartmentMapping(SaveEMRSectionDepartmentMappingRequest request, AllGlobalValues globalValues);

        ServiceResult<object> GetEMRSectionScoreFormula(int sectionId);
        ServiceResult<object> SaveEMRSectionScoreFormula(SaveEMRSectionScoreFormulaRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetEMRSectionAttributeCondition(int sectionId);
        ServiceResult<object> SaveEMRSectionAttributeCondition(SaveEMRSectionAttributeConditionRequest request, AllGlobalValues globalValues);
        ServiceResult<object> DeleteEMRSectionAttributeCondition(int id);
        ServiceResult<object> GetEMRHeaderQueryResult(int headerId);
        ServiceResult<string> SaveDoctorFavouriteEMRSections(SaveDoctorFavouriteEMRSectionsRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetDoctorFavouriteEMRSections(int doctorId);
        ServiceResult<CreateUpdateChiefComplaintMasterResponse> CreateUpdateChiefComplaintMaster(CreateUpdateChiefComplaintMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetChiefComplaintMasterList(int? isActive);
        ServiceResult<string> SaveDoctorFavouriteTableEntry(SaveDoctorFavouriteTableEntryRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetDoctorFavouriteTableEntries(int doctorId, int entityId, int recordId);
        ServiceResult<string> DeleteDoctorFavouriteTableEntry(int id);
        ServiceResult<string> DeleteRecordByTableName(int id, string tableName, AllGlobalValues globalValues);
        ServiceResult<string> UploadEMRControlDocument(UploadEMRControlDocumentRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetEMRControlDocumentMapping(int headerId);
        ServiceResult<string> DeleteEMRControlDocumentMapping(int headerId, int documentId, AllGlobalValues globalValues);

        ServiceResult<object> GetDoseMasterList(int? doseId, int? isActive);
        ServiceResult<object> CreateUpdateDoseMaster(CreateUpdateDoseMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<string> UploadEMRDocument(UploadEMRDocumentRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetEMRDocumentMapping(int visitId);
        ServiceResult<object> GetEMRSectionHeaderMappingByDoctorId(int doctorId, int usedForPatientTypeId);

        ServiceResult<SavePatientConsultationResponse> SavePatientConsultation(
            SavePatientConsultationRequest request,
            AllGlobalValues globalValues);

        ServiceResult<object> GetDoctorConsultationByVisitId(int visitId);

        ServiceResult<object> GetPatientVisitDetailsByPatientId(int patientId);
        ServiceResult<object> GetVitalDepartmentMappingByDoctorId(int doctorId);
        ServiceResult<object> GetPatientVital(int patientId, int visitId = 0);

        ServiceResult<IEnumerable<Dictionary<string, object>>> GetTemplateCategoryMasterList();
        ServiceResult<object> CreateUpdateTemplateCategoryMaster(CreateUpdateTemplateCategoryMasterRequest request, AllGlobalValues globalValues);

        ServiceResult<object> CreateUpdateEMRTemplateMaster(CreateUpdateEMRTemplateMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetEMRTemplateMaster(int? isActive);
        ServiceResult<object> GetEMRTemplateSectionMapping(int templateId);
        ServiceResult<object> GetEMRTemplateDepartmentMapping(int typeId, int relatedToId);
        ServiceResult<string> SaveEMRTemplateDepartmentMapping(SaveEMRTemplateDepartmentMappingRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetEMRTemplateSectionMappingByDoctorId(int doctorId, int usedForPatientTypeId,int applicableToId);
        ServiceResult<CreateUpdateCarePlanResponse> CreateUpdateCarePlan(CreateUpdateCarePlanRequest request, AllGlobalValues globalValues);
        ServiceResult<object> GetCarePlanMaster(int doctorId);
        ServiceResult<object> GetCarePlanDetails(int carePlanId);
    }
}