using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonPortalBackend.Models
{
    [Table("bon_db_mailing", Schema = "dbo")]
    public class BonDbMailing
    {
        [Key]
        public string Lzgemail { get; set; }
        public string? Honempfmail { get; set; }
        public string? AutCode { get; set; }
    }
}