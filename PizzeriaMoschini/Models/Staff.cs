using System.ComponentModel.DataAnnotations;

namespace PizzeriaMoschini.Models
{
    public class Staff
    {
        [Key]
        [Display(Name = "Staff ID")]
        public int StaffID { get; set; }

        [Required]
        public string Name { get; set; }

        public string Email { get; set; }
    }
}
