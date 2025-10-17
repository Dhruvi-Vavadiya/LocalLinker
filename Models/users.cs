using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LocalLinker.Models
{
    public class users
    {
        [Key]
        public int User_id { get; set; }

        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Email { get; set; }

        //[MaxLength(50)]
        public string Password { get; set; }

        [NotMapped]
        public string ConfirmPassword { get; set; }

        [MaxLength(50)]
        public string Phone { get; set; }

        [MaxLength(50)]
        public string UserType { get; set; }  

        public bool Is_Active { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
