using Microsoft.EntityFrameworkCore;
using ZerodaTrade.Data;

namespace ZerodaTrade.Services
{
    public static class DbInitializer
    {
        public static void Initialize(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                try
                {
                    // Create database if it doesn't exist
                    context.Database.EnsureCreated();

                    // Seed a default admin user if no users exist
                    if (!context.Users.Any())
                    {
                        var hasher = ZerodaTrade.Helpers.PasswordHasher.HashPassword("password");
                        var user = new ZerodaTrade.Models.User
                        {
                            Username = "admin",
                            PasswordHash = hasher.hash,
                            PasswordSalt = hasher.salt,
                            CreatedAt = DateTime.UtcNow
                        };
                        context.Users.Add(user);
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while creating the database.");
                }
            }
        }
    }
}
