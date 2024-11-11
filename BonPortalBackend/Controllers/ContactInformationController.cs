using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BonPortalBackend.Data;
using BonPortalBackend.Models;
using BonPortalBackend.Extensions;
using BonPortalBackend.Services;
using System.Linq;
using Microsoft.AspNetCore.Http;


namespace BonPortalBackend.Controllers
{
    public class ContactInformationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContactInformationController> _logger;
    private readonly IEmailService _emailService;
    private readonly Random _random;
    private const string AuthenticationSessionKey = "IsAuthenticated";

    private void SetAuthenticationStatus(bool isAuthenticated)
    {
        HttpContext.Session.SetBool(AuthenticationSessionKey, isAuthenticated);
    }

    public ContactInformationController(
        ApplicationDbContext context, 
        ILogger<ContactInformationController> logger,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _random = new Random();
    }

[HttpPost]
public async Task<IActionResult> InitiateEmailChange(int autCode, string currentEmail, string newEmail)
{
    try
    {
        _logger.LogInformation($"Initiating email change from {currentEmail} to {newEmail}");

        // Generate a random 6-digit verification code
        int verificationCode = _random.Next(100000, 999999);

        _logger.LogInformation($"Generated verification code: {verificationCode}");

        // Store the verification attempt in the database
        var verificationEntry = new BonDbVerificationCodes
        {
            Initial_Mail = currentEmail,
            New_Mail = newEmail,
            New_Auth_Code = verificationCode,
            Auth_Success = false
        };

        _context.bon_db_verificationcodes.Add(verificationEntry);
        await _context.SaveChangesAsync();

        try
        {
            // Send verification email
            await _emailService.SendVerificationEmailAsync(newEmail, verificationCode);
            
            // Return JSON response with needed data for modal
            return Json(new { 
                success = true, 
                message = "Verifizierungscode wurde erneut gesendet.",
                newEmail = newEmail,
                currentEmail = currentEmail
            });
        }
        catch (Exception emailEx)
        {
            _logger.LogError($"Error sending verification email: {emailEx.Message}");
            return Json(new { 
                success = false, 
                message = "Fehler beim Senden des Verifizierungscodes per E-Mail."
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error initiating email change: {ex.Message}");
        return Json(new { 
            success = false, 
            message = "Fehler beim Initiieren der E-Mail-Änderung."
        });
    }
}

[HttpPost]
public async Task<IActionResult> VerifyNewEmail(string newEmail, string currentEmail, int autCode, string verificationCode)
{
    try
    {
        _logger.LogInformation($"Starting verification process - Current Email: {currentEmail}, New Email: {newEmail}, Verification Code: {verificationCode}");

        if (!int.TryParse(verificationCode, out int parsedCode))
        {
            _logger.LogWarning($"Failed to parse verification code: {verificationCode}");
            return Json(new { success = false, message = "Ungültiger Code-Format" });
        }

        // Log all verification entries for this email
        var allVerificationEntries = await _context.bon_db_verificationcodes
            .Where(v => v.Initial_Mail == currentEmail)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        _logger.LogInformation($"Found {allVerificationEntries.Count} verification entries for {currentEmail}");
        foreach (var entry in allVerificationEntries)
        {
            _logger.LogInformation($"Verification Entry - ID: {entry.Id}, Initial: {entry.Initial_Mail}, " +
                                 $"New: {entry.New_Mail}, Code: {entry.New_Auth_Code}, " +
                                 $"Success: {entry.Auth_Success}, Created: {entry.CreatedAt}");
        }

        // Find the most recent verification entry with matching code
        var verificationEntry = await _context.bon_db_verificationcodes
            .Where(v => v.Initial_Mail == currentEmail 
                    && v.New_Auth_Code == parsedCode
                    && !v.Auth_Success)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

        if (verificationEntry == null)
        {
            _logger.LogWarning($"No matching verification entry found for code: {parsedCode}");
            
            // Try to find why it didn't match
            var lastEntry = allVerificationEntries.FirstOrDefault();
            if (lastEntry != null)
            {
                _logger.LogInformation($"Last verification entry - Code: {lastEntry.New_Auth_Code}, " +
                                     $"Matches provided code: {lastEntry.New_Auth_Code == parsedCode}, " +
                                     $"Already verified: {lastEntry.Auth_Success}");
            }
            
            return Json(new { success = false, message = "Falscher Code" });
        }

        _logger.LogInformation($"Found matching verification entry. Updating status...");

        // Update verification status
        verificationEntry.Auth_Success = true;
        await _context.SaveChangesAsync();

        // Update the email in contacts
        var contacts = await _context.BonDbContacts
            .Where(c => c.Honempfemail == currentEmail)
            .ToListAsync();

        foreach (var contact in contacts)
        {
            contact.Honempfemail_NEU = verificationEntry.New_Mail;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Email verification completed successfully");

        return Json(new { 
            success = true,
            message = "E-Mail-Adresse wurde erfolgreich verifiziert."
        });
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error during verification: {ex.Message}");
        _logger.LogError($"Stack trace: {ex.StackTrace}");
        return Json(new { 
            success = false, 
            message = "Fehler bei der Verifizierung."
        });
    }
}

[HttpGet]
public IActionResult VerifyCode(string? code = null, string? autCode = null)
{
    // If code is provided in URL, use that as the autCode
    string? verificationCode = code ?? autCode;
    
    _logger.LogInformation($"Initial verification code from URL: {verificationCode}");

    var isAuthenticated = HttpContext.Session.GetBool(AuthenticationSessionKey) ?? false;
    
    // If there's a code in the URL, add it to ViewData for auto-fill
    if (!string.IsNullOrEmpty(verificationCode))
    {
        // Format the code if it doesn't have dashes
        if (!verificationCode.Contains("-") && verificationCode.Length == 12)
        {
            verificationCode = $"{verificationCode.Substring(0, 4)}-{verificationCode.Substring(4, 4)}-{verificationCode.Substring(8, 4)}";
        }
        ViewData["PrefilledCode"] = verificationCode;
        _logger.LogInformation($"Formatted verification code: {verificationCode}");

        // Try to find the mailing entry
        var mailingEntry = _context.BonDbMailing
            .FirstOrDefault(m => m.AutCode == verificationCode);

        _logger.LogInformation($"Database lookup result: {(mailingEntry != null ? "Found" : "Not Found")}");
        if (mailingEntry != null)
        {
            _logger.LogInformation($"Found mailing entry with email: {mailingEntry.Honempfmail}");
            
            var contacts = _context.BonDbContacts
                .Where(c => c.Honempfemail == mailingEntry.Honempfmail)
                .ToList();

            _logger.LogInformation($"Found {contacts.Count} contacts");

            return View(new VerifyCodeViewModel 
            { 
                IsAuthenticated = true,
                AutCode = verificationCode,
                Email = mailingEntry.Honempfmail,
                Contacts = contacts,
                IsAwaitingVerification = false
            });
        }
        else
        {
            // Log the actual database values for comparison
            var allCodes = _context.BonDbMailing
                .Select(m => m.AutCode)
                .ToList();
            _logger.LogInformation($"Available codes in database: {string.Join(", ", allCodes)}");
            
            ViewData["NotFound"] = true;
            _logger.LogWarning($"No mailing entry found for code: {verificationCode}");
        }
    }

    return View(new VerifyCodeViewModel 
    { 
        IsAuthenticated = isAuthenticated,
        IsAwaitingVerification = false
    });
}

[HttpPost]
public IActionResult VerifyCodeSubmit(string autCode)
{
    try
    {
        _logger.LogInformation($"Attempting to verify code: {autCode}");

        if (string.IsNullOrEmpty(autCode))
        {
            ViewData["NotFound"] = true;
            return View("VerifyCode", new VerifyCodeViewModel());
        }

        // Clean up the input code if needed (in case user enters without dashes)
        var formattedCode = autCode.Replace(" ", "").Replace("-", "").Insert(4, "-").Insert(9, "-");

        // Find the mailing entry with the provided auth code
        var mailingEntry = _context.BonDbMailing
            .FirstOrDefault(m => m.AutCode == formattedCode);

        if (mailingEntry == null)
        {
            ViewData["NotFound"] = true;
            return View("VerifyCode", new VerifyCodeViewModel());
        }

        // Find all contacts with matching email
        var contacts = _context.BonDbContacts
            .Where(c => c.Honempfemail == mailingEntry.Honempfmail)
            .ToList();

        // Set authentication status
        SetAuthenticationStatus(true);
        HttpContext.Session.SetString("LastAutCode", formattedCode);

        var viewModel = new VerifyCodeViewModel
        {
            AutCode = formattedCode,
            Email = mailingEntry.Honempfmail,
            Contacts = contacts,
            IsAuthenticated = true,
            IsAwaitingVerification = false
        };

        return View("VerifyCode", viewModel);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error verifying code: {ex.Message}");
        _logger.LogError($"Stack trace: {ex.StackTrace}");
        throw;
    }
}

[HttpPost]
public IActionResult SaveOptIn(int autCode, string email, bool optIn, string? newEmail)
{
    _logger.LogInformation($"Received parameters - autCode: {autCode}, email: {email}, optIn: {optIn}, newEmail: {newEmail}");

    try
    {
        if (string.IsNullOrEmpty(email))
        {
            return Json(new { success = false, message = "Email parameter is missing" });
        }

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var contacts = _context.BonDbContacts
                    .Where(c => c.Honempfemail == email)
                    .ToList();

                _logger.LogInformation($"Found {contacts.Count} contacts for email {email}");

                if (!contacts.Any())
                {
                    return Json(new { success = false, message = "Keine Kontakte gefunden." });
                }

                foreach (var contact in contacts)
                {
                    string emailToSave = !string.IsNullOrEmpty(newEmail) ? newEmail : email;
                    contact.Honempfemail_NEU = optIn ? emailToSave : null;
                    contact.Honempf_Opt_In = optIn ? "TRUE" : "FALSE";
                }

                _context.SaveChanges();
                transaction.Commit();

                string successMessage = !string.IsNullOrEmpty(newEmail) 
                    ? "Ihre neue E-Mail-Adresse wurde erfolgreich hinterlegt."
                    : "Ihre Einstellungen wurden erfolgreich gespeichert.";
                
                return Json(new { success = true, message = successMessage });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Transaction rolled back due to error: {ex.Message}");
                throw;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error in SaveOptIn: {ex.Message}");
        return Json(new { success = false, message = "Ein Fehler ist aufgetreten beim Speichern Ihrer Einstellungen." });
    }
}
}
}