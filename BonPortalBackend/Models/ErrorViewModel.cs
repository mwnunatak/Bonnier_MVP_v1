namespace BonPortalBackend.Models;
public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool IsDatabaseError { get; set; }
    public string? ErrorMessage { get; set; }
}