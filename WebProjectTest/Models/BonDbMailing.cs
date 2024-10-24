using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebProjectTest.Models
{
    [Table("bon_db_mailing", Schema = "dbo")]
    public class BonDbMailing
    {
        [Key]
        public string Lzgemail { get; set; }
        public string? Honempfmail { get; set; }
        public int? AutCode { get; set; }
        [Column("column4")]
        public string? Column4 { get; set; }
    }
}