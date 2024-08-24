using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaMoschini.Models
{
    public class Reservation
    {
        // Primary key for Reservation entity
        [Key]
        [Display(Name = "Reservation ID")]
        public int ReservationID { get; set; }

        // Foreign key for Customer entity
        [ForeignKey("Customer")]
        [Display(Name = "Customer ID")]
        public int CustomerID { get; set; }

        // Foreign key for Table entity
        [ForeignKey("Table")]
        [Display(Name = "Table ID")]
        public int TableID { get; set; }

        // Nullable foreign key for Staff entity (optional relationship)
        [ForeignKey("Staff")]
        [Display(Name = "Staff ID")]
        public int? StaffID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Reservation Date")]
        public DateTime ReservationDate { get; set; }

        [Required]
        [Display(Name = "Time Slot")]
        public string TimeSlot { get; set; }

        [Required]
        [Range(1, 6, ErrorMessage = "Please enter the number of guests between 1 and 6. For larger groups, please call the restaurant at +353 1 551 1206 or email us at pizzeriamoschini@outlook.ie. Thank you!")]
        [Display(Name = "Number of Guests")]
        public int NumberOfGuests { get; set; }

        // Navigation property for Customer entity
        public Customer Customer { get; set; }

        // Navigation property for Table entity
        public Table Table { get; set; }

        // Navigation property for Table entity
        public Staff Staff { get; set; }
    }
}
