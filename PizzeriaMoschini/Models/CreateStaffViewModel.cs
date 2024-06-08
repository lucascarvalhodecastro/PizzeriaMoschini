using System.ComponentModel.DataAnnotations;

namespace PizzeriaMoschini.Models
{
    public class CreateStaffViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
