using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineBookShop.Models;
using OnlineBookShop.Services;


public static class BegeniEndpoints
{
    public static void MapBegeniEndpoints(this WebApplication app)
    {
        app.MapGet("/api/begeniler/{userId:int}", (IBegeniService begeniService, int userId, ILogger<Program> logger) =>
        {
            try
            {
                return Results.Ok(begeniService.GetByUserId(userId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Beğeniler alınamadı");
                return Results.Problem(ex.Message);
            }
        });

        app.MapPost("/api/begeniler", async (IBegeniService begeniService, HttpContext context, ILogger<Program> logger) =>
        {
            try
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<Dictionary<string, int>>(body)!;

                var result = await begeniService.AddAsync(data["kitapId"], data["kullaniciId"]);
                return result ? Results.Ok("Beğeni eklendi.") : Results.BadRequest("Zaten beğenilmiş.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Beğeni ekleme hatası");
                return Results.Problem(ex.Message);
            }
        });

        app.MapDelete("/api/begeniler/{kitapId:int}/{kullaniciId:int}", async (IBegeniService begeniService, int kitapId, int kullaniciId, ILogger<Program> logger) =>
        {
            try
            {
                var result = await begeniService.RemoveAsync(kitapId, kullaniciId);
                return result ? Results.Ok("Beğeni silindi.") : Results.NotFound("Beğeni yok.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Beğeni silme hatası");
                return Results.Problem(ex.Message);
            }
        });
    }
}
