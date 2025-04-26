using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using API.Helper.Factory;
using System.Text;

namespace API.Extensions
{
    public static class IdentityServiceExtensions
    {
        private const string SecretKey = "iNivDmHLpUA223sqsfhqGbMRdRj1PVkH"; // todo: get this from somewhere secure
        private static readonly SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {
            var jwtAppSettingOptions = config.GetSection(nameof(API.Helper.Factory.JwtIssuerOptions));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtAppSettingOptions[nameof(API.Helper.Factory.JwtIssuerOptions.Issuer)],
                        ValidateAudience = true,
                        ValidAudience = jwtAppSettingOptions[nameof(API.Helper.Factory.JwtIssuerOptions.Audience)],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = _signingKey,
                        RequireExpirationTime = false,
                        ValidateLifetime = false,
                        ClockSkew = System.TimeSpan.Zero
                    };
                    
                    // Thêm xử lý sự kiện cho JwtBearer
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            // Ghi log khi xác thực thất bại
                            return System.Threading.Tasks.Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            // Xử lý khi token hợp lệ
                            return System.Threading.Tasks.Task.CompletedTask;
                        }
                    };
                });

            return services;
        }
    }
} 