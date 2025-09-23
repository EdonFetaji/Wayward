using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Wayward.Repository
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Assume you're running EF Tools from Wayward.Repository directory
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Wayward.Web");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables() // works after adding the package
                .Build();

            var cs = configuration.GetConnectionString("DefaultConnection");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs)) // Pomelo
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
