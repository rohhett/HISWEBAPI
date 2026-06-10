using HISWEBAPI.DTO;
using HISWEBAPI.Models;
using HISWEBAPI.Configuration;
using System.Collections.Generic;
using System.Data;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IPatientLabReport
    {
        DataTable GetLabHeaderFooter(int branchId, int typeId = 4, int dummyMode = 0);
        DataTable GetPatientInvestigationsForReportPrint(int branchId, int isHeaderPng, string patientInvestigationIdList, int userId, int dummyMode = 0);
        DataTable GetPatientTabularReportForPrint(int patientInvestigationId, int dummyMode = 0);
        DataTable GetPatientAllergyReportForPrint(int patientInvestigationId, int dummyMode = 0);
        DataTable GetPatientFreeTextReportForPrint(int patientInvestigationId, int dummyMode = 0);
        DataTable GetPatientHistoReportForPrint(int patientInvestigationId, int dummyMode = 0);
        DataTable GetPatientMicroReportForPrint(int patientInvestigationId, int dummyMode = 0);
    }
}
