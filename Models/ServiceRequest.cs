using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalLinker.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Request_id { get; set; }
        [ForeignKey("Customer")]
        public int? Customer_id { get; set; }

        [ForeignKey("Service")]
        public int? Service_id { get; set; }

        [ForeignKey("Location")]
        public int? Location_id { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; } = "Pending";
        public DateTime Entry_Date { get; set; } = DateTime.Now;
        public DateTime? Modifiy_Date { get; set; }
        //public users? Customer { get; set; }
        //public Service? Service { get; set; }
    }
}
