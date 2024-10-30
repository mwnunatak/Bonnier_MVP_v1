using System.Collections.Generic;

namespace BonPortalBackend.Models
{
    public class VerifyCodeViewModel
    {
        public string? CurrentEmail { get; set; }
        public string? NewEmail { get; set; }
        public int? VerificationCode { get; set; }
        public bool IsAwaitingVerification { get; set; } = false;  // Added this property
        public List<BonDbContacts>? Contacts { get; set; }
        public int? AutCode { get; set; }
        public string? Email { get; set; }
        public bool IsAuthenticated { get; set; } = false;  // Add this new property

        public DateTime? LastVerificationAttempt { get; set; }
    public bool CanResendCode => !LastVerificationAttempt.HasValue || 
                                DateTime.Now.Subtract(LastVerificationAttempt.Value).TotalSeconds >= 20;
    public int RemainingSeconds => LastVerificationAttempt.HasValue ? 
        Math.Max(0, 20 - (int)DateTime.Now.Subtract(LastVerificationAttempt.Value).TotalSeconds) : 0;
    }
    
}