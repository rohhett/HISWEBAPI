using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO
{
    public class CreateUpdateAllergyMasterRequest
    {
        public int AllergyId { get; set; } = 0;

        [Required(ErrorMessage = "AllergyName is required")]
        [StringLength(256, ErrorMessage = "AllergyName cannot exceed 256 characters")]
        public string AllergyName { get; set; }

        [Required(ErrorMessage = "AllergyTypeId is required")]
        public int AllergyTypeId { get; set; }

        [StringLength(100, ErrorMessage = "AllergyType cannot exceed 100 characters")]
        public string AllergyType { get; set; }

        public string SnomedCode { get; set; }

        [Required(ErrorMessage = "Active is required")]
        [Range(0, 1, ErrorMessage = "Active must be 0 or 1")]
        public int Active { get; set; }
    }

    public class DeleteAllergyMasterRequest
    {
        [Required(ErrorMessage = "AllergyId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "AllergyId must be greater than 0")]
        public int AllergyId { get; set; }
    }

    // ─── Patient Allergy Details ──────────────────────────────────────────────────

    public class CreateUpdatePatientAllergyDetailsRequest
    {
        public int Id { get; set; } = 0;

        [Required(ErrorMessage = "PatientId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "PatientId must be greater than 0")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "AllergyId is required")]
        public int AllergyId { get; set; }

        [StringLength(256, ErrorMessage = "AllergyName cannot exceed 256 characters")]
        public string AllergyName { get; set; }

        [Required(ErrorMessage = "AllergyTypeId is required")]
        public int AllergyTypeId { get; set; }

        [StringLength(256, ErrorMessage = "AllergyType cannot exceed 256 characters")]
        public string AllergyType { get; set; }

        [StringLength(512, ErrorMessage = "Reaction cannot exceed 512 characters")]
        public string Reaction { get; set; }

        [StringLength(512, ErrorMessage = "Remarks cannot exceed 512 characters")]
        public string Remarks { get; set; }

        [StringLength(100, ErrorMessage = "InteractionSeverity cannot exceed 100 characters")]
        public string InteractionSeverity { get; set; }

        [StringLength(100, ErrorMessage = "ClinicalStatus cannot exceed 100 characters")]
        public string ClinicalStatus { get; set; }

        [StringLength(100, ErrorMessage = "VerificationStatus cannot exceed 100 characters")]
        public string VerificationStatus { get; set; }

        [StringLength(100, ErrorMessage = "SnomedCode cannot exceed 100 characters")]
        public string SnomedCode { get; set; }

        public int NotKnownAllergy { get; set; } = 0;
    }

    public class CreateUpdatePatientAllergyDetailsResponse
    {
        public int Id { get; set; }
    }

    public class DeletePatientAllergyDetailsRequest
    {
        [Required(ErrorMessage = "Id is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Id must be greater than 0")]
        public int Id { get; set; }

        [Required(ErrorMessage = "PatientId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "PatientId must be greater than 0")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "DeactivationRemarks is required")]
        [StringLength(512, ErrorMessage = "DeactivationRemarks cannot exceed 512 characters")]
        public string DeactivationRemarks { get; set; }
    }

    public class CreateUpdateDiagnosisMasterRequest
    {
        public int DiagnosisId { get; set; } = 0;

        [Required(ErrorMessage = "DiagnosisName is required")]
        [StringLength(256, ErrorMessage = "DiagnosisName cannot exceed 256 characters")]
        public string DiagnosisName { get; set; }

        [StringLength(100, ErrorMessage = "SnomedCode cannot exceed 100 characters")]
        public string? SnomedCode { get; set; }

        [Required(ErrorMessage = "Active is required")]
        [Range(0, 1, ErrorMessage = "Active must be 0 or 1")]
        public int Active { get; set; }
    }

    public class CreateUpdateProcedureMasterRequest
    {
        public int ProcedureId { get; set; } = 0;

        [Required(ErrorMessage = "ProcedureName is required")]
        [StringLength(256, ErrorMessage = "ProcedureName cannot exceed 256 characters")]
        public string ProcedureName { get; set; }

        [StringLength(100, ErrorMessage = "SnomedCode cannot exceed 100 characters")]
        public string? SnomedCode { get; set; }

        [Required(ErrorMessage = "Active is required")]
        [Range(0, 1, ErrorMessage = "Active must be 0 or 1")]
        public int Active { get; set; }
    }


    public class CreateUpdateEMRSectionMasterRequest
    {
        public int SectionId { get; set; } = 0;

        [Required(ErrorMessage = "SectionName is required")]
        [StringLength(256, ErrorMessage = "SectionName cannot exceed 256 characters")]
        public string SectionName { get; set; }

        [StringLength(256, ErrorMessage = "DisplayName cannot exceed 256 characters")]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        [Range(0, 1, ErrorMessage = "IsActive must be 0 or 1")]
        public int IsActive { get; set; }

        public List<EMRSectionHeaderMappingItem> HeaderMappings { get; set; } = new();
    }

    public class EMRSectionHeaderMappingItem
    {
        [Required(ErrorMessage = "HeaderId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "HeaderId must be greater than 0")]
        public int HeaderId { get; set; }

        public int SequenceNo { get; set; } = 0;
    }
}