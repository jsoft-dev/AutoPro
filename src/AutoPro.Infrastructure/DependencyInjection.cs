using System.Text;
using AutoPro.Application.Billing;
using AutoPro.Application.Inventory;
using AutoPro.Application.Email;
using AutoPro.Application.Messaging;
using AutoPro.Application.Options;
using AutoPro.Application.Sms;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AutoPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));

        services.AddDbContext<AutoProDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AutoProDbContext).Assembly.GetName().Name)));

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<JobNumberGenerator>();
        services.AddScoped<GoodsReceiptNumberGenerator>();
        services.AddScoped<PurchaseOrderNumberGenerator>();
        services.AddScoped<IInventoryStockAdjuster, InventoryStockAdjuster>();
        services.AddScoped<IGoodsReceiptPostingService, GoodsReceiptPostingService>();
        services.AddScoped<IJobInvoiceFinalizer, JobInvoiceFinalizer>();
        services.AddScoped<IPaymentClosedNotifier, PaymentClosedNotifier>();
        services.AddScoped<OrganizationLogoStore>();

        var email = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (email.Enabled &&
            string.Equals(email.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(email.SmtpHost))
            {
                throw new InvalidOperationException("Email is Enabled with Provider Smtp but SmtpHost is missing.");
            }

            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        var sms = configuration.GetSection(SmsOptions.SectionName).Get<SmsOptions>() ?? new SmsOptions();
        if (sms.Enabled &&
            string.Equals(sms.Provider, "SmsGo", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(sms.ApiKey))
            {
                throw new InvalidOperationException("Sms is Enabled with Provider SmsGo but ApiKey is missing.");
            }

            var baseUrl = (string.IsNullOrWhiteSpace(sms.BaseUrl) ? "https://api.smsgo.lk" : sms.BaseUrl).TrimEnd('/') + "/";
            services.AddHttpClient<ISmsSender, SmsGoSmsSender>(client =>
                client.BaseAddress = new Uri(baseUrl));
        }
        else
        {
            services.AddSingleton<ISmsSender, LoggingSmsSender>();
        }

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
