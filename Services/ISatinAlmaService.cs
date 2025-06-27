namespace OnlineBookShop.Services
{
    public interface ISatinAlmaService
    {
        Task<(bool Success, string? Error)> SatinAlAsync(int userId, List<int> kitapIdListesi);
    }
}
