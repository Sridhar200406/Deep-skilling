using System.Text;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var jwt    = builder.Configuration.GetSection("JwtSettings");
var secret = jwt["Secret"]!; var issuer = jwt["Issuer"]!; var audience = jwt["Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true, ValidIssuer   = issuer,
            ValidateAudience = true, ValidAudience = audience,
            ValidateLifetime = true, ClockSkew    = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("GatewayCors",
    p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();
app.UseCors("GatewayCors");
app.UseMiddleware<GatewayExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
await app.UseOcelot();
app.Run();
