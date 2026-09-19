using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ShareXHost.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=sharexhost_test;Username=sharexhost_test;Password=test_user";

    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        IHost host = base.CreateHost(builder);

        using IServiceScope scope = host.Services.CreateScope();

        AppDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Database.Migrate();
        
        dbContext.Files.RemoveRange(dbContext.Files);
        dbContext.Users.RemoveRange(dbContext.Users);
        dbContext.Links.RemoveRange(dbContext.Links);
        
        dbContext.SaveChanges();

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    { 
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:Enabled"] = "false" });
        });
        builder.ConfigureServices(services =>
        {
            // replace the existing DbContext with a new one that uses the test database
            ServiceDescriptor? dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
            
            // replace file storage service with a test implementation
            ServiceDescriptor? fileStorageDescriptor = 
                services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorage));
            if (fileStorageDescriptor != null) services.Remove(fileStorageDescriptor);
            services.AddSingleton<IFileStorage, TestFileStorage>();
            
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(TestConnectionString);
            });
        });
    }
}