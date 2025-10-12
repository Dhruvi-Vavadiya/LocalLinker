namespace LocalLinker.Models
{
    public class ServiceProvider
    {
        public int Provider_id { get; set; }
        public int? User_id { get; set; }
        public int? Service_id { get; set; }
        public int? Location_id { get; set; }
        public int? Experience_years { get; set; }
        public string? Description { get; set; }
        public bool? IsVerified { get; set; }

        public users? User { get; set; }
        public Service? Service { get; set; }
        public Location? Location { get; set; }
    }
}
