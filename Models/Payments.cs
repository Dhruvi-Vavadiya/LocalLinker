using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalLinker.Models
{
    public class Payments
    {
        [Key]
        public int Payment_id { get; set; }

        [Required]
        public int Booking_id { get; set; }

        [Required]
        public int User_id { get; set; }

        [Required]
        [StringLength(100)]
        public string Razorpay_Order_Id { get; set; }

        [StringLength(100)]
        public string? Razorpay_Payment_Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(30)]
        public string Payment_Status { get; set; } = "Pending";

        

        public DateTime Created_At { get; set; } = DateTime.Now;

        public DateTime? Modified_At { get; set; }
    }
}
