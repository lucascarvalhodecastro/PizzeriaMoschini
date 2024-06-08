using System.ComponentModel.DataAnnotations;

namespace PizzeriaMoschini.Models
{
    public class Staff
    {
        // Primary key for Staff entity
        [Key]
        [Display(Name = "Staff ID")]
        public int StaffID { get; set; }

        [Required]
        public string Name { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        // Navigation property for the reservations made by Staff
        public ICollection<Reservation> Reservations { get; set; }
    }
}
