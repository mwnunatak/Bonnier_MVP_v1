using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonPortalBackend.Models
{
    [Table("bon_db_verificationcodes")]
    public class BonDbVerificationCodes
{
    public int Id { get; set; }
    public required string Initial_Mail { get; set; }
    public required string New_Mail { get; set; }
    public int New_Auth_Code { get; set; }
    public bool Auth_Success { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
}