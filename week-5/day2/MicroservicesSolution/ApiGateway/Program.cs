using System.Text;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Load Ocelot config ──────────────────────────────────────────────────────
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// ── JWT Authentication (validates tokens at the gateway level) ──────────────
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSection["Secret"]!;
var issuer      = jwtSection["Issuer"]!;
var audience    = jwtSection["Audience"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── CORS — allow all origins in dev ─────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("GatewayCors", p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── Ocelot ──────────────────────────────────────────────────────────────────
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("GatewayCors");

// Centralized gateway error handling
app.UseMiddleware<GatewayExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Start Ocelot pipeline — this handles all /gateway/* routing
await app.UseOcelot();

app.Run();
