using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShareXHost.Storage;

namespace ShareXHost.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
                options.UseNpgsql("Host=localhost;Port=5432;Database=sharexhost_test;Username=sharexhost_test;Password=test_user");
            });

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();
                
                // add a test user to the database
                if (!dbContext.Users.Any(u => u.Name == "Alice"))
                {
                    dbContext.Users.Add(new User
                    {
                        Name = "Alice",
                        Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000")
                    });
                }
                
                if (!dbContext.Users.Any(u => u.Name == "Bob"))
                {
                    dbContext.Users.Add(new User
                    {
                        Name = "Bob",
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111")
                    });
                }
                dbContext.SaveChanges();
            }
        });
    }
}