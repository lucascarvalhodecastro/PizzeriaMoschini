using System.ComponentModel.DataAnnotations;

namespace PizzeriaMoschini.Models
{
    public class Customer
    {
        [Key]
        [Display(Name = "Customer ID")]
        public int CustomerID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        public string Email { get; set; }

        public ICollection<Reservation> Reservations { get; set; }
    }
}
