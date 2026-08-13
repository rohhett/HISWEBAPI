using HISWEBAPI.DTO;
using HISWEBAPI.Models;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IIPDRepository
    {
        ServiceResult<object> GetIPDPatientBedHistory(int visitId);
        ServiceResult<string> TransferIPDPatientBed(TransferIPDPatientBedRequest request, AllGlobalValues globalValues);
    }
}