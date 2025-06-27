using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OnlineBookShop.Services;
using OnlineBookShop.Models;
using System.Text.Json;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Config dosyasını oku (appsettings.json)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Swagger ve CORS
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OnlineBookShop API", Version = "v1" });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Ayarları
var key = Encoding.ASCII.GetBytes("Bu-Cok-Gizli-Bir-Key1234567890!!");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Servisler
builder.Services.AddScoped<IKitapService, KitapService>();
builder.Services.AddScoped<ISepetService, SepetService>();
builder.Services.AddScoped<IBegeniService, BegeniService>();
builder.Services.AddScoped<ISiparisService, SiparisService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPuanService, PuanService>();
builder.Services.AddScoped<ISatinAlmaService, SatinAlmaService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OnlineBookShop API v1");
        c.RoutePrefix = "swagger";
    });
}

// Endpoint çağrıları
app.MapUserEndpoints();
app.MapKitapEndpoints();
app.MapBegeniEndpoints();
app.MapSepetEndpoints();
app.MapPuanEndpoints();
app.MapSiparisEndpoints();
app.MapSatinAlmaEndpoints();

app.Run();

