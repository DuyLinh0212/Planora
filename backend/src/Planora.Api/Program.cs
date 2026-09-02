using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Planora.Api.Authorization;
using Planora.Api.BackgroundServices;
using Planora.Api.Hubs;
using Planora.Api.Middleware;
using Planora.Application.Common.Interfaces;
using Planora.Infrastructure;
using Planora.Infrastructure.Authentication;
using Planora.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Render supplies PORT at runtime. Binding explicitly avoids depending on port
// detection and keeps local ASPNETCORE_URLS behavior unchanged.
if (int.TryParse(builder.Configuration["PORT"], out var renderPort) && renderPort is > 0 and <= 65535)
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");

// Keep local OAuth credentials in the .NET user-secrets store. CreateBuilder normally
// adds this provider in Development, but adding it explicitly makes Visual Studio,
// `dotnet run`, and other local launchers behave consistently.
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>(optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwtOptions.Secret) < 64)
    throw new InvalidOperationException("Jwt:Secret must contain at least 64 bytes. Configure it through user secrets or environment variables.");

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Keep every enum contract readable and stable for clients. Without this converter
    // statuses are serialized as 0/1/2..., which breaks string-based UI behavior.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddPlanoraInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddHostedService<OverdueTaskExpirationService>();
builder.Services.AddHostedService<TaskEmailDeliveryService>();
builder.Services.AddHostedService<NotificationRetentionService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options => options.AddDefaultPolicy(corsPolicy => corsPolicy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [
        "http://localhost:4200",
        "http://127.0.0.1:4200",
        "http://localhost:4300",
        "http://127.0.0.1:4300"
    ])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Render's reverse-proxy IPs are not static. The app only trusts
        // forwarded headers outside local development, behind its proxy.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/429",
            title = "Too many authentication attempts.",
            status = StatusCodes.Status429TooManyRequests,
            code = "authentication_rate_limited",
            detail = "Please wait briefly before trying this action again."
        }, cancellationToken);
    };
    options.AddPolicy("auth-login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"login:{httpContext.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("auth-refresh", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"refresh:{httpContext.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("auth-registration", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"registration:{httpContext.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("auth-password", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"password:{httpContext.Connection.RemoteIpAddress}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0, AutoReplenishment = true }));
});
builder.Services.AddHealthChecks().AddDbContextCheck<PlanoraDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var database = migrationScope.ServiceProvider.GetRequiredService<PlanoraDbContext>();
    app.Logger.LogInformation("Applying pending database migrations before accepting traffic.");
    await database.Database.MigrateAsync();
}

app.Logger.LogInformation(
    "External integration configuration loaded. Environment={Environment}, GoogleLoginConfigured={GoogleLoginConfigured}, GmailConfigured={GmailConfigured}, TaskEmailSmtpConfigured={TaskEmailSmtpConfigured}",
    app.Environment.EnvironmentName,
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"]),
    !string.IsNullOrWhiteSpace(builder.Configuration["GmailIntegration:ClientId"])
        && !string.IsNullOrWhiteSpace(builder.Configuration["GmailIntegration:ClientSecret"])
        && !string.IsNullOrWhiteSpace(builder.Configuration["GmailIntegration:TokenEncryptionKey"]),
    !string.IsNullOrWhiteSpace(builder.Configuration["TaskEmailNotifications:SmtpHost"])
        && !string.IsNullOrWhiteSpace(builder.Configuration["TaskEmailNotifications:FromAddress"]));

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
    app.UseForwardedHeaders();
// The local Angular apps intentionally call the HTTP development endpoint on
// port 5273. Redirecting an OPTIONS preflight to HTTPS makes browsers report
// a misleading CORS error because preflight requests do not follow redirects.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "Planora API", version = "v1", openApi = "/openapi/v1.json" })).AllowAnonymous();
app.MapOpenApi().AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = healthCheck => healthCheck.Tags.Contains("ready") }).AllowAnonymous();
app.MapHub<PlanoraHub>("/hubs/planora");
app.MapControllers();

app.Run();

public partial class Program;
