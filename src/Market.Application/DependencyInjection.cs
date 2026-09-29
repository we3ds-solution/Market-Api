using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Market.Application.Validators;
using Market.Domain.Entities;

namespace Market.Application;

/// <summary>
/// Application layer dependency injection registration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR with all handlers in the Application assembly
        services.AddMediatR(config =>
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register custom validators for dependency injection (DIP fix)
        // Using fully qualified names to resolve ambiguity with FluentValidation.IValidator<T>
        services.AddScoped<Validators.IValidator<Product>, ProductValidator>();
        services.AddScoped<Validators.IValidator<Category>, CategoryValidator>();
        services.AddScoped<Validators.IValidator<Order>, OrderValidator>();
        services.AddScoped<Validators.IValidator<User>, UserValidator>();
        services.AddScoped<Validators.IValidator<Vendor>, VendorValidator>();
        services.AddScoped<Validators.IValidator<Cart>, CartValidator>();
        services.AddScoped<Validators.IValidator<Review>, ReviewValidator>();

        return services;
    }
}


