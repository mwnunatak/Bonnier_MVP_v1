using Microsoft.EntityFrameworkCore;
using WebProjectTest.Models;

namespace WebProjectTest.Data
{
    public class ApplicationDbContext : DbContext
    {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<ContactInformation> ContactInformation {get; set;}
    public DbSet<BonDbMailing> BonDbMailing {get; set;}
    public DbSet<BonDbContacts> BonDbContacts {get; set;}
    }
}