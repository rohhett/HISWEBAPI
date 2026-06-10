using HISWEBAPI.DTO;
using HISWEBAPI.Models;

namespace HISWEBAPI.Services.Interfaces
{
    public interface IPatientInvestigationReportPdfService
    {
        PatientInvestigationReportPdfResult GenerateReport(PatientInvestigationReportRequest request, AllGlobalValues globalValues, string baseUrl);
    }
}
