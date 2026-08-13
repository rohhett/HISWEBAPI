using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO
{
    public class TransferIPDPatientBedRequest
    {
        [Required(ErrorMessage = "BillingTypeId is required")]
        public int BillingTypeId { get; set; }

        [Required(ErrorMessage = "RoomTypeId is required")]
        public int RoomTypeId { get; set; }

        [Required(ErrorMessage = "NewBedId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "NewBedId must be greater than 0")]
        public int NewBedId { get; set; }

        [Required(ErrorMessage = "CurrentBedId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "CurrentBedId must be greater than 0")]
        public int CurrentBedId { get; set; }

        [Required(ErrorMessage = "VisitId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "VisitId must be greater than 0")]
        public int VisitId { get; set; }
    }
}