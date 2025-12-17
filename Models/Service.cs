using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalLinker.Models
{
    public class Service
    {
        [Key]
        public int Service_id { get; set; }
        public string? Service_name { get; set; }

        public int? price { get; set; }
        public string? Image { get; set; }

        public bool? IsActive { get; set; } = true;

        public string? description { get; set; }
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public ICollection<ServiceProvider>? Providers { get; set; }
    }
}
