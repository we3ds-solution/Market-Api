using Market.Domain.Repositories;
using Market.Application.Abstractions.Services;
using Market.Infrastructure.Persistence.Services;
using Market.Infrastructure.Services;
using Market.Infrastructure.Data;
using Market.Infrastructure.Data.Seeds;
using Market.Infrastructure.Persistence.Repositories;
using Market.Infrastructure.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection registration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext with SQL Server
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection connection string not found in configuration.");
        }

        services.AddDbContext<MarketDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                {
                    // Keep migration discovery anchored to the DbContext assembly rather than
                    // the API startup assembly.
                    sqlServerOptions.MigrationsAssembly(typeof(MarketDbContext).Assembly.FullName);

                    // SQL Server can still be completing its startup sequence when the API
                    // container begins. Retry transient connection failures so startup can
                    // reach MigrateAsync and apply the initial schema.
                    sqlServerOptions.EnableRetryOnFailure();
                }));

        // Register repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();

        // Register Application read services (implemented in Infrastructure with EF Core)
        services.AddScoped<IProductReadService, ProductReadService>();

        // Register current user service for authentication/authorization
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register data seeder
        services.AddScoped<DataSeeder>();

        return services;
    }

    /// <summary>
    /// Initialize the database with migrations and seed data.
    /// This method should be called from the API layer (Program.cs)
    /// </summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider services, ILogger logger)
    {
        using (var scope = services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

            try
            {
                logger.LogInformation("Initializing database...");

                // Apply any pending migrations and create database
                await context.Database.MigrateAsync();

                // Seed data
                await seeder.SeedAsync();

                logger.LogInformation("Database initialization completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing database. Application startup aborted.");
                throw; // Re-throw: a database failure must not appear as a successful startup
            }
        }
    }
}
