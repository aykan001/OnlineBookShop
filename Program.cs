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

var builder = WebApplication.CreateBuilder(args);

// Swagger + CORS
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

var key = Encoding.ASCII.GetBytes("Bu-Cok-Gizli-Bir-Key1234567890!!"); // ✅ En az 32 karakter

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

string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=12345;Database=users";


// ➔ Kayıt
app.MapPost("/api/register", async (HttpContext context) =>
{
    try
    {
        var user = await context.Request.ReadFromJsonAsync<User>();
        var validationContext = new ValidationContext(user!);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(user!, validationContext, results, true))
        {
            var errors = results.Select(r => r.ErrorMessage).ToList();
            return Results.BadRequest(string.Join(" | ", errors));
        }

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        using var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email = @Email", conn);
        checkCmd.Parameters.AddWithValue("Email", user.Email);
        var exists = (long)await checkCmd.ExecuteScalarAsync();

        if (exists > 0)
            return Results.BadRequest("Bu email zaten kayıtlı.");

        using var insertCmd = new NpgsqlCommand("INSERT INTO users (ad, email, sifre) VALUES (@Ad, @Email, @Sifre)", conn);
        insertCmd.Parameters.AddWithValue("Ad", user.Ad);
        insertCmd.Parameters.AddWithValue("Email", user.Email);
        insertCmd.Parameters.AddWithValue("Sifre", user.Sifre);
        await insertCmd.ExecuteNonQueryAsync();

        return Results.Ok("Kayıt başarılı.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ➔ Giriş
app.MapPost("/api/login", async (User loginUser) =>
{
    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("SELECT id, ad, email FROM users WHERE email = @Email AND sifre = @Sifre", conn);
        cmd.Parameters.AddWithValue("Email", loginUser.Email);
        cmd.Parameters.AddWithValue("Sifre", loginUser.Sifre);

        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            string ad = reader.GetString(1);
            string email = reader.GetString(2);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", id.ToString()),
                    new Claim("email", email)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Results.Ok(new { message = "Giriş yapıldı", userId = id, token = jwt });
        }

        return Results.BadRequest("Email veya şifre hatalı.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ➔ Kitapları Getir
app.MapGet("/api/kitaplar", async (HttpRequest request) =>
{
    var kitaplar = new List<Kitap>();

    // İsteğe bağlı parametreleri al
    int sayfa = int.TryParse(request.Query["sayfa"], out var s) ? s : 1;
    int adet = int.TryParse(request.Query["adet"], out var a) ? a : 10;
    string? tur = request.Query["tur"];
    string? sirala = request.Query["sirala"]; // kitap_adi, ucret, stok
    string? yon = request.Query["yon"]; // asc, desc

    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Temel SQL
        string sql = "SELECT id, kitap_adi, tur, aciklama, stok, ucret FROM kitaplar";

        var filters = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        // ➤ Filtre: Tür
        if (!string.IsNullOrEmpty(tur))
        {
            filters.Add("tur ILIKE @tur");
            parameters.Add(new NpgsqlParameter("tur", $"%{tur}%"));
        }

        if (filters.Count > 0)
            sql += " WHERE " + string.Join(" AND ", filters);

        // ➤ Sıralama
        var siralanabilirAlanlar = new[] { "kitap_adi", "ucret", "stok" };
        var yonler = new[] { "asc", "desc" };
        if (!string.IsNullOrEmpty(sirala) && siralanabilirAlanlar.Contains(sirala.ToLower()))
        {
            string siralamaYonu = yonler.Contains(yon?.ToLower()) ? yon.ToUpper() : "ASC";
            sql += $" ORDER BY {sirala} {siralamaYonu}";
        }

        // ➤ Sayfalama
        sql += " LIMIT @limit OFFSET @offset";
        parameters.Add(new NpgsqlParameter("limit", adet));
        parameters.Add(new NpgsqlParameter("offset", (sayfa - 1) * adet));

        // Komutu çalıştır
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters.ToArray());
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            kitaplar.Add(new Kitap
            {
                Id = reader.GetInt32(0),
                KitapAdi = reader.GetString(1),
                Tur = reader.GetString(2),
                Aciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Stok = reader.GetInt32(4),
                Ucret = reader.GetDecimal(5)
            });
        }

        return Results.Ok(kitaplar);
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});


// ✅ Kullanıcıya özel beğenileri getirme
app.MapGet("/api/begeniler/{userId:int}", async (int userId) =>
{
    var begenilenKitaplar = new List<Kitap>();

    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = @"
          SELECT k.id, k.kitap_adi, k.tur, k.aciklama, k.stok, k.ucret
          FROM begeniler b
          INNER JOIN kitaplar k ON b.kitap_id = k.id
          WHERE b.kullanici_id = @userId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            begenilenKitaplar.Add(new Kitap
            {
                Id = reader.GetInt32(0),
                KitapAdi = reader.GetString(1),
                Tur = reader.GetString(2),
                Aciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Stok = reader.GetInt32(4),
                Ucret = reader.GetDecimal(5)
            });
        }

        return Results.Ok(begenilenKitaplar);
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ✅ Kitap beğenme (POST)
app.MapPost("/api/begeniler", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, int>>(body)!;

        int kitapId = data["kitapId"];
        int kullaniciId = data["kullaniciId"];

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var check = new NpgsqlCommand("SELECT COUNT(*) FROM begeniler WHERE kitap_id = @kitapId AND kullanici_id = @kullaniciId", conn);
        check.Parameters.AddWithValue("kitapId", kitapId);
        check.Parameters.AddWithValue("kullaniciId", kullaniciId);
        var exists = (long)await check.ExecuteScalarAsync();

        if (exists > 0)
            return Results.BadRequest("Bu kitap zaten beğenilmiş.");

        var insert = new NpgsqlCommand("INSERT INTO begeniler (kitap_id, kullanici_id) VALUES (@kitapId, @kullaniciId)", conn);
        insert.Parameters.AddWithValue("kitapId", kitapId);
        insert.Parameters.AddWithValue("kullaniciId", kullaniciId);
        await insert.ExecuteNonQueryAsync();

        return Results.Ok("Beğeni eklendi.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ✅ Beğeni silme (DELETE)
app.MapDelete("/api/begeniler/{kitapId:int}/{kullaniciId:int}", async (int kitapId, int kullaniciId) =>
{
    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM begeniler WHERE kitap_id = @kitapId AND kullanici_id = @kullaniciId", conn);
        cmd.Parameters.AddWithValue("kitapId", kitapId);
        cmd.Parameters.AddWithValue("kullaniciId", kullaniciId);
        int affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return Results.NotFound("Beğeni bulunamadı.");

        return Results.Ok("Beğeni kaldırıldı.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ➤ Sepete Ekleme
app.MapPost("/api/sepet", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<Sepet>(body, options);

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Aynı kullanıcı aynı kitabı birden fazla ekleyemesin
        using var check = new NpgsqlCommand("SELECT COUNT(*) FROM sepet WHERE kitap_id = @kitapId AND kullanici_id = @kullaniciId", conn);
        check.Parameters.AddWithValue("kitapId", data.KitapId);
        check.Parameters.AddWithValue("kullaniciId", data.KullaniciId);
        var exists = (long)await check.ExecuteScalarAsync();

        if (exists > 0)
            return Results.BadRequest("Bu kitap zaten sepete eklenmiş.");

        using var insert = new NpgsqlCommand("INSERT INTO sepet (kitap_id, kullanici_id) VALUES (@kitapId, @kullaniciId)", conn);
        insert.Parameters.AddWithValue("kitapId", data.KitapId);
        insert.Parameters.AddWithValue("kullaniciId", data.KullaniciId);
        await insert.ExecuteNonQueryAsync();

        return Results.Ok("Kitap sepete eklendi.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});



// ➤ Sepeti Listele
app.MapGet("/api/sepet/{userId:int}", async (int userId) =>
{
    var sepetKitaplar = new List<Kitap>();

    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = @"
          SELECT k.id, k.kitap_adi, k.tur, k.aciklama, k.stok, k.ucret
          FROM sepet s
          INNER JOIN kitaplar k ON s.kitap_id = k.id
          WHERE s.kullanici_id = @userId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            sepetKitaplar.Add(new Kitap
            {
                Id = reader.GetInt32(0),
                KitapAdi = reader.GetString(1),
                Tur = reader.GetString(2),
                Aciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Stok = reader.GetInt32(4),
                Ucret = reader.GetDecimal(5)
            });
        }

        return Results.Ok(sepetKitaplar);
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});




// ➤ Sepetten Silme
app.MapDelete("/api/sepet/{bookId:int}/{userId:int}", async (int bookId, int userId) =>
{
    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("DELETE FROM sepet WHERE kitap_id = @kitapId AND kullanici_id = @userId", conn);
        cmd.Parameters.AddWithValue("kitapId", bookId);
        cmd.Parameters.AddWithValue("userId", userId);
        int affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return Results.NotFound("Kitap sepette bulunamadı.");

        return Results.Ok("Kitap sepetten silindi.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});



// ➤ Kullanıcı Bilgilerini Getir
app.MapGet("/api/users/{id:int}", async (int id) =>
{
    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("SELECT ad, email FROM users WHERE id = @Id", conn);
        cmd.Parameters.AddWithValue("Id", id);
        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return Results.Ok(new {
                ad = reader.GetString(0),
                email = reader.GetString(1)
            });
        }

        return Results.NotFound("Kullanıcı bulunamadı.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ➤ Hesap Silme
app.MapDelete("/api/users/{id:int}", async (int id) =>
{
    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("DELETE FROM users WHERE id = @Id", conn);
        cmd.Parameters.AddWithValue("Id", id);
        int affected = await cmd.ExecuteNonQueryAsync();

        if (affected == 0)
            return Results.NotFound("Kullanıcı bulunamadı.");

        return Results.Ok("Hesap silindi.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ➤ Şifre Güncelleme
app.MapPost("/api/change-password", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

        int userId = int.Parse(data["userId"]);
        string oldPassword = data["oldPassword"];
        string newPassword = data["newPassword"];

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE id = @id AND sifre = @sifre", conn);
        checkCmd.Parameters.AddWithValue("id", userId);
        checkCmd.Parameters.AddWithValue("sifre", oldPassword);
        var exists = (long)await checkCmd.ExecuteScalarAsync();

        if (exists == 0)
            return Results.BadRequest("Eski şifre yanlış.");

        var updateCmd = new NpgsqlCommand("UPDATE users SET sifre = @yeni WHERE id = @id", conn);
        updateCmd.Parameters.AddWithValue("yeni", newPassword);
        updateCmd.Parameters.AddWithValue("id", userId);
        await updateCmd.ExecuteNonQueryAsync();

        return Results.Ok("Şifre güncellendi.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ➤ Puan Ver
app.MapPost("/api/puanver", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, int>>(body)!;

        int kullaniciId = data["kullaniciId"];
        int kitapId = data["kitapId"];
        int puan = data["puan"];

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var upsert = new NpgsqlCommand(@"
            INSERT INTO puanlar (kullanici_id, kitap_id, puan)
            VALUES (@kullaniciId, @kitapId, @puan)
            ON CONFLICT (kullanici_id, kitap_id)
            DO UPDATE SET puan = EXCLUDED.puan;
        ", conn);
        upsert.Parameters.AddWithValue("kullaniciId", kullaniciId);
        upsert.Parameters.AddWithValue("kitapId", kitapId);
        upsert.Parameters.AddWithValue("puan", puan);
        await upsert.ExecuteNonQueryAsync();

        return Results.Ok("Puan kaydedildi.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});

// ➤ Kitap Ortalama Puan Getir
app.MapGet("/api/kitappuan/{kitapId:int}", async (int kitapId) =>
{
    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new NpgsqlCommand("SELECT AVG(puan) FROM puanlar WHERE kitap_id = @kitapId", conn);
        cmd.Parameters.AddWithValue("kitapId", kitapId);
        var result = await cmd.ExecuteScalarAsync();

        if (result is DBNull || result == null)
            return Results.Ok(0.0);

        double avg = Convert.ToDouble(result);
        return Results.Ok(avg);
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});



// ➤ Satın alma
app.MapPost("/api/satinalma", async (HttpContext context) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body, options);

        int userId = data["userId"].GetInt32();
        var kitapIdArray = data["kitapIds"].EnumerateArray().Select(k => k.GetInt32()).ToList();

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        foreach (var kitapId in kitapIdArray)
        {
            // Stok kontrolü
            var stokCmd = new NpgsqlCommand("SELECT stok FROM kitaplar WHERE id = @kitapId", conn);
            stokCmd.Parameters.AddWithValue("kitapId", kitapId);
            var stokResult = await stokCmd.ExecuteScalarAsync();
            int stok = stokResult != null ? Convert.ToInt32(stokResult) : 0;

            if (stok <= 0)
                return Results.BadRequest($"Kitap ID {kitapId} stokta yok.");

            // Stok azalt
            var updateCmd = new NpgsqlCommand("UPDATE kitaplar SET stok = stok - 1 WHERE id = @kitapId", conn);
            updateCmd.Parameters.AddWithValue("kitapId", kitapId);
            await updateCmd.ExecuteNonQueryAsync();

            // Sepetten sil
            var deleteCmd = new NpgsqlCommand("DELETE FROM sepet WHERE kitap_id = @kitapId AND kullanici_id = @userId", conn);
            deleteCmd.Parameters.AddWithValue("kitapId", kitapId);
            deleteCmd.Parameters.AddWithValue("userId", userId);
            await deleteCmd.ExecuteNonQueryAsync();

            // → Sipariş tablosuna ekle
            var siparisCmd = new NpgsqlCommand("INSERT INTO siparisler (kullanici_id, kitap_id) VALUES (@userId, @kitapId)", conn);
            siparisCmd.Parameters.AddWithValue("userId", userId);
            siparisCmd.Parameters.AddWithValue("kitapId", kitapId);
            await siparisCmd.ExecuteNonQueryAsync();
        }

        return Results.Ok("Satın alma işlemi başarıyla tamamlandı.");
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});


app.MapGet("/api/siparisler/{userId:int}", async (int userId) =>
{
    var siparisler = new List<Kitap>();

    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        string sql = @"
          SELECT k.id, k.kitap_adi, k.tur, k.aciklama, k.stok, k.ucret
          FROM siparisler s
          INNER JOIN kitaplar k ON s.kitap_id = k.id
          WHERE s.kullanici_id = @userId";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            siparisler.Add(new Kitap
            {
                Id = reader.GetInt32(0),
                KitapAdi = reader.GetString(1),
                Tur = reader.GetString(2),
                Aciklama = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Stok = reader.GetInt32(4),
                Ucret = reader.GetDecimal(5)
            });
        }

        return Results.Ok(siparisler);
    }
    catch (Exception ex)
    {
        return Results.Problem("Hata: " + ex.Message);
    }
});


app.Run();
