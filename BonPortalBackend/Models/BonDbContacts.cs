using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BonPortalBackend.Models
{
[Table("bon_db_contacts", Schema = "dbo")]
public class BonDbContacts
{
    [Key]
    public int VtgP_AdrNr { get; set; }
    public string? VtgP_Name { get; set; }
    public int? abw_Hon_Nr { get; set; }
    public string? VtgP_Strasse { get; set; }
    public string? VtgP_LKZ { get; set; }
    public int? VtgP_PLZ { get; set; }
    public string? VtgP_Ort { get; set; }
    public string? Lzgemail { get; set; }
    public string? Hon_Empanger_Adr { get; set; }
    public string? Honempfemail { get; set; }
    public string? Honempfemail_NEU { get; set; }
    public string? Honempf_Opt_In { get; set; }
}
}