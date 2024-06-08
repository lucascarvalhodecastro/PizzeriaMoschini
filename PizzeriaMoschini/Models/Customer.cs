using System.ComponentModel.DataAnnotations;

namespace PizzeriaMoschini.Models
{
    public class Customer
    {
        // Primary key for Customer entity
        [Key]
        [Display(Name = "Customer ID")]
        public int CustomerID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        public string Email { get; set; }

        // Navigation property for the reservations made by Customer
        public ICollection<Reservation> Reservations { get; set; }
    }
}
