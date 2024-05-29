using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PizzeriaMoschini.Models
{
    public class Reservation
    {
        [Key]
        [Display(Name = "Reservation ID")]
        public int ReservationID { get; set; }

        [ForeignKey("Customer")]
        [Display(Name = "Customer ID")]
        public int CustomerID { get; set; }

        [ForeignKey("Table")]
        [Display(Name = "Table ID")]
        public int TableID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Reservation Date")]
        public DateTime ReservationDate { get; set; }

        [Required]
        [Display(Name = "Time Slot")]
        public string TimeSlot { get; set; }

        [Required]
        [Range(1, 6)]
        [Display(Name = "Number of Guests")]
        public int NumberOfGuests { get; set; }

        public Customer Customer { get; set; }

        public Table Table { get; set; }
    }
}
