using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineBookShop.Services;

public static class SiparisEndpoints
{
    public static void MapSiparisEndpoints(this WebApplication app)
    {
        app.MapGet("/api/siparisler/{userId:int}", (ISiparisService siparisService, int userId, ILogger<Program> logger) =>
        {
            try
            {
                var kitaplar = siparisService.GetByUserId(userId);
                return Results.Ok(kitaplar);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sipariş listesi hatası");
                return Results.Problem(ex.Message);
            }
        });
    }
}
