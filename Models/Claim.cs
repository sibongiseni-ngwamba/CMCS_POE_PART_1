using System.ComponentModel.DataAnnotations;

namespace CMCS_POE_PART_2.Models
{
    public class Claim
    {
        public int claimID { get; set; }
        [Required]
        public int number_of_sessions { get; set; }
        [Required]
        public int number_of_hours { get; set; }
        [Required]
        public int amount_of_rate { get; set; }
        public decimal TotalAmount { get; set; }
        [Required]
        public string module_name { get; set; } = string.Empty;
        [Required]
        public string faculty_name { get; set; } = string.Empty;
        public string supporting_documents { get; set; } = string.Empty;
        public string claim_status { get; set; } = "Pending";
        public DateTime creating_date { get; set; } = DateTime.Today;
        public int lecturerID { get; set; }
        public string LecturerName { get; set; } = string.Empty;  // For views, populated via join
    }
}
