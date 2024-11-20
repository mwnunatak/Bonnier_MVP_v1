using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonPortalBackend.Models
{
    [Table("bon_db_recoveries", Schema = "dbo")]
    public class BonDbRecoveries
    {
        [Key]
        public int Id { get; set; }

        [Column("Lzgemail")]
        public string? Lzgemail { get; set; }

        [Column("Honempfemail")]
        public string? Honempfemail { get; set; }

        [Column("Honempfemail_NEW")]
        public string? Honempfemail_NEW { get; set; }

        [Column("SafetyLink")]
        public string? SafetyLink { get; set; }

        [Column("SafetyRequest")]
        public string? SafetyRequest { get; set; }
    }
}