namespace LocalLinker.Models
{
    public class Service
    {
        public int Service_id { get; set; }
        public string? Service_name { get; set; }

        public ICollection<ServiceProvider>? Providers { get; set; }
    }
}
