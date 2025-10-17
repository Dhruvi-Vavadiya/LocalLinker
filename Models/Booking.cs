using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LocalLinker.Models
{
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookingId { get; set; }

        [StringLength(50)]
        public int? Service_Request_Id { get; set; }

        public int? ProviderId { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }  // ENUM: Pending, Confirmed, Completed, Cancelled

        public DateTime? Created_At { get; set; }

        public DateTime? Modifiy_Date { get; set; }
    }
}
