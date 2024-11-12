using Microsoft.EntityFrameworkCore;
using BonPortalBackend.Models;

namespace BonPortalBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            this.Database.SetCommandTimeout(10); // Set timeout to 10 seconds
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new DbUpdateException("Database operation failed", ex);
            }
        }
    public DbSet<ContactInformation> ContactInformation {get; set;}
    public DbSet<BonDbMailing> bon_db_mailing {get; set;}
    public DbSet<BonDbContacts> bon_db_contacts {get; set;}
    public DbSet<BonDbVerificationCodes> bon_db_verificationcodes { get; set; }
    }
}