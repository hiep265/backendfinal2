using API.Data;
using API.Extensions;
using API.Helpers;
using API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using System.Threading.Tasks;
using API.MCP.Extensions;
using API.MCP.Configuration;
using API.MCP.Tools;
using API.MCP;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddApplicationServices(builder.Configuration);
// Khi method AddSwaggerDocumentation() chưa tồn tại, hãy sử dụng cấu hình Swagger trực tiếp
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "E-Fashion Shop API", Version = "v1" });
// });
builder.Services.AddIdentityServices(builder.Configuration);

// Configure MCP using extension methods from McpToolsExtension
builder.Services.AddAllMcpTools(builder.Configuration);

// Add AI Services from extension method
builder.Services.AddAIServices(builder.Configuration);

// Register McpClient
builder.Services.AddScoped<IMcpClient>(provider => {
    // Create a transport that uses stdio to communicate with a local MCP server
    var transportOptions = new StdioClientTransportOptions
    {
        Name = "McpServer",
        Command = "dotnet",
        Arguments = new[] { "run", "--project", "../API/API" }
    };
        
    var transport = new StdioClientTransport(transportOptions);
    return McpClientFactory.CreateAsync(transport).GetAwaiter().GetResult();
});

// Register McpClient wrapper
builder.Services.AddScoped<API.MCP.McpClient>();

// Add CORS policy
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true); // Cho phép tất cả nguồn kể cả localhost
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Fashion Shop API v1"));
}

// Sử dụng custom exception middleware
app.UseMiddleware<ExceptionMiddleware>();

// Cấu hình Rewrite khi không tìm thấy route
app.UseStatusCodePagesWithReExecute("/errors/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Cho phép CORS
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Điều hướng về index.html khi không tìm thấy route
app.MapFallbackToController("Index", "Fallback");

// Apply migrations and seed data
await ApplyMigrationsAsync(app);

await app.RunAsync();

// Helper method to apply migrations
async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
        // Sử dụng DPContext thay vì ApplicationDbContext
        var context = services.GetRequiredService<DPContext>();
        await context.Database.MigrateAsync();
        // Uncomment this if you have a seed method
        await SeedData.Seed(app);
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred during migration");
    }
}
