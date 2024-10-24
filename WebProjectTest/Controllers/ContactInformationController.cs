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
        return View();
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
            return View();
        }

        var contact = _context.ContactInformation
            .FirstOrDefault(c => c.AutCode == codeValue);

        if (contact == null)
        {
            ViewData["NotFound"] = true;
            return View();
        }

        return View(contact);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error verifying code: {ex.Message}");
        _logger.LogError($"Stack trace: {ex.StackTrace}");
        throw;
    }
}

[HttpPost]
public IActionResult SaveOptIn(int autCode, bool optIn)
{
    try
    {
        var contact = _context.ContactInformation.FirstOrDefault(c => c.AutCode == autCode);
        if (contact != null)
        {
            contact.OptIn = optIn ? "TRUE" : "FALSE";
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Ihre Einstellungen wurden erfolgreich hinterlegt.";
        }
        return RedirectToAction(nameof(VerifyCode));
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error saving opt-in preference: {ex.Message}");
        TempData["ErrorMessage"] = "An error occurred while saving your preference.";
        return RedirectToAction(nameof(VerifyCode));
    }
}
}
}