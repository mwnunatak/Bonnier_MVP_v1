using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebProjectTest.Models
{
    [Table("BON_DB_Kontaktadressen", Schema = "dbo")]
    public class ContactInformation
    {   
        [Column("Autor")]
        public string? Autor { get; set; }
        
        [Column("E_Mail")]
        public string? Email { get; set; }
        
        [Key]
        [Column("AutCode")]
        public int AutCode { get; set; }  // Changed from string to int
        
        [Column("Opt_In")]
        public string? OptIn { get; set; }  // Changed from bool to string (nvarchar)
        
        [Column("Honorar_E_Mail")]
        public string? HonorarEmail { get; set; }
    }
}