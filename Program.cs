using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Npgsql;
using OnlineBookShop.Models;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OnlineBookShop.Services;

var builder = WebApplication.CreateBuilder(args);

// Swagger + CORS
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.AddLogging();
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

// Dependency Injection
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

// Veritabanı bağlantı stringi
string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";

// Kayıt
app.MapPost("/api/register", async (IUserService userService, User user, ILogger<Program> logger) =>
{
    try
    {
        var success = await userService.RegisterAsync(user);
        return success ? Results.Ok("Kayıt başarılı.") : Results.BadRequest("Geçersiz bilgi veya email zaten kayıtlı.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Kayıt işlemi sırasında hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

// Giriş
app.MapPost("/api/login", async (IUserService userService, User loginUser, ILogger<Program> logger) =>
{
    try
    {
        var (success, userId, ad, email) = await userService.ValidateLoginAsync(loginUser.Email, loginUser.Sifre);
        if (!success)
            return Results.BadRequest("Email veya şifre hatalı.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", userId.ToString()),
                new Claim("email", email)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Results.Ok(new { message = "Giriş yapıldı", userId, token = jwt });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Giriş işlemi sırasında hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

// Sadece kitap listeleme
app.MapGet("/api/kitaplar", async (IKitapService kitapService, ILogger<Program> logger) =>
{
    try
    {
        var kitaplar = await kitapService.GetAllAsync();
        return Results.Ok(kitaplar);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Kitaplar alınırken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

// Beğeniler
app.MapGet("/api/begeniler/{userId:int}", (IBegeniService begeniService, int userId, ILogger<Program> logger) =>
{
    try
    {
        var kitaplar = begeniService.GetByUserId(userId);
        return Results.Ok(kitaplar);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Beğenileri getirirken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapPost("/api/begeniler", async (IBegeniService begeniService, HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, int>>(body)!;

        var result = await begeniService.AddAsync(data["kitapId"], data["kullaniciId"]);
        if (!result)
            return Results.BadRequest("Bu kitap zaten beğenilmiş.");
        return Results.Ok("Beğeni eklendi.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Beğeni eklerken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapDelete("/api/begeniler/{kitapId:int}/{kullaniciId:int}", async (IBegeniService begeniService, int kitapId, int kullaniciId, ILogger<Program> logger) =>
{
    try
    {
        var result = await begeniService.RemoveAsync(kitapId, kullaniciId);
        if (!result)
            return Results.NotFound("Beğeni bulunamadı.");
        return Results.Ok("Beğeni silindi.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Beğeni silerken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

// Sepet
app.MapPost("/api/sepet", async (ISepetService sepetService, Sepet sepet, ILogger<Program> logger) =>
{
    try
    {
        var result = await sepetService.AddAsync(sepet);
        if (!result)
            return Results.BadRequest("Bu kitap zaten sepette var.");
        return Results.Ok("Kitap sepete eklendi.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Sepete ekleme hatası");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapGet("/api/sepet/{userId:int}", (ISepetService sepetService, int userId, ILogger<Program> logger) =>
{
    try
    {
        var kitaplar = sepetService.GetByUserId(userId);
        return Results.Ok(kitaplar);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Sepeti listelerken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapDelete("/api/sepet/{kitapId:int}/{kullaniciId:int}", async (ISepetService sepetService, int kitapId, int kullaniciId) =>
{
    var result = await sepetService.RemoveAsync(kitapId, kullaniciId);
    if (!result)
        return Results.NotFound("Kitap sepette bulunamadı.");
    return Results.Ok("Kitap sepetten silindi.");
});

// Kullanıcı
app.MapGet("/api/users/{id:int}", async (IUserService userService, int id, ILogger<Program> logger) =>
{
    try
    {
        var user = await userService.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound("Kullanıcı bulunamadı.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Kullanıcı bilgisi getirme hatası");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapDelete("/api/users/{id:int}", async (IUserService userService, int id, ILogger<Program> logger) =>
{
    try
    {
        var result = await userService.DeleteAsync(id);
        return result ? Results.Ok("Hesap silindi.") : Results.NotFound("Kullanıcı bulunamadı.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Hesap silme hatası");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapPost("/api/change-password", async (IUserService userService, HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body)!;

        int userId = int.Parse(data["userId"]);
        string oldPassword = data["oldPassword"];
        string newPassword = data["newPassword"];

        var result = await userService.UpdatePasswordAsync(userId, oldPassword, newPassword);
        return result ? Results.Ok("Şifre güncellendi.") : Results.BadRequest("Eski şifre yanlış.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Şifre güncelleme hatası");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

// Puan
app.MapPost("/api/puanver", async (IPuanService puanService, HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, int>>(await new StreamReader(context.Request.Body).ReadToEndAsync())!;
        var ok = await puanService.PuanVerAsync(data["kullaniciId"], data["kitapId"], data["puan"]);
        return ok ? Results.Ok("Puan kaydedildi.") : Results.Problem("Hata oluştu.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Puan verirken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapGet("/api/kitappuan/{kitapId:int}", async (IPuanService puanService, int kitapId, ILogger<Program> logger) =>
{
    try
    {
        var ort = await puanService.OrtalamaPuanAsync(kitapId);
        return Results.Ok(ort);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ortalama puan getirilirken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

// Satın alma
app.MapPost("/api/satinalma", async (ISatinAlmaService satinService, HttpContext context, ILogger<Program> logger) =>
{
    try
    {
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body)!;
        int userId = data["userId"].GetInt32();
        var kitapIds = data["kitapIds"].EnumerateArray().Select(e => e.GetInt32()).ToList();

        var (success, error) = await satinService.SatinAlAsync(userId, kitapIds);
        return success ? Results.Ok("Satın alma başarılı.") : Results.BadRequest(error);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Satın alma sırasında hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.MapGet("/api/siparisler/{userId:int}", (ISiparisService siparisService, int userId, ILogger<Program> logger) =>
{
    try
    {
        var kitaplar = siparisService.GetByUserId(userId);
        return Results.Ok(kitaplar);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Siparişler getirilirken hata oluştu.");
        return Results.Problem("Sunucu hatası: " + ex.Message);
    }
});

app.Run();

