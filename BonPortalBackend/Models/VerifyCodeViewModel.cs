using System.Collections.Generic;

namespace BonPortalBackend.Models
{
    public class VerifyCodeViewModel
{
    public string? CurrentEmail { get; set; }
    public string? NewEmail { get; set; }
    public string? VerificationCode { get; set; }
    public bool IsAwaitingVerification { get; set; } = false;
    public List<BonDbContacts>? Contacts { get; set; }
    public string? AutCode { get; set; }  // Changed from int? to string?
    public string? Email { get; set; }
    public bool IsAuthenticated { get; set; } = false;
    public DateTime? LastVerificationAttempt { get; set; }
    public bool CanResendCode => !LastVerificationAttempt.HasValue || 
                                DateTime.Now.Subtract(LastVerificationAttempt.Value).TotalSeconds >= 20;
    public int RemainingSeconds => LastVerificationAttempt.HasValue ? 
        Math.Max(0, 20 - (int)DateTime.Now.Subtract(LastVerificationAttempt.Value).TotalSeconds) : 0;
}
    
}