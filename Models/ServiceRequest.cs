using System.ComponentModel.DataAnnotations;

namespace LocalLinker.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Request_id { get; set; }
        public int? Customer_id { get; set; }
        public int? Service_id { get; set; }
        public int? Location_id { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; } = "Pending";
        public DateTime Entry_Date { get; set; } = DateTime.Now;
        public DateTime? Modify_Date { get; set; }

        public users? Customer { get; set; }
        public Service? Service { get; set; }
    }
}
