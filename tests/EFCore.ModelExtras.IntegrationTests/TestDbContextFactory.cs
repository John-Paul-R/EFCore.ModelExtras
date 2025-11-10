using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EFCore.ModelExtras.IntegrationTests;

public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Database=test_db;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .UseModelExtras();

        return new TestDbContext(optionsBuilder.Options);
    }
}
