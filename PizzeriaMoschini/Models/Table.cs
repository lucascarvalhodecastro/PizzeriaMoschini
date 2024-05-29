using System.ComponentModel.DataAnnotations;

namespace PizzeriaMoschini.Models
{
    public class Table
    {
        [Key]
        [Display(Name = "Table ID")]
        public int TableID { get; set; }

        [Required]
        public int Capacity { get; set; }
    }
}
