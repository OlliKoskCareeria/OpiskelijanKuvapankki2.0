using System.ComponentModel.DataAnnotations;

namespace OpiskelijanKuvapankki2_0.Models
{
    public class EmailVerification
    {
        [Key]
        public int LoginID { get; set; }
        [Required]
        public string Code { get; set; }
        public DateTime TimeValid { get; set; }
        public int Attempts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}
