using System.ComponentModel.DataAnnotations;

namespace LocalLinker.Models
{
    public class Service
    {
        [Key]
        public int Service_id { get; set; }
        public string? Service_name { get; set; }

        public ICollection<ServiceProvider>? Providers { get; set; }
    }
}
