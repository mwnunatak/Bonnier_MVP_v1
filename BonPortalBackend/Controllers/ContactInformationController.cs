using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BonPortalBackend.Data;
using BonPortalBackend.Models;
using BonPortalBackend.Extensions;
using BonPortalBackend.Services;
using System.Linq;
using Microsoft.AspNetCore.Http;
using BonPortalBackend.Utils;



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
            .Where(v => v.Initial_Mail == currentEmail && v.New_Mail == newEmail)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        _logger.LogInformation($"Found {allVerificationEntries.Count} verification entries for {currentEmail}");
        
        // Get the most recent verification entry regardless of the code
        var mostRecentEntry = allVerificationEntries.FirstOrDefault();
        
        if (mostRecentEntry != null)
        {
            _logger.LogInformation($"Most recent verification code: {mostRecentEntry.New_Auth_Code}, Submitted code: {parsedCode}");
            
            if (mostRecentEntry.New_Auth_Code == parsedCode)
            {
                if (mostRecentEntry.Auth_Success)
                {
                    return Json(new { success = false, message = "Dieser Code wurde bereits verwendet." });
                }

                // Update verification status
                mostRecentEntry.Auth_Success = true;
                await _context.SaveChangesAsync();

                // Update the email in contacts and create safety records
                var contacts = await _context.bon_db_contacts
                    .Where(c => c.Honempfemail == currentEmail || c.Lzgemail == currentEmail)
                    .ToListAsync();

                _logger.LogInformation($"Found {contacts.Count} contacts to update with new email");

                foreach (var contact in contacts)
                {
                    // Store original email before updating
                    string safetyToken = SafetyLinkGenerator.GenerateSafetyLink();
                    string fullSafetyLink = SafetyLinkGenerator.GetFullSafetyLink(Request, safetyToken);

                    // Create recovery record
                    var recoveryRecord = new BonDbRecoveries
                    {
                        Lzgemail = contact.Lzgemail,
                        Honempfemail = contact.Honempfemail,
                        Honempfemail_NEW = newEmail,
                        SafetyLink = safetyToken,
                        SafetyRequest = "FALSE"
                    };
                    
                    _context.bon_db_recoveries.Add(recoveryRecord);
                    
                    // Update contact with new email
                    contact.Honempfemail_NEU = newEmail;
                    _logger.LogInformation($"Updating contact {contact.VtgP_Name} with new email: {newEmail}");

                    // Send notification emails to both old addresses if they exist and are different
                    if (!string.IsNullOrEmpty(contact.Lzgemail))
                    {
                        await _emailService.SendEmailChangeNotificationAsync(contact.Lzgemail, newEmail, fullSafetyLink);
                    }
                    
                    if (!string.IsNullOrEmpty(contact.Honempfemail) && contact.Honempfemail != contact.Lzgemail)
                    {
                        await _emailService.SendEmailChangeNotificationAsync(contact.Honempfemail, newEmail, fullSafetyLink);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Email verification completed successfully");

                return Json(new { 
                    success = true,
                    message = "E-Mail-Adresse wurde erfolgreich verifiziert."
                });
            }
            else
            {
                _logger.LogWarning($"Code mismatch - Expected: {mostRecentEntry.New_Auth_Code}, Received: {parsedCode}");
                return Json(new { success = false, message = "Falscher Code" });
            }
        }
        else
        {
            _logger.LogWarning("No verification entries found");
            return Json(new { success = false, message = "Keine Verifizierung ausstehend" });
        }
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
public async Task<IActionResult> HandleSafetyRequest(string token)
{
    try
    {
        var recovery = await _context.bon_db_recoveries
            .FirstOrDefaultAsync(r => r.SafetyLink == token);

        if (recovery == null)
        {
            return View("Error", new ErrorViewModel { ErrorMessage = "Ungültiger oder abgelaufener Sicherheitslink." });
        }

        if (recovery.SafetyRequest == "TRUE")
        {
            return View("SafetyRequest", new { Message = "Diese Sicherheitsanfrage wurde bereits bearbeitet." });
        }

        // Update the recovery record
        recovery.SafetyRequest = "TRUE";
        await _context.SaveChangesAsync();

        // Return the safety request view
        return View("SafetyRequest", new { Message = "Wir haben registriert, dass Sie ihre E-Mail nicht geändert haben. Der Vorgang wird gesperrt und Sie erhalten Ihre Abrechnung weiterhin analog." });
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error handling safety request: {ex.Message}");
        return View("Error", new ErrorViewModel { ErrorMessage = "Ein Fehler ist aufgetreten." });
    }
}

[HttpGet]
public IActionResult VerifyCode(string? q = null)
{
    string? verificationCode = null;
    var isAuthenticated = HttpContext.Session.GetBool(AuthenticationSessionKey) ?? false;

    if (!string.IsNullOrEmpty(q))
    {
        verificationCode = VerificationCodeEncoder.DecodeVerificationCode(q);
        _logger.LogInformation($"Decoded verification code from q parameter: {verificationCode}");
        
        // Validate format and checksum for decoded URL parameter
        if (!VerificationCodeEncoder.ValidateFormat(verificationCode))
        {
            _logger.LogWarning($"Invalid code format: {verificationCode}");
            ViewData["Error"] = "Ungültiges Code-Format.";
            return View(new VerifyCodeViewModel());
        }
        
        if (!VerificationCodeEncoder.ValidateChecksum(verificationCode))
        {
            _logger.LogWarning($"Invalid checksum for code: {verificationCode}");
            ViewData["Error"] = "Ungültiger Code.";
            return View(new VerifyCodeViewModel());
        }
    }
    else if (isAuthenticated)
    {
        verificationCode = HttpContext.Session.GetString("LastAutCode");
        _logger.LogInformation($"Retrieved last auth code from session: {verificationCode}");
    }

    if (!string.IsNullOrEmpty(verificationCode))
    {
        if (!verificationCode.Contains("-") && verificationCode.Length == 12)
        {
            verificationCode = $"{verificationCode.Substring(0, 4)}-{verificationCode.Substring(4, 4)}-{verificationCode.Substring(8, 4)}";
        }
        _logger.LogInformation($"Formatted verification code: {verificationCode}");

        var mailingEntry = _context.bon_db_mailing
            .FirstOrDefault(m => m.AutCode == verificationCode);

        _logger.LogInformation($"Database lookup result: {(mailingEntry != null ? "Found" : "Not Found")}");
        if (mailingEntry != null)
        {
            string emailToUse = !string.IsNullOrEmpty(mailingEntry.Honempfmail)
                ? mailingEntry.Honempfmail
                : mailingEntry.Lzgemail;

            var contacts = _context.bon_db_contacts
                .Where(c => c.Honempfemail == emailToUse || c.Lzgemail == emailToUse)
                .ToList();

            HttpContext.Session.SetString("LastAutCode", verificationCode);
            
            return View(new VerifyCodeViewModel
            {
                IsAuthenticated = true,
                AutCode = verificationCode,
                Email = emailToUse,
                Contacts = contacts,
                IsAwaitingVerification = false
            });
        }
        else
        {
            ViewData["NotFound"] = true;
        }
    }

    if (isAuthenticated)
    {
        var lastCode = HttpContext.Session.GetString("LastAutCode");
        var mailingEntry = _context.bon_db_mailing
            .FirstOrDefault(m => m.AutCode == lastCode);

        if (mailingEntry != null)
        {
            var contacts = _context.bon_db_contacts
                .Where(c => c.Honempfemail == mailingEntry.Honempfmail)
                .ToList();

            return View(new VerifyCodeViewModel
            {
                IsAuthenticated = true,
                AutCode = lastCode,
                Email = mailingEntry.Honempfmail,
                Contacts = contacts,
                IsAwaitingVerification = false
            });
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
            ViewData["Error"] = "Bitte geben Sie einen Code ein.";
            return View("VerifyCode", new VerifyCodeViewModel());
        }

        // Clean up the input code
        var formattedCode = autCode.Replace(" ", "").Replace("-", "");
        
        // Validate format first
        if (!VerificationCodeEncoder.ValidateFormat(formattedCode))
        {
            _logger.LogWarning($"Invalid code format: {formattedCode}");
            ViewData["Error"] = "Ungültiges Code-Format.";
            return View("VerifyCode", new VerifyCodeViewModel());
        }
        
        // Validate checksum
        if (!VerificationCodeEncoder.ValidateChecksum(formattedCode))
        {
            _logger.LogWarning($"Invalid checksum for code: {formattedCode}");
            ViewData["Error"] = "Ungültiger Code.";
            return View("VerifyCode", new VerifyCodeViewModel());
        }
        
        // Add dashes for database lookup
        formattedCode = formattedCode.Insert(4, "-").Insert(9, "-");

        // Find the mailing entry with the provided auth code
        var mailingEntry = _context.bon_db_mailing
            .FirstOrDefault(m => m.AutCode == formattedCode);

        if (mailingEntry == null)
        {
            ViewData["NotFound"] = true;
            return View("VerifyCode", new VerifyCodeViewModel());
        }

        // Use Lzgemail if Honempfmail is empty
        string emailToUse = !string.IsNullOrEmpty(mailingEntry.Honempfmail) 
            ? mailingEntry.Honempfmail 
            : mailingEntry.Lzgemail;

        _logger.LogInformation($"Using email for contact search: {emailToUse}");

        // Find all contacts with matching email in either field
        var contacts = _context.bon_db_contacts
            .Where(c => c.Honempfemail == emailToUse || c.Lzgemail == emailToUse)
            .ToList();

        _logger.LogInformation($"Found {contacts.Count} contacts for email {emailToUse}");

        // Set authentication status
        SetAuthenticationStatus(true);
        HttpContext.Session.SetString("LastAutCode", formattedCode);

        var viewModel = new VerifyCodeViewModel
        {
            AutCode = formattedCode,
            Email = emailToUse,
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
        ViewData["Error"] = "Ein Fehler ist aufgetreten.";
        return View("VerifyCode", new VerifyCodeViewModel());
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
                // Find contacts by either email field
                var contacts = _context.bon_db_contacts
                    .Where(c => c.Honempfemail == email || c.Lzgemail == email)
                    .ToList();

                _logger.LogInformation($"Found {contacts.Count} contacts for email {email}");

                if (!contacts.Any())
                {
                    return Json(new { success = false, message = "Keine Kontakte gefunden." });
                }

                foreach (var contact in contacts)
{
    if (optIn)
    {
        // Always use the verified new email if it exists, otherwise keep using current email
        contact.Honempfemail_NEU = newEmail ?? contact.Honempfemail_NEU ?? email;
        _logger.LogInformation($"Setting new email for {contact.VtgP_Name} to: {contact.Honempfemail_NEU}");
    }
    else
    {
        contact.Honempfemail_NEU = null;
        _logger.LogInformation($"Clearing email for {contact.VtgP_Name} due to opt-out");
    }
    
    contact.Honempf_Opt_In = optIn ? "TRUE" : "FALSE";
    _logger.LogInformation($"Updated Opt-In status for {contact.VtgP_Name} to: {contact.Honempf_Opt_In}");
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