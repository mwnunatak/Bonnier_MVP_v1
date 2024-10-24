using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjectTest.Data;
using WebProjectTest.Models;
using System.Linq;

namespace WebProjectTest.Controllers
{
    public class ContactInformationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContactInformationController> _logger;

    public ContactInformationController(ApplicationDbContext context, ILogger<ContactInformationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult VerifyCode()
    {
        return View(new VerifyCodeViewModel());
    }

    [HttpPost]
    public IActionResult VerifyCode(string autCode)
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

            var viewModel = new VerifyCodeViewModel
            {
                AutCode = codeValue,
                Email = mailingEntry.Honempfmail,
                Contacts = contacts
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error verifying code: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [HttpPost]
public IActionResult SaveOptIn(int autCode, string email, bool optIn, string? newEmail, string? confirmNewEmail)
{
    _logger.LogInformation($"Received parameters - autCode: {autCode}, email: {email}, optIn: {optIn}, newEmail: {newEmail}");

    try
    {
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("Email parameter is null or empty");
            TempData["ErrorMessage"] = "Email parameter is missing";
            return RedirectToAction(nameof(VerifyCode));
        }

        // Validate new email if provided
        if (!string.IsNullOrEmpty(newEmail))
        {
            if (newEmail != confirmNewEmail)
            {
                TempData["ErrorMessage"] = "Die eingegebenen E-Mail-Adressen stimmen nicht überein.";
                return RedirectToAction(nameof(VerifyCode));
            }
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
                    _logger.LogWarning("No contacts found to update");
                    TempData["ErrorMessage"] = "Keine Kontakte gefunden.";
                    return RedirectToAction(nameof(VerifyCode));
                }

                foreach (var contact in contacts)
                {
                    _logger.LogInformation($"Before update - Contact {contact.VtgP_AdrNr}");
                    
                    // Use the new email if provided, otherwise use the existing email
                    string emailToSave = !string.IsNullOrEmpty(newEmail) ? newEmail : email;
                    
                    // Only update Honempfemail_NEU, not the original Honempfemail
                    contact.Honempfemail_NEU = optIn ? emailToSave : null;
                    contact.Honempf_Opt_In = optIn ? "TRUE" : "FALSE";
                    
                    _logger.LogInformation($"After update - Contact {contact.VtgP_AdrNr}");
                }

                _logger.LogInformation("Attempting to save changes to database");
                _context.SaveChanges();
                transaction.Commit();

                _logger.LogInformation("Changes saved successfully");
                string successMessage = !string.IsNullOrEmpty(newEmail) 
                    ? "Ihre neue E-Mail-Adresse wurde erfolgreich hinterlegt."
                    : "Ihre Einstellungen wurden erfolgreich gespeichert.";
                TempData["SuccessMessage"] = successMessage;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Transaction rolled back due to error: {ex.Message}");
                throw;
            }
        }
        
        return RedirectToAction(nameof(VerifyCode));
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error in SaveOptIn: {ex.Message}");
        _logger.LogError($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            _logger.LogError($"Inner exception: {ex.InnerException.Message}");
        }
        TempData["ErrorMessage"] = "Ein Fehler ist aufgetreten beim Speichern Ihrer Einstellungen.";
        return RedirectToAction(nameof(VerifyCode));
    }
}
}
}