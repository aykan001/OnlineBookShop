using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineBookShop.Services;

public static class KitapEndpoints
{
    public static void MapKitapEndpoints(this WebApplication app)
    {
        app.MapGet("/api/kitaplar", async (
                int sayfa,
                int adet,
                string? tur,
                string? sirala,
                string? yon,
                IKitapService kitapService,
                ILogger<Program> logger) =>
            {
                try
                {
                    var (kitaplar, toplam) = await kitapService.GetAllAsync(sayfa, adet, tur, sirala, yon);
                    return Results.Ok(new { kitaplar, toplam });

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Kitaplar alınamadı.");
                    return Results.Problem(ex.Message);
                }
            });
            
      

    }
}


