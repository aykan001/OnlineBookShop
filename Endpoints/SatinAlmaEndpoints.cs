using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OnlineBookShop.Services;


public static class SatinAlmaEndpoints
{
    public static void MapSatinAlmaEndpoints(this WebApplication app)
    {
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
                logger.LogError(ex, "Satın alma hatası");
                return Results.Problem(ex.Message);
            }
        });
    }
}
