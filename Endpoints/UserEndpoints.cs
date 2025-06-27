using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineBookShop.Models;
using OnlineBookShop.Services;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var key = Encoding.ASCII.GetBytes("Bu-Cok-Gizli-Bir-Key1234567890!!");

        // Kullanıcı kayıt
        app.MapPost("/api/register", async (IUserService userService, User user, ILogger<Program> logger) =>
        {
            try
            {
                var success = await userService.RegisterAsync(user);
                return success ? Results.Ok("Kayıt başarılı.") : Results.BadRequest("Geçersiz bilgi veya email zaten kayıtlı.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kayıt hatası");
                return Results.Problem(ex.Message);
            }
        });

        // Giriş (Login) + JWT Token oluşturma
        app.MapPost("/api/login", async (IUserService userService, User loginUser, ILogger<Program> logger) =>
        {
            try
            {
                var (success, userId, ad, email) = await userService.ValidateLoginAsync(loginUser.Email, loginUser.Sifre);
                if (!success) return Results.BadRequest("Email veya şifre hatalı.");

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
                logger.LogError(ex, "Giriş hatası");
                return Results.Problem(ex.Message);
            }
        });

        // Kullanıcı bilgilerini ID ile getir
        app.MapGet("/api/users/{id:int}", async (IUserService userService, int id, ILogger<Program> logger) =>
        {
            try
            {
                var user = await userService.GetByIdAsync(id);
                return user != null ? Results.Ok(user) : Results.NotFound("Kullanıcı bulunamadı.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kullanıcı bilgisi hatası");
                return Results.Problem(ex.Message);
            }
        });

        // Kullanıcı ve ilişkili tüm verileri sil (CASCADE veya elle)
        app.MapDelete("/api/users/{id:int}", async (int id, IUserService userService, ILogger<Program> logger) =>
        {
            try
            {
                var result = await userService.DeleteAsync(id);
                return result ? Results.Ok("Kullanıcı silindi.") : Results.Problem("Silme başarısız.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hesap silme hatası");
                return Results.Problem("Hata: " + ex.Message);
            }
        });

        // Şifre güncelleme
        app.MapPost("/api/change-password", async (IUserService userService, HttpContext context, ILogger<Program> logger) =>
        {
            try
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body)!;

                var result = await userService.UpdatePasswordAsync(
                    int.Parse(data["userId"]), data["oldPassword"], data["newPassword"]);

                return result ? Results.Ok("Şifre güncellendi.") : Results.BadRequest("Eski şifre yanlış.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Şifre güncelleme hatası");
                return Results.Problem(ex.Message);
            }
        });
    }
}
