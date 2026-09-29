using Market.API.Middleware;
using Market.API.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

namespace Market.API;

/// <summary>
/// Presentation layer dependency injection registration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Add controllers
        services.AddControllers();

        // Add Health Checks
        services.AddHealthChecks();

        // Register exception mapper service (SRP - separates exception mapping from middleware)
        services.AddScoped<IExceptionMapper, ExceptionMapper>();

        // State-changing public endpoints are protected from basic abuse. The partition is
        // intentionally based on remote IP because this API does not yet have authentication.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("write", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        // Add endpoints API explorer and Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Market API",
                Version = "v1",
                Description = "A RESTful API for e-commerce platform",
                Contact = new OpenApiContact
                {
                    Name = "Mostafa SAID",
                    Url = new Uri("https://github.com/Mostafa-SAID7/Market-Api")
                }
            });

            // Add JWT Security Definition to Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' followed by your JWT token."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Response compression for faster Swagger + API responses
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }


    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Market API v1");
            options.RoutePrefix = "swagger";
            // Enable deep linking and persist authorization
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
            // Inject custom CSS to speed up perceived load
            options.InjectStylesheet("/css/swagger-override.css");
        });

        return app;
    }

    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // Add security headers to all responses. X-XSS-Protection is intentionally omitted:
        // modern browsers ignore it and its legacy behavior can create vulnerabilities.
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Response compression FIRST - wraps everything below
        app.UseResponseCompression();

        // Serve static files with aggressive caching for assets
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                var path = ctx.File.Name;
                // Cache JS/CSS/images for 7 days (they're fingerprinted or rarely change)
                if (path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".svg") || path.EndsWith(".png"))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=604800, immutable";
                }
            }
        });

        // Apply custom middleware (error handling, logging, validation, correlation ID)
        app.UseCustomMiddleware();

        // Enable HTTPS redirection
        app.UseHttpsRedirection();

        app.UseRateLimiter();

        // Enable authorization
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapApplicationRoutes(this WebApplication app)
    {
        // Map Health Check endpoint
        app.MapHealthChecks("/health");

        // Redirect root to index.html
        app.MapGet("/", context =>
        {
            context.Response.Redirect("/index.html", permanent: false);
            return Task.CompletedTask;
        });

        // Map all controller routes
        app.MapControllers();

        // Fallback for 404 - must be last
        app.MapFallback(async context =>
        {
            var path = context.Request.Path.Value;

            // Don't handle API, Swagger, or Health routes - let them handle or 404 naturally
            if (path?.StartsWith("/api") == true || path?.StartsWith("/swagger") == true || path?.StartsWith("/health") == true)
            {
                context.Response.StatusCode = 404;
                return;
            }

            // Show 404 page for all other non-existent routes
            context.Response.StatusCode = 404;
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync("wwwroot/404.html");
        });

        return app;
    }

}
