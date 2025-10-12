using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LocalLinker.Models
{
    public class Reviews
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Review_id { get; set; }

        [StringLength(50)]
        public string? Service_Request_Id { get; set; }

        public int? Rating { get; set; }

        [StringLength(50)]
        public string? Review_Text { get; set; }

        public DateTime? Created_At { get; set; }
    }
}
