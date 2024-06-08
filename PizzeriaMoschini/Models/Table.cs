using System.ComponentModel.DataAnnotations;

namespace PizzeriaMoschini.Models
{
    public class Table
    {
        // Primary key for Table entity
        [Key]
        [Display(Name = "Table ID")]
        public int TableID { get; set; }

        [Required]
        public int Capacity { get; set; }

        // Navigation property to link reservations associated with table
        public ICollection<Reservation> Reservations { get; set; }
    }
}
