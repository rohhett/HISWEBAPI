using HISWEBAPI.DTO;
using HISWEBAPI.Models;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IEMRRepository
    {
        ServiceResult<object> GetAllergyMasterList(int? isActive,int? allergyTypeId);
        ServiceResult<object> CreateUpdateAllergyMaster(CreateUpdateAllergyMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<string> DeleteAllergyMaster(int allergyId, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<Dictionary<string, object>>> GetSaltNameMasterList();
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


    }
}