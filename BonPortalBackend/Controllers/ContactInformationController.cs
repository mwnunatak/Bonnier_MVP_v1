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

        // Send verification email
        await _emailService.SendVerificationEmailAsync(newEmail, verificationCode);

        // Return JSON response with needed data for modal
        return Json(new { 
            success = true, 
            message = "Verifizierungscode wurde gesendet.",
            newEmail = newEmail,
            currentEmail = currentEmail
        });
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error initiating email change: {ex.Message}");
        return Json(new { 
            success = false, 
            message = "Fehler beim Senden des Verifizierungscodes."
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
public IActionResult VerifyCode(string? autCode = null)
{
    var isAuthenticated = HttpContext.Session.GetBool(AuthenticationSessionKey) ?? false;

    if (!string.IsNullOrEmpty(autCode) && int.TryParse(autCode, out int codeValue))
    {
        var mailingEntry = _context.BonDbMailing
            .FirstOrDefault(m => m.AutCode == codeValue);

        if (mailingEntry != null)
        {
            var contacts = _context.BonDbContacts
                .Where(c => c.Honempfemail == mailingEntry.Honempfmail)
                .ToList();

            return View(new VerifyCodeViewModel 
            { 
                IsAuthenticated = isAuthenticated,
                AutCode = codeValue,
                Email = mailingEntry.Honempfmail,
                Contacts = contacts
            });
        }
    }

    return View(new VerifyCodeViewModel { IsAuthenticated = isAuthenticated });
}

[HttpPost]
public IActionResult VerifyCodeSubmit(string autCode)
{
    try
    {
        _logger.LogInformation($"Attempting to verify code: {autCode}");

        if (string.IsNullOrEmpty(autCode) || !int.TryParse(autCode, out int codeValue))
        {
            ViewData["NotFound"] = true;
            return View(new VerifyCodeViewModel());
        }

        // Find the mailing entry with the provided auth code
        var mailingEntry = _context.BonDbMailing
            .FirstOrDefault(m => m.AutCode == codeValue);

        if (mailingEntry == null)
        {
            ViewData["NotFound"] = true;
            return View(new VerifyCodeViewModel());
        }

        // Find all contacts with matching email
        var contacts = _context.BonDbContacts
            .Where(c => c.Honempfemail == mailingEntry.Honempfmail)
            .ToList();

        // Set authentication status
        SetAuthenticationStatus(true);

        var viewModel = new VerifyCodeViewModel
        {
            AutCode = codeValue,
            Email = mailingEntry.Honempfmail,
            Contacts = contacts,
            IsAuthenticated = true
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