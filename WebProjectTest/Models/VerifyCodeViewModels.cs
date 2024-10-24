using System.Collections.Generic;
using WebProjectTest.Models;  // Add this line

namespace WebProjectTest.Models
{
   public class VerifyCodeViewModel
{
    public int? AutCode { get; set; }
    public string? Email { get; set; }
    public List<BonDbContacts> Contacts { get; set; } = new List<BonDbContacts>();
    
    // New properties
    public bool IsChangingEmail { get; set; }
    public string? NewEmail { get; set; }
    public string? ConfirmNewEmail { get; set; }
}
}