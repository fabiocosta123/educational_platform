using EducationalPlataform.Data;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Factories
{
    public class EducationalPlataformContextFactory : IDesignTimeDbContextFactory<EducationalPlataformContext>
    {
        public EducationalPlataformContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EducationalPlataformContext>();
            optionsBuilder.UseSqlServer("Server=FABIO\\SQLEXPRESS;Database=EducationalPlataform;Trusted_Connection=True;TrustServerCertificate=True;");
            return new EducationalPlataformContext(optionsBuilder.Options);
        }
    }
}
