using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalLinker.Models
{
    public class ServiceProvider
    {
        [Key]
        public int Provider_id { get; set; }
        public int? User_id { get; set; }
        public int? Service_id { get; set; }
        public int? Location_id { get; set; }
        public int? Experience_years { get; set; }
        public string? Description { get; set; }
        public bool? IsVerified { get; set; }

        [ForeignKey("User_id")]
        public users? User { get; set; }

        [ForeignKey("Service_id")]
        public Service? Service { get; set; }

        [ForeignKey("Location_id")]
        public Location? Location { get; set; }
    }
}
