using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineBookShop.Models;
using OnlineBookShop.Services;


public static class PuanEndpoints
{
    public static void MapPuanEndpoints(this WebApplication app)
    {
        app.MapPost("/api/puanver", async (IPuanService puanService, HttpContext context, ILogger<Program> logger) =>
        {
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, int>>(await new StreamReader(context.Request.Body).ReadToEndAsync())!;
                var ok = await puanService.PuanVerAsync(data["kullaniciId"], data["kitapId"], data["puan"]);
                return ok ? Results.Ok("Puan kaydedildi.") : Results.Problem("Kaydedilemedi.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Puan hatası");
                return Results.Problem(ex.Message);
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
                logger.LogError(ex, "Puan ortalaması hatası");
                return Results.Problem(ex.Message);
            }
        });
    }
}
