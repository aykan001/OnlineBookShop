using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineBookShop.Models;
using OnlineBookShop.Services;


public static class SepetEndpoints
{
    public static void MapSepetEndpoints(this WebApplication app)
    {
        app.MapPost("/api/sepet", async (ISepetService sepetService, Sepet sepet, ILogger<Program> logger) =>
        {
            try
            {
                var result = await sepetService.AddAsync(sepet);
                return result ? Results.Ok("Sepete eklendi.") : Results.BadRequest("Bu kitap zaten sepette var.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sepete ekleme hatası");
                return Results.Problem(ex.Message);
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
                logger.LogError(ex, "Sepet alınamadı");
                return Results.Problem(ex.Message);
            }
        });

        app.MapDelete("/api/sepet/{kitapId:int}/{kullaniciId:int}", async (ISepetService sepetService, int kitapId, int kullaniciId) =>
        {
            try
            {
                var result = await sepetService.RemoveAsync(kitapId, kullaniciId);
                return result ? Results.Ok("Sepetten silindi.") : Results.NotFound("Kitap sepette bulunamadı.");
            }
            catch (Exception ex)
            {
                return Results.Problem("Silme hatası: " + ex.Message);
            }
        });
    }
}

