using OnlineBookShop.Models;

namespace OnlineBookShop.Services
{
    public interface IKitapService
    {
        Task<(List<Kitap> Kitaplar, int Toplam)> GetAllAsync(int sayfa, int adet, string? tur, string? sirala, string? yon);

        Task<Kitap?> GetByIdAsync(int id);
        Task<int> CountAsync(string? tur);

    }
}




