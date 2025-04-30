using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using API.Data;
using API.Helper.Factory;
using API.Helpers;
using API.Models;
using System;
using System.Text;
using API.Dtos;
using API.Helper.Validations;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using FluentValidation;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
namespace API.Extensions
{
    public static class ApplicationServicesExtension
    {
        private const string SecretKey = "iNivDmHLpUA223sqsfhqGbMRdRj1PVkH"; // todo: get this from somewhere secure
        private static readonly SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            var jwtAppSettingOptions = config.GetSection(nameof(API.Helper.Factory.JwtIssuerOptions));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "E-Fashion Shop API",
                    Version = "v1",
                    Description = "API cho ứng dụng thương mại điện tử thời trang",
                    Contact = new OpenApiContact
                    {
                        Name = "E-Fashion Team",
                        Email = "support@efashion.com"
                    }
                });

                // Thêm các tùy chọn bảo mật để xác thực JWT
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            // Configure JwtIssuerOptions
            services.Configure<API.Helper.Factory.JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(API.Helper.Factory.JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(API.Helper.Factory.JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            });

            services.AddControllers().AddNewtonsoftJson();

            services.AddSignalR();
            services.AddControllersWithViews();
            services.AddDbContext<DPContext>(op => op.UseSqlServer(config.GetConnectionString("db")));

            services.Configure<FormOptions>(o => {
                o.ValueLengthLimit = int.MaxValue;
                o.MultipartBodyLengthLimit = int.MaxValue;
                o.MemoryBufferThreshold = int.MaxValue;
            });

            services.AddSingleton<IJwtFactory, JwtFactory>();
            services.AddScoped<IDataConnector, DataConnector>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiUser", policy => policy.RequireClaim(Constants.Strings.JwtClaimIdentifiers.Rol, Constants.Strings.JwtClaims.ApiAccess));
            });

            // Comment out wkhtmltopdf initialization to fix the startup error
            // services.AddWkhtmltopdf("wkhtmltopdf");

            services.AddIdentity<AppUser, IdentityRole>(o =>
            {
                // configure identity options
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<DPContext>()
            .AddDefaultTokenProviders();

            // Add validation
            services.AddScoped<IValidator<CredentialsViewModel>, CredentialsViewModelValidator>();
            services.AddScoped<IValidator<RegistrationViewModel>, RegistrationViewModelValidator>();

            // Add automapper with customized configuration to avoid char mapping issues
            services.AddAutoMapper(cfg =>
{
    // Cho phép ánh xạ null cho collection
    cfg.AllowNullCollections = true;

    // Tắt ánh xạ cho constructor
    cfg.DisableConstructorMapping();

    // Cấu hình để bỏ qua các thuộc tính kiểu char nếu không cần thiết
    cfg.ForAllPropertyMaps(pm => pm.SourceType == typeof(char), (pm, options) =>
    {
        options.Ignore(); // Bỏ qua ánh xạ cho các thuộc tính kiểu char
    });

}, typeof(IStartup).Assembly);

            return services;
        }
    }
}